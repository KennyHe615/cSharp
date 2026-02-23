using System.Collections.Concurrent;
using System.Text;

using Application.Abstractions.Context;

using Infrastructure.ExternalApis.Genesys.Abstractions;
using Infrastructure.ExternalApis.Genesys.Models.Auth;
using Infrastructure.ExternalApis.Http;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Genesys;

/// <summary>
/// Phase-1 Genesys token provider.
/// Uses memory cache first, then fetches from Genesys OAuth API.
/// </summary>
public sealed class GenesysTokenProvider : IGenesysTokenProvider
{
    #region ========== *** Properties and Constructor *** ==========

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> TokenLocks =
        new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.OrdinalIgnoreCase);

    private const string OAuthGrantType = "client_credentials";
    private const int ExpirySafetySeconds = 300;
    private const int MinCacheSeconds = 60;

    private readonly ILobContext _lobContext;
    private readonly IMemoryCache _cache;
    private readonly HttpApiClient _oauthClient;

    public GenesysTokenProvider(ILobContext lobContext,
                                IMemoryCache cache,
                                IHttpApiClientFactory httpApiClientFactory,
                                IOptions<GenesysOptions> options,
                                ILogger<HttpApiClient> httpApiClientLogger)
    {
        _lobContext = lobContext ?? throw new InvalidOperationException("LobContext is not configured.");
        _cache = cache           ?? throw new ArgumentNullException(nameof(cache));
        ArgumentNullException.ThrowIfNull(httpApiClientFactory);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(httpApiClientLogger);

        GenesysOptions optionValue =
            options.Value ?? throw new InvalidOperationException("GenesysOptions is not configured.");

        _oauthClient = new HttpApiClient(httpApiClientFactory.GetOrAddClient(optionValue.OAuthBaseUrl),
                                         httpApiClientFactory,
                                         _lobContext,
                                         httpApiClientLogger);
    }

    #endregion

    /// <inheritdoc />
    public async Task<string> GetValidTokenAsync(CancellationToken ct = default)
    {
        if (TryGetValidFromMemory(out string token)) return token;

        SemaphoreSlim gate = TokenLocks.GetOrAdd(LobKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            if (TryGetValidFromMemory(out token)) return token;

            return await FetchAndCacheTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RefreshTokenAsync(CancellationToken ct = default)
    {
        SemaphoreSlim gate = TokenLocks.GetOrAdd(LobKey, _ => new SemaphoreSlim(1, 1));

        await gate.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            _cache.Remove(CacheKey);

            await FetchAndCacheTokenAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    #region ========== *** Private Methods *** ==========

    private string LobKey => _lobContext.LobName.Value;

    private string CacheKey => $"genesys:oauth:{LobKey}";

    /// <summary>
    /// Returns a non-expired token from memory cache.
    /// </summary>
    private bool TryGetValidFromMemory(out string token)
    {
        token = string.Empty;

        if (!_cache.TryGetValue(CacheKey, out GenesysTokenCacheEntry? entry) || entry is null) return false;

        if (entry.ExpiresAtUtc <= DateTimeOffset.UtcNow || string.IsNullOrWhiteSpace(entry.AccessToken))
        {
            _cache.Remove(CacheKey);

            return false;
        }

        token = entry.AccessToken;

        return true;
    }

    /// <summary>
    /// Fetches a fresh token from Genesys OAuth and stores it in memory cache.
    /// </summary>
    private async Task<string> FetchAndCacheTokenAsync(CancellationToken ct)
    {
        string clientId = _lobContext.GenesysClientId;
        string clientSecret = _lobContext.GenesysClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException($"Missing Genesys OAuth credentials for LOB '{LobKey}'.");
        }

        string rawCredentials = $"{clientId}:{clientSecret}";
        string basicCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(rawCredentials));

        Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                                             {
                                                 ["Authorization"] = $"Basic {basicCredentials}"
                                             };

        GenesysTokenResponse? response = await _oauthClient
                                              .PostUrlEncodedAsync<GenesysTokenResponse>(GenesysOptions.OAuthEndpoint,
                                                new { grant_type = OAuthGrantType },
                                                headers,
                                                ct)
                                              .ConfigureAwait(false);

        if (response is null || string.IsNullOrWhiteSpace(response.AccessToken))
        {
            throw new ExternalServiceHttpException(System.Net.HttpStatusCode.OK,
                                                   "POST",
                                                   GenesysOptions.OAuthEndpoint,
                                                   "Genesys OAuth returned an empty token payload.");
        }

        int ttlSeconds = Math.Max(MinCacheSeconds, response.ExpiresIn - ExpirySafetySeconds);
        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);

        GenesysTokenCacheEntry entry = new GenesysTokenCacheEntry(response.AccessToken, expiresAtUtc);
        _cache.Set(CacheKey, entry, expiresAtUtc);

        return entry.AccessToken;
    }

    #endregion
}
