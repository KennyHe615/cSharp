using Application.Shared.Context;
using Application.Shared.Providers;

using Azure;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.Genesys.Auth;

/// <summary>
/// Provides a unified, thread-safe mechanism for acquiring and caching Genesys OAuth access tokens across different Lines of Business (LOBs).
/// </summary>
/// <remarks>
/// Implementation follows a tiered caching strategy:
/// <list type="bullet">
/// <item><b>In-Memory:</b> Immediate reuse within the same process.</item>
/// <item><b>Distributed Fallback:</b> Persists tokens to a secret store (Key Vault) to share across multiple function instances.</item>
/// <item><b>API Fetch:</b> Direct request to Genesys OAuth endpoint as the final source of truth.</item>
/// </list>
/// </remarks>
public sealed class GenesysTokenProvider(IOptions<GenesysOptions> genesysOptions,
                                         ILobContext lobContext,
                                         GenesysTokenClient tokenClient,
                                         ISecretProvider secretProvider,
                                         IMemoryCache cache,
                                         ILogger<GenesysTokenProvider> logger) : ITokenProvider
{
    private readonly GenesysOptions _genesysOptions = genesysOptions.Value;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string LobName => lobContext.LobName;

    private string TokenCacheKey => $"GenesysOAuthToken_{LobName}";

    private string TokenSecretName => $"GenesysToken-{LobName}".NormalizeSecretName();

    /// <summary>
    /// Retrieves a valid Genesys OAuth token, checking the local cache and Key Vault before fetching from the API.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A valid JWT access token for Genesys API calls.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the LOB context is missing.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when token acquisition from Genesys fails.</exception>
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

        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            // Step 2: Try to get from Key Vault (via ISecretProvider which handles its own local cache)
            try
            {
                string kvToken = await secretProvider.GetSecretAsync(TokenSecretName, cancellationToken)
                                                     .ConfigureAwait(false);
                if (!string.IsNullOrEmpty(kvToken))
                {
                    logger.LogInformation("[LOB: {Lob}] Recovered Genesys token from Key Vault", LobName);

                    // Cache locally for 1 hour as a safe baseline since we don't know the remaining TTL
                    cache.Set(TokenCacheKey, kvToken, TimeSpan.FromHours(1));

                    return kvToken;
                }
            }
            catch (Exception ex) when (ex.GetBaseException() is RequestFailedException { Status: 404 })
            {
                logger.LogInformation(
                    "[LOB: {Lob}] Genesys token secret '{SecretName}' not found in Key Vault. Proceeding to API fetch.",
                    LobName,
                    TokenSecretName);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                                  "[LOB: {Lob}] Non-critical error retrieving Genesys token from Key Vault. Falling back to API. {ExJson}",
                                  LobName,
                                  ex.ToJson());
            }

            return await FetchAndCacheTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    /// <summary>
    /// Explicitly invalidates the current cached token and forces a fresh fetch from the Genesys OAuth API.
    /// Typically called when an upstream request receives a 401 Unauthorized response.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    public async Task RefreshTokenAsync(CancellationToken cancellationToken = default)
    {
        await _tokenLock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            logger.LogInformation("[LOB: {Lob}] Refreshing Genesys OAuth token due to 401", LobName);

            // Always invalidate first: a 401 means the current token is not usable even if it exists in cache.
            cache.Remove(TokenCacheKey);

            await FetchAndCacheTokenAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Performs the actual HTTP request to fetch a new token from Genesys, updates the local memory cache,
    /// and asynchronously persists the result to the secure secret store.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>The newly acquired access token string.</returns>
    /// <exception cref="ExternalServiceHttpException">
    /// Thrown if credentials are missing or if the Genesys API returns an error or malformed response.
    /// </exception>
    private async Task<string> FetchAndCacheTokenAsync(CancellationToken cancellationToken)
    {
        // Merge and Validate Credentials using MultiLobOptions for shared defaults
        string clientId = lobContext.GenesysClientId;
        string clientSecret = lobContext.GenesysClientSecret;
        string oauthEndpoint = _genesysOptions.OAuthEndpoint;

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(oauthEndpoint))
        {
            throw new ExternalServiceHttpException(System.Net.HttpStatusCode.BadRequest,
                                                   "POST",
                                                   oauthEndpoint,
                                                   $"Critical: [LOB: {LobName}] Genesys OAuth credentials (ClientId/Secret/Endpoint) are incomplete.");
        }

        GenesysTokenResponse? tokenResponse =
            await tokenClient.FetchTokenAsync(clientId, clientSecret, cancellationToken).ConfigureAwait(false);

        if (tokenResponse?.AccessToken == null)
        {
            throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                                   "POST",
                                                   oauthEndpoint,
                                                   $"[LOB: {LobName}] Genesys API returned success but no access token was found in the payload.",
                                                   null,
                                                   "Empty or malformed token response body");
        }

        // Cache for the duration specified by Genesys, minus a 5-minute safety margin
        int cacheSeconds = Math.Max(60, tokenResponse.ExpiresIn - 300);
        TimeSpan cacheExpiration = TimeSpan.FromSeconds(cacheSeconds);
        cache.Set(TokenCacheKey, tokenResponse.AccessToken, cacheExpiration);

        // Persist to Key Vault for cross-instance sharing
        try
        {
            await secretProvider.UpsertSecretAsync(TokenSecretName, tokenResponse.AccessToken, cancellationToken)
                                .ConfigureAwait(false);
            logger.LogDebug("[LOB: {Lob}] Genesys token updated in Key Vault ('{SecretName}')",
                            LobName,
                            TokenSecretName);
        }
        catch (Exception ex)
        {
            // Log as warning but don't fail the request; the local cache is still valid
            logger.LogWarning(ex,
                              "[LOB: {Lob}] Failed to update Genesys token in Key Vault. {ExJson}",
                              LobName,
                              ex.ToJson());
        }

        logger.LogInformation("[LOB: {Lob}] Genesys token refreshed and cached successfully. Expires in {ExpiresIn}s",
                              LobName,
                              tokenResponse.ExpiresIn);

        return tokenResponse.AccessToken;
    }

    #endregion
}
