using FunctionApp.Application.Shared.Context;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Shared.Token;

public sealed class GenesysTokenProvider(IOptions<MultiLobOptions> multiLobOptions,
                                         ILobContext lobContext,
                                         GenesysTokenClient tokenClient,
                                         IMemoryCache cache,
                                         ILogger<GenesysTokenProvider> logger) : ITokenProvider
{
    private readonly MultiLobOptions _multiLobOptions = multiLobOptions.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string TokenCacheKey => $"GenesysOAuthToken_{lobContext.LobName}";

    private string? LobName => lobContext.LobName;

    public async Task<string> GetValidTokenAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(LobName))
        {
            throw new InvalidOperationException("LOB context is not set.");
        }

        if (cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
        {
            logger.LogDebug("[LOB: {Lob}] Using cached Genesys token", LobName);

            return cachedToken;
        }

        await _tokenLock.WaitAsync(cancellationToken);

        try
        {
            if (cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

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
            // Double-check: If the cache already has a token, it means another
            // concurrent request already performed the refresh while we were waiting.
            if (cache.TryGetValue(TokenCacheKey, out string? cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                logger.LogDebug("[LOB: {Lob}] Token was already refreshed by another concurrent request.", LobName);

                return;
            }

            logger.LogInformation("[LOB: {Lob}] Refreshing Genesys OAuth token due to 401", LobName);

            cache.Remove(TokenCacheKey);

            await FetchAndCacheTokenAsync(cancellationToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    #region ========== *** Private Methods *** ==========

    private async Task<string> FetchAndCacheTokenAsync(CancellationToken cancellationToken)
    {
        LobSettings? settings = lobContext.LobSettings;

        if (settings == null)
        {
            throw new InvalidOperationException($"Critical: [LOB: {LobName}] Configuration not found in context.");
        }

        // Merge and Validate Credentials using MultiLobOptions for shared defaults
        string clientId = settings.GenesysClientId;
        string clientSecret = settings.GenesysClientSecret;
        string oauthEndpoint = _multiLobOptions.GenesysOAuthEndpoint;

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(oauthEndpoint))
        {
            throw new InvalidOperationException(
                $"Critical: [LOB: {LobName}] Genesys OAuth credentials (ClientId/Secret/Endpoint) are incomplete.");
        }

        GenesysTokenResponseDto? tokenResponse =
            await tokenClient.FetchTokenAsync(clientId, clientSecret, cancellationToken);

        if (tokenResponse?.AccessToken == null)
        {
            throw new InvalidOperationException($"[LOB: {LobName}] Failed to retrieve access token from Genesys");
        }

        // Cache for the duration specified by Genesys, minus a 5-minute safety margin
        TimeSpan cacheExpiration = TimeSpan.FromSeconds(tokenResponse.ExpiresIn - 300);
        cache.Set(TokenCacheKey, tokenResponse.AccessToken, cacheExpiration);

        logger.LogInformation("[LOB: {Lob}] Genesys token refreshed and cached successfully. Expires in {ExpiresIn}s",
                              LobName,
                              tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }

    #endregion
}
