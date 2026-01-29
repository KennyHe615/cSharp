using Application.Shared.Providers;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Azure.KeyVaults;

internal sealed class KeyVaultsSecretCache(KeyVaultsSecretProvider innerProvider,
                                           IMemoryCache cache,
                                           IOptions<KeyVaultsOptions> options,
                                           ILogger<KeyVaultsSecretCache> logger) : ISecretProvider
{
    private readonly KeyVaultsOptions _options = options.Value;

    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        string cacheKey = GetCacheKey(secretName);

        if (cache.TryGetValue(cacheKey, out string? secret) && !string.IsNullOrEmpty(secret))
        {
            logger.LogDebug("Using cached value for secret '{SecretName}'", secretName);

            return secret;
        }

        secret = await innerProvider.GetSecretAsync(secretName, cancellationToken);

        MemoryCacheEntryOptions cacheEntryOptions =
            new MemoryCacheEntryOptions().SetAbsoluteExpiration(TimeSpan.FromMinutes(_options.CacheDurationMinutes));

        cache.Set(cacheKey, secret, cacheEntryOptions);

        logger.LogDebug("Secret '{SecretName}' cached for {Duration} minutes",
                        secretName,
                        _options.CacheDurationMinutes);

        return secret;
    }

    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        await innerProvider.UpsertSecretAsync(secretName, value, cancellationToken);
        cache.Remove(GetCacheKey(secretName));
        logger.LogDebug("Invalidated cache for upserted secret '{SecretName}'", secretName);
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        await innerProvider.DeleteSecretAsync(secretName, cancellationToken);
        cache.Remove(GetCacheKey(secretName));
        logger.LogDebug("Invalidated cache for deleted secret '{SecretName}'", secretName);
    }

    #region ========== *** Private Methods *** ==========

    private static string GetCacheKey(string secretName)
    {
        return $"Secret_{secretName}";
    }

    #endregion
}
