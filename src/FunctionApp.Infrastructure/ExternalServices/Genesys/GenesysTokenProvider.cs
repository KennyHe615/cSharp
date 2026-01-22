using System.Text;

using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys;

public sealed class GenesysTokenProvider(IOptions<GenesysOptions> options,
                                         TokenHttpClient tokenHttpClient,
                                         IMemoryCache cache,
                                         ILogger<GenesysTokenProvider> logger) : ITokenProvider
{
    private readonly GenesysOptions _options = options.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private const string TokenCacheKey = "GenesysOAuthToken";

    public async Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            logger.LogDebug("Using cached Genesys token");

            return cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            if (cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            logger.LogInformation("Fetching new Genesys OAuth token");

            return await FetchAndCacheTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            logger.LogInformation("Refreshing Genesys OAuth token due to 401");

            cache.Remove(TokenCacheKey);

            await FetchAndCacheTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    private async Task<string> FetchAndCacheTokenAsync(CancellationToken cancellationToken)
    {
        string credentials = $"{_options.ClientId}:{_options.ClientSecret}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        Dictionary<string, string> headers = new()
                                             {
                                                 { "Authorization", $"Basic {base64Credentials}" },
                                                 { "Content-Type", "application/x-www-form-urlencoded" }
                                             };

        GenesysTokenResponseDto? tokenResponse = await tokenHttpClient.PostAsync<GenesysTokenResponseDto>(
            "/oauth/token?grant_type=client_credentials",
            headers,
            cancellationToken);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException("Failed to retrieve access token from Genesys");
        }

        TimeSpan cacheExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 300);
        cache.Set(TokenCacheKey, tokenResponse.AccessToken, cacheExpiration);

        logger.LogInformation("Genesys token cached successfully, expires in {ExpiresIn}s", tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }
}
