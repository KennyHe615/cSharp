using Application.Shared.Providers;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// A caching decorator for <see cref="KeyVaultsSecretProvider"/> that implements <see cref="ISecretProvider"/>.
/// </summary>
/// <remarks>
/// This class reduces the number of calls to Azure Key Vault by storing secret values in an <see cref="IMemoryCache"/>.
/// It ensures that any updates or deletions invalidate the corresponding cache entries.
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
    /// <param name="innerProvider">The underlying secret provider for Key Vault operations.</param>
    /// <param name="cache">The memory cache instance for storing secret values.</param>
    /// <param name="options">Configuration options for Key Vault, including cache duration.</param>
    /// <param name="logger">The logger instance.</param>
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

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is <c>null</c> or whitespace.</exception>
    /// <exception cref="KeyVaultsException">Thrown when an error occurs during secret retrieval.</exception>
    public async Task<string?> TryGetSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        string cacheKey = GetCacheKey(secretName);

        if (_cache.TryGetValue(cacheKey, out string? cached) && !string.IsNullOrWhiteSpace(cached))
        {
            _logger.LogDebug("Using cached value for secret '{SecretName}'", secretName);

            return cached;
        }

        string? secret;
        try
        {
            secret = await _innerProvider.TryGetSecretAsync(secretName, ct).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to try-get Key Vault secret '{secretName}'.", ex);
        }

        if (string.IsNullOrWhiteSpace(secret))
        {
            // Not found (null) or empty value: do not cache.
            if (secret is not null)
            {
                _logger.LogWarning("Secret '{SecretName}' resolved to an empty value; skipping cache.", secretName);
            }

            return secret;
        }

        MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(GetCacheTtl());

        _cache.Set(cacheKey, secret, cacheEntryOptions);

        return secret;
    }

    /// <inheritdoc />
    /// <exception cref="KeyVaultsException">Thrown when the secret is not found or an error occurs.</exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        string? value = await TryGetSecretAsync(secretName, ct).ConfigureAwait(false);

        return value ?? throw new KeyVaultsException($"Secret '{secretName}' was not found in Key Vault.");
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="KeyVaultsException">Thrown when the upsert operation fails.</exception>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await _innerProvider.UpsertSecretAsync(secretName, value, ct).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to upsert Key Vault secret '{secretName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(secretName));
        }
    }

    /// <inheritdoc />
    /// <exception cref="KeyVaultsException">Thrown when the delete operation fails.</exception>
    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            await _innerProvider.DeleteSecretAsync(secretName, ct).ConfigureAwait(false);
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultsException($"Failed to delete Key Vault secret '{secretName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(secretName));
        }
    }

    /// <summary>
    /// Generates a standardized cache key for a given secret name.
    /// </summary>
    /// <param name="secretName">The name of the secret.</param>
    /// <returns>A string representing the cache key.</returns>
    public static string GetCacheKey(string secretName)
    {
        return $"KeyVaultsSecret:{secretName}";
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Calculates the Time-To-Live (TTL) for cache entries based on configuration, with a minimum floor.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing the cache duration.</returns>
    private TimeSpan GetCacheTtl()
    {
        int minutes = _options.CacheDurationMinutes;

        return TimeSpan.FromMinutes(Math.Max(60, minutes));
    }

    #endregion
}
