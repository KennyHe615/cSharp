using System.Text.Json;

using Application.Abstractions.Identity;

using Infrastructure.Configuration.Options;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Contracts;
using Infrastructure.Identity;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Environment;
using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace Infrastructure.ExternalApis.Providers.Genesys.Auth;

/// <summary>
/// Storage implementation for <see cref="IGenesysTokenStore"/>.
/// </summary>
/// <remarks>
/// Lookup order for reads:
/// <c>memory cache -> Key Vault</c>.
/// <para>
/// When Key Vault is unavailable, operations degrade gracefully:
/// memory remains usable for the current process lifetime.
/// </para>
/// </remarks>
public sealed class GenesysTokenStore : IGenesysTokenStore
{
    #region ========== *** Properties and Constructor *** ==========

    private static readonly JsonSerializerOptions TokenJsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IMemoryCache _cache;
    private readonly ISecretProvider _secretProvider;
    private readonly KeyVaultOptions _keyVaultOptions;
    private readonly AppEnvironment _appEnvironment;
    private readonly ILogger<GenesysTokenStore> _logger;
    private const string LogCategory = "GenesysTokenStore";

    /// <summary>
    /// Initializes a new instance of the <see cref="GenesysTokenStore"/> class.
    /// </summary>
    /// <param name="cache">In-memory token cache.</param>
    /// <param name="secretProvider">Key Vault secret provider abstraction.</param>
    /// <param name="keyVaultOptions">Key Vault naming options.</param>
    /// <param name="appEnvironment">Normalized runtime environment context.</param>
    /// <param name="logger">Logger instance.</param>
    public GenesysTokenStore(IMemoryCache cache,
                             ISecretProvider secretProvider,
                             IOptions<KeyVaultOptions> keyVaultOptions,
                             AppEnvironment appEnvironment,
                             ILogger<GenesysTokenStore> logger)
    {
        _cache = cache                   ?? throw new ArgumentNullException(nameof(cache));
        _secretProvider = secretProvider ?? throw new ArgumentNullException(nameof(secretProvider));
        _appEnvironment = appEnvironment;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(keyVaultOptions);
        _keyVaultOptions = keyVaultOptions.Value;
    }

    #endregion

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="lobKey"/> is null/empty/whitespace.</exception>
    public async Task<GenesysTokenCacheEntry?> TryGetValidAsync(string lobKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(lobKey))
        {
            throw new ArgumentException("LOB key cannot be null, empty, or whitespace.", nameof(lobKey));
        }

        const string logEntity = "TryGetValid";
        using IDisposable scope = _logger.BeginOperationScope(new LobName(lobKey), LogCategory, logEntity);

        string cacheKey = BuildCacheKey(lobKey);

        if (TryGetValidFromMemory(cacheKey, out GenesysTokenCacheEntry? memoryEntry)) return memoryEntry;

        string secretName = BuildTokenSecretName(lobKey);

        try
        {
            string? payload = await _secretProvider.TryGetSecretAsync(secretName, ct)
                                                   .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payload)) return null;

            GenesysTokenCacheEntry? keyVaultEntry;
            try
            {
                keyVaultEntry = JsonSerializer.Deserialize<GenesysTokenCacheEntry>(payload, TokenJsonOptions);
            }
            catch (JsonException ex)
            {
                _logger.LogWarningWithDetails(ex,
                                              LobLogTemplates.LobCategoryEntity
                                              + "Invalid token payload format in Key Vault secret '{SecretName}'.",
                                              new LobName(lobKey).Value,
                                              LogCategory,
                                              logEntity,
                                              secretName);

                return null;
            }

            if (!IsValid(keyVaultEntry)) return null;

            _cache.Set(cacheKey, keyVaultEntry!, keyVaultEntry!.ExpiresAtUtc);

            return keyVaultEntry;
        }
        catch (KeyVaultSecretException ex)
        {
            // Degraded mode: continue to OAuth flow when Key Vault is unavailable.
            _logger.LogWarningWithDetails(ex,
                                          LobLogTemplates.LobCategoryEntity
                                          + "Key Vault unavailable while reading token secret '{SecretName}'.",
                                          new LobName(lobKey).Value,
                                          LogCategory,
                                          logEntity,
                                          secretName);

            return null;
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="lobKey"/> is null/empty/whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entry"/> is null.</exception>
    public async Task UpsertAsync(string lobKey, GenesysTokenCacheEntry entry, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(lobKey))
        {
            throw new ArgumentException("LOB key cannot be null, empty, or whitespace.", nameof(lobKey));
        }

        ArgumentNullException.ThrowIfNull(entry);

        const string logEntity = "Upsert";
        using IDisposable scope = _logger.BeginOperationScope(new LobName(lobKey), LogCategory, logEntity);

        string cacheKey = BuildCacheKey(lobKey);
        string secretName = BuildTokenSecretName(lobKey);

        _cache.Set(cacheKey, entry, entry.ExpiresAtUtc);

        string payload = JsonSerializer.Serialize(entry, TokenJsonOptions);

        try
        {
            await _secretProvider.UpsertSecretAsync(secretName, payload, ct)
                                 .ConfigureAwait(false);
        }
        catch (KeyVaultSecretException ex)
        {
            // Degraded mode: in-memory still available for current run.
            _logger.LogWarningWithDetails(ex,
                                          LobLogTemplates.LobCategoryEntity
                                          + "Failed to persist token to Key Vault secret '{SecretName}'. Using in-memory token only.",
                                          new LobName(lobKey).Value,
                                          LogCategory,
                                          logEntity,
                                          secretName);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="lobKey"/> is null/empty/whitespace.</exception>
    public async Task RemoveAsync(string lobKey, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(lobKey))
        {
            throw new ArgumentException("LOB key cannot be null, empty, or whitespace.", nameof(lobKey));
        }

        const string logEntity = "Remove";
        using IDisposable scope = _logger.BeginOperationScope(new LobName(lobKey), LogCategory, logEntity);

        string cacheKey = BuildCacheKey(lobKey);
        string secretName = BuildTokenSecretName(lobKey);

        _cache.Remove(cacheKey);

        try
        {
            await _secretProvider.DeleteSecretAsync(secretName, ct)
                                 .ConfigureAwait(false);
        }
        catch (KeyVaultSecretException ex)
        {
            // Degraded mode: treat as best-effort delete on backing store.
            _logger.LogWarningWithDetails(ex,
                                          LobLogTemplates.LobCategoryEntity
                                          + "Failed to delete token secret '{SecretName}' from Key Vault.",
                                          new LobName(lobKey).Value,
                                          LogCategory,
                                          logEntity,
                                          secretName);
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Attempts to retrieve a valid token from memory cache.
    /// </summary>
    /// <param name="cacheKey">Computed cache key for token entry.</param>
    /// <param name="entry">Resolved entry when valid.</param>
    private bool TryGetValidFromMemory(string cacheKey, out GenesysTokenCacheEntry? entry)
    {
        entry = null;

        if (!_cache.TryGetValue(cacheKey, out GenesysTokenCacheEntry? cached) || !IsValid(cached))
        {
            _cache.Remove(cacheKey);

            return false;
        }

        entry = cached;

        return true;
    }

    /// <summary>
    /// Validates token payload completeness and expiration.
    /// </summary>
    private static bool IsValid(GenesysTokenCacheEntry? entry)
    {
        return entry is not null
               && !string.IsNullOrWhiteSpace(entry.AccessToken)
               && entry.ExpiresAtUtc > DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Builds in-memory cache key for a LOB token.
    /// </summary>
    private string BuildCacheKey(string lobKey)
    {
        return $"genesys:oauth:{_appEnvironment.Alias}:{lobKey}";
    }

    /// <summary>
    /// Builds Key Vault secret name for a LOB token.
    /// </summary>
    private string BuildTokenSecretName(string lobKey)
    {
        return $"{_keyVaultOptions.GenesysTokenSecretPrefix}-{_appEnvironment.Alias}-{lobKey}";
    }

    #endregion
}
