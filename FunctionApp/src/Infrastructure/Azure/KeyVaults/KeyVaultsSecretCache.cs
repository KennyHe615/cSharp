using Application.Shared.Providers;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Extensions;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// Caching decorator for <see cref="KeyVaultsSecretProvider"/> that stores resolved secrets in an in-memory cache.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>
/// <description> Cache entries use an absolute expiration based on <see cref="KeyVaultsOptions.CacheDurationMinutes"/> (minimum 60 minutes). </description>
/// </item>
/// <item>
/// <description> Empty or whitespace secret values are not cached. </description>
/// </item>
/// <item>
/// <description> Upserts and deletes invalidate the corresponding cache entry. </description>
/// </item>
/// <item>
/// <description> Provider failures are normalized by wrapping non-<see cref="KeyVaultsException"/> exceptions into <see cref="KeyVaultsException"/>. </description>
/// </item>
/// </list>
/// </remarks>
internal sealed class KeyVaultsSecretCache : ISecretProvider
{
    private readonly KeyVaultsSecretProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<KeyVaultsSecretCache> _logger;
    private readonly KeyVaultsOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="KeyVaultsSecretCache"/> class.
    /// </summary>
    /// <param name="innerProvider">The underlying provider used to fetch/update/delete secrets.</param>
    /// <param name="cache">The in\-memory cache used for storing resolved secret values.</param>
    /// <param name="options">Configuration options controlling cache behavior.</param>
    /// <param name="logger">Logger instance for cache diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="innerProvider"/>, <paramref name="cache"/>, <paramref name="options"/>, or <paramref name="logger"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="options"/> has a null <see cref="IOptions{TOptions}.Value"/>.</exception>
    public KeyVaultsSecretCache(KeyVaultsSecretProvider innerProvider,
                                IMemoryCache cache,
                                IOptions<KeyVaultsOptions> options,
                                ILogger<KeyVaultsSecretCache> logger)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value ?? throw new ArgumentException("Options value cannot be null.", nameof(options));
    }

    /// <summary>
    /// Gets a secret value by name, using the cache when available.
    /// </summary>
    /// <param name="secretName">The Key Vault secret name.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resolved secret value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="KeyVaultsException">
    /// Thrown when the underlying provider fails and the exception is not already a <see cref="KeyVaultsException"/>.
    /// </exception>
    /// <remarks>
    /// If the underlying provider returns an empty/whitespace value, the result is returned as\-is and is not cached.
    /// </remarks>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        string cacheKey = GetCacheKey(secretName);

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            _logger.LogDebug("Using cached value for secret '{SecretName}'", secretName);

            return cached;
        }

        string secret;
        try
        {
            secret = await _innerProvider.GetSecretAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to get Key Vault secret '{normalizedName}'.", ex);
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            _logger.LogWarning("Secret '{SecretName}' resolved to an empty value; skipping cache.", normalizedName);

            return secret;
        }

        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(GetCacheTtl());
        _cache.Set(cacheKey, secret, cacheEntryOptions);

        return secret;
    }

    /// <summary>
    /// Creates or updates a secret value, then invalidates the cache entry for that secret.
    /// </summary>
    /// <param name="secretName">The Key Vault secret name.</param>
    /// <param name="value">The new secret value.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="KeyVaultsException">
    /// Thrown when the underlying provider fails and the exception is not already a <see cref="KeyVaultsException"/>.
    /// </exception>
    /// <remarks>
    /// Cache invalidation occurs in a <c>finally</c> block, so the cache entry is removed even when the upsert fails.
    /// </remarks>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await _innerProvider.UpsertSecretAsync(normalizedName, value, cancellationToken).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to upsert Key Vault secret '{normalizedName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(normalizedName));
        }
    }

    /// <summary>
    /// Deletes a secret by name, then invalidates the cache entry for that secret.
    /// </summary>
    /// <param name="secretName">The Key Vault secret name.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when <paramref name="cancellationToken"/> is canceled.</exception>
    /// <exception cref="KeyVaultsException">
    /// Thrown when the underlying provider fails and the exception is not already a <see cref="KeyVaultsException"/>.
    /// </exception>
    /// <remarks>
    /// Cache invalidation occurs in a <c>finally</c> block, so the cache entry is removed even when the delete fails.
    /// </remarks>
    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        try
        {
            await _innerProvider.DeleteSecretAsync(normalizedName, cancellationToken).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to delete Key Vault secret '{normalizedName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(normalizedName));
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Computes the cache TTL to apply to secret entries.
    /// </summary>
    /// <returns>An absolute expiration duration, with a minimum of 60 minutes.</returns>
    private TimeSpan GetCacheTtl()
    {
        int minutes = _options.CacheDurationMinutes;

        return TimeSpan.FromMinutes(Math.Max(60, minutes));
    }

    /// <summary>
    /// Builds the cache key for a given secret name.
    /// </summary>
    /// <param name="normalizedName">The secret name.</param>
    /// <returns>A stable cache key string.</returns>
    private static string GetCacheKey(string normalizedName)
    {
        return $"KeyVaultSecret:{normalizedName}";
    }

    #endregion
}
