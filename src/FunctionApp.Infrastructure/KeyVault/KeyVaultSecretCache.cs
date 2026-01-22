using FunctionApp.Application.Shared.Secrets;
using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.KeyVault;

internal sealed class KeyVaultSecretCache(KeyVaultSecretProvider innerProvider,
                                          IMemoryCache cache,
                                          IOptions<KeyVaultOptions> options,
                                          ILogger<KeyVaultSecretCache> logger) : ISecretProvider
{
    private readonly KeyVaultOptions _options = options.Value;

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
