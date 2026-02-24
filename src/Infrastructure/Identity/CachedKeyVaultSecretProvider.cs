using Application.Abstractions.Identity;

using Infrastructure.Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using SharedKernel.Concurrency;


namespace Infrastructure.Identity;

/// <summary>
/// Caching decorator for <see cref="ISecretProvider"/> backed by <see cref="KeyVaultSecretProvider"/>.
/// </summary>
/// <remarks>
/// Behavior summary:
/// <list type="bullet">
/// <item><description>Reads follow cache-first lookup.</description></item>
/// <item><description>Cache misses use keyed async locking to prevent duplicate Key Vault calls.</description></item>
/// <item><description>Upsert/Delete always invalidate cache for the affected secret.</description></item>
/// </list>
/// </remarks>
public sealed class CachedKeyVaultSecretProvider : ISecretProvider
{
    private readonly KeyVaultSecretProvider _innerProvider;
    private readonly IMemoryCache _cache;
    private readonly IOptions<KeyVaultOptions> _options;
    private readonly ILogger<CachedKeyVaultSecretProvider> _logger;
    private readonly KeyedSemaphoreLock _keyedLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachedKeyVaultSecretProvider"/> class.
    /// </summary>
    public CachedKeyVaultSecretProvider(KeyVaultSecretProvider innerProvider,
                                        IMemoryCache cache,
                                        IOptions<KeyVaultOptions> options,
                                        ILogger<CachedKeyVaultSecretProvider> logger,
                                        KeyedSemaphoreLock keyedLock)
    {
        _innerProvider = innerProvider ?? throw new ArgumentNullException(nameof(innerProvider));
        _cache = cache                 ?? throw new ArgumentNullException(nameof(cache));
        _options = options             ?? throw new ArgumentNullException(nameof(options));
        _logger = logger               ?? throw new ArgumentNullException(nameof(logger));
        _keyedLock = keyedLock         ?? throw new ArgumentNullException(nameof(keyedLock));
    }

    /// <inheritdoc />
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when the secret does not exist or retrieval fails.
    /// </exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        string? value = await TryGetSecretAsync(secretName, ct).ConfigureAwait(false);

        return value ?? throw new KeyVaultSecretException($"Secret '{secretName}' was not found in Key Vault.");
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when secret resolution fails unexpectedly.
    /// </exception>
    public async Task<string?> TryGetSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        string currentKey = GetCacheKey(secretName);

        // Fast path: return cached secret without lock.
        if (TryGetFromCache(currentKey, out string? cached))
        {
            _logger.LogDebug("Using cached value for secret '{SecretName}'.", secretName);

            return cached;
        }

        // Slow path: acquire per-secret lock, then double-check cache.
        await using IAsyncDisposable gate = await _keyedLock.AcquireAsync(currentKey, ct).ConfigureAwait(false);

        if (TryGetFromCache(currentKey, out cached)) return cached;

        try
        {
            string? secret = await _innerProvider.TryGetSecretAsync(secretName, ct).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(secret))
            {
                // null => not found, empty => invalid value, both should not be cached
                return secret;
            }

            _cache.Set(currentKey, secret, BuildCacheEntryOptions(_options.Value.CacheDurationMinutes));

            return secret;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultSecretException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultSecretException($"Failed to resolve Key Vault secret '{secretName}'.", ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is null.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">Thrown when upsert fails.</exception>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await _innerProvider.UpsertSecretAsync(secretName, value, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultSecretException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultSecretException($"Failed to upsert Key Vault secret '{secretName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(secretName));
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">Thrown when delete fails.</exception>
    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        try
        {
            await _innerProvider.DeleteSecretAsync(secretName, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultSecretException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new KeyVaultSecretException($"Failed to delete Key Vault secret '{secretName}'.", ex);
        }
        finally
        {
            _cache.Remove(GetCacheKey(secretName));
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Attempts to read a non-empty value from cache.
    /// </summary>
    private bool TryGetFromCache(string cacheKey, out string? value)
    {
        value = null;

        if (!_cache.TryGetValue(cacheKey, out string? cached) || string.IsNullOrWhiteSpace(cached))
        {
            return false;
        }

        value = cached;

        return true;
    }

    /// <summary>
    /// Builds cache entry policy using configured absolute expiration.
    /// </summary>
    private static MemoryCacheEntryOptions BuildCacheEntryOptions(int cacheDurationMinutes)
    {
        int minutes = Math.Max(1, cacheDurationMinutes);

        return new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(minutes));
    }

    /// <summary>
    /// Builds cache key for a secret name.
    /// </summary>
    private static string GetCacheKey(string secretName)
    {
        return $"kv:secret:{secretName}";
    }

    #endregion
}
