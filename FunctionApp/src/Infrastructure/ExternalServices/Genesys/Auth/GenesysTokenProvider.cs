using System.Collections.Concurrent;

using Application.Shared.Context;
using Application.Shared.Providers;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

using Shared.Constants;
using Shared.Extensions;

using KeyVaultsSecretCache = Infrastructure.Azure.KeyVaults.KeyVaultsSecretCache;


namespace Infrastructure.ExternalServices.Genesys.Auth;

/// <summary>
/// Provides a unified, thread-safe mechanism for acquiring and caching Genesys OAuth access tokens across different Lines of Business (LOBs).
/// </summary>
/// <remarks>
/// <para>
/// Implementation follows a tiered caching strategy:
/// <list type="bullet">
/// <item><b>In-Memory:</b> Immediate reuse within the same process via <see cref="IMemoryCache"/>.</item>
/// <item><b>Distributed Fallback:</b> Persists tokens to Key Vault via <see cref="ISecretProvider"/> to share across multiple function instances.</item>
/// <item><b>API Fetch:</b> Direct request to Genesys OAuth endpoint as the final source of truth.</item>
/// </list>
/// </para>
/// <para>
/// <b>Concurrency Management:</b>
/// To prevent "Thundering Herd" issues on the Genesys API and avoid redundant token generation ("Token Waste"),
/// this provider utilizes a static dictionary of <see cref="SemaphoreSlim"/> instances partitioned by LOB.
/// Since the provider is typically registered with a Scoped lifetime, the static nature of the locks ensures
/// that parallel Function triggers for the same LOB coordinate their API requests even across different dependency injection scopes.
/// </para>
/// </remarks>
public sealed class GenesysTokenProvider(ILobContext lobContext,
                                         GenesysTokenClient tokenClient,
                                         ISecretProvider secretProvider,
                                         IMemoryCache cache,
                                         ILogger<GenesysTokenProvider> logger) : ITokenProvider
{
    /// <summary>
    /// A static registry of semaphores to ensure thread-safety and serialized API access per Line of Business.
    /// This prevents multiple parallel synchronization tasks for the same LOB from simultaneously requesting new tokens.
    /// </summary>
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TokenLocks = new();

    private string LobName => lobContext.LobName;

    private string TokenSecretName => $"{KeyVaultsConstants.GenesysToken}-{LobName}";

    private string TokenCacheKey => KeyVaultsSecretCache.GetCacheKey(TokenSecretName);

    /// <summary>
    /// Retrieves a valid Genesys OAuth token, checking the local cache and Key Vault before fetching from the API.
    /// </summary>
    /// <remarks>
    /// This method implements double-check locking using the per-LOB semaphore to ensure that only the first thread
    /// to encounter a cache miss performs the expensive API fetch, while subsequent threads wait and then
    /// resolve the newly cached token.
    /// </remarks>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    public async Task<string> GetValidTokenAsync(CancellationToken ct = default)
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

        // Get or create a lock specific to this LOB
        SemaphoreSlim lobLock = TokenLocks.GetOrAdd(LobName, _ => new SemaphoreSlim(1, 1));
        await lobLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (cache.TryGetValue(TokenCacheKey, out cachedToken) && !string.IsNullOrEmpty(cachedToken))
            {
                return cachedToken;
            }

            // Step 2: Try to get from Key Vault (via ISecretProvider which handles its own local cache)
            try
            {
                // Use TryGet to treat "not found" as an acceptable case (for token ONLY; no exception, no warning log).
                string? kvToken = await secretProvider.TryGetSecretAsync(TokenSecretName, ct).ConfigureAwait(false);

                if (!string.IsNullOrEmpty(kvToken))
                {
                    // Cache locally for 1 hour as a safe baseline since we don't know the remaining TTL
                    cache.Set(TokenCacheKey, kvToken, TimeSpan.FromHours(1));

                    return kvToken;
                }

                logger.LogInformation(
                    "[LOB: {Lob}] Genesys token secret '{SecretName}' not found in Key Vault. Proceeding to API fetch.",
                    LobName,
                    TokenSecretName);
            }
            catch (Exception ex)
            {
                // Actual Key Vault failures (auth, throttling, network, etc.)
                logger.LogWarningWithDetails(ex,
                                             "[LOB: {Lob}] Non-critical error retrieving Genesys token from Key Vault. Falling back to API.",
                                             LobName);
            }

            return await FetchAndCacheTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            lobLock.Release();
        }
    }

    /// <summary>
    /// Explicitly invalidates the current cached token and forces a fresh fetch from the Genesys OAuth API.
    /// Typically called when an upstream request receives a 401 Unauthorized response.
    /// </summary>
    /// <remarks>
    /// This method shares the same per-LOB lock as <see cref="GetValidTokenAsync"/>. This ensures that while
    /// a 401 recovery is in progress, no other parallel requests for the same LOB attempt to use the invalid
    /// token or trigger redundant refresh cycles.
    /// </remarks>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>A task representing the asynchronous refresh operation.</returns>
    public async Task RefreshTokenAsync(CancellationToken ct = default)
    {
        SemaphoreSlim lobLock = TokenLocks.GetOrAdd(LobName, _ => new SemaphoreSlim(1, 1));
        await lobLock.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            logger.LogInformation("[LOB: {Lob}] Refreshing Genesys OAuth token due to 401", LobName);

            // Always invalidate first: a 401 means the current token is not usable even if it exists in cache.
            cache.Remove(TokenCacheKey);

            await FetchAndCacheTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            lobLock.Release();
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Performs the actual HTTP request to fetch a new token from Genesys, updates the local memory cache,
    /// and asynchronously persists the result to the secure secret store.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests.</param>
    /// <returns>The newly acquired access token string.</returns>
    /// <exception cref="ExternalServiceHttpException">
    /// Thrown if credentials are missing or if the Genesys API returns an error or malformed response.
    /// </exception>
    private async Task<string> FetchAndCacheTokenAsync(CancellationToken ct)
    {
        // Merge and Validate Credentials using MultiLobOptions for shared defaults
        string clientId = lobContext.GenesysClientId;
        string clientSecret = lobContext.GenesysClientSecret;
        const string oauthEndpoint = GenesysConstants.OAuthBaseUrl;

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
            await tokenClient.FetchTokenAsync(clientId, clientSecret, ct).ConfigureAwait(false);

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

        string token = tokenResponse.AccessToken;
        cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(cacheSeconds));

        // Persist to Key Vault for cross-instance sharing
        try
        {
            await secretProvider.UpsertSecretAsync(TokenSecretName, token, ct).ConfigureAwait(false);

            logger.LogDebug("[LOB: {Lob}] Genesys token updated in Key Vault ('{SecretName}')",
                            LobName,
                            TokenSecretName);
        }
        catch (Exception ex)
        {
            // Log as warning but don't fail the request; the local cache is still valid
            logger.LogWarningWithDetails(ex, "[LOB: {Lob}] Failed to update Genesys token in Key Vault.", LobName);
        }

        logger.LogInformation("[LOB: {Lob}] Genesys token refreshed and cached successfully. Expires in {ExpiresIn}s",
                              LobName,
                              tokenResponse.ExpiresIn);

        return token;
    }

    #endregion
}
