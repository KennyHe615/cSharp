using Application.Abstractions.Context;

using Infrastructure.ExternalApis.Genesys.Tokens.Abstractions;
using Infrastructure.ExternalApis.Genesys.Tokens.Models;

using SharedKernel.Concurrency;


namespace Infrastructure.ExternalApis.Genesys.Tokens;

/// <summary>
/// Provides valid Genesys OAuth tokens with fallback chain:
/// memory cache -> Key Vault -> Genesys OAuth API.
/// </summary>
public sealed class GenesysTokenProvider : IGenesysTokenProvider
{
    #region ========== *** Properties and Constructor *** ==========

    private const int ExpirySafetySeconds = 300;
    private const int MinCacheSeconds = 60;

    private readonly ILobContext _lobContext;
    private readonly IGenesysTokenStore _tokenStore;
    private readonly IGenesysTokenApiClient _tokenApiClient;
    private readonly KeyedSemaphoreLock _keyedLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="GenesysTokenProvider"/> class.
    /// </summary>
    public GenesysTokenProvider(ILobContext lobContext,
                                IGenesysTokenStore tokenStore,
                                IGenesysTokenApiClient tokenApiClient,
                                KeyedSemaphoreLock keyedLock)
    {
        _lobContext = lobContext         ?? throw new InvalidOperationException("LobContext is not configured.");
        _tokenStore = tokenStore         ?? throw new ArgumentNullException(nameof(tokenStore));
        _tokenApiClient = tokenApiClient ?? throw new ArgumentNullException(nameof(tokenApiClient));
        _keyedLock = keyedLock           ?? throw new ArgumentNullException(nameof(keyedLock));
    }

    private string LobKey => _lobContext.LobName.Value;

    private string LockKey => $"genesys:oauth:lock:{LobKey}";

    #endregion

    /// <inheritdoc />
    public async Task<string> GetValidTokenAsync(CancellationToken ct = default)
    {
        GenesysTokenCacheEntry? entry = await _tokenStore.TryGetValidAsync(LobKey, ct).ConfigureAwait(false);

        if (entry is not null) return entry.AccessToken;

        await using IAsyncDisposable gate = await _keyedLock.AcquireAsync(LockKey, ct).ConfigureAwait(false);

        entry = await _tokenStore.TryGetValidAsync(LobKey, ct).ConfigureAwait(false);

        if (entry is not null) return entry.AccessToken;

        GenesysTokenCacheEntry fresh = await FetchFreshTokenAsync(ct).ConfigureAwait(false);
        await _tokenStore.UpsertAsync(LobKey, fresh, ct).ConfigureAwait(false);

        return fresh.AccessToken;
    }

    /// <inheritdoc />
    public async Task RefreshTokenAsync(CancellationToken ct = default)
    {
        await using IAsyncDisposable gate = await _keyedLock.AcquireAsync(LockKey, ct).ConfigureAwait(false);

        // Fetch first so we never replace a previously valid token unless refresh succeeds.
        GenesysTokenCacheEntry fresh = await FetchFreshTokenAsync(ct).ConfigureAwait(false);

        await _tokenStore.UpsertAsync(LobKey, fresh, ct).ConfigureAwait(false);
    }

    #region ========== *** Private Methods *** ==========

    private async Task<GenesysTokenCacheEntry> FetchFreshTokenAsync(CancellationToken ct)
    {
        string clientId = _lobContext.GenesysClientId;
        string clientSecret = _lobContext.GenesysClientSecret;

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException($"Missing Genesys OAuth credentials for LOB '{LobKey}'.");
        }

        GenesysTokenResponse response =
            await _tokenApiClient.RequestClientCredentialsTokenAsync(clientId, clientSecret, ct).ConfigureAwait(false);

        int ttlSeconds = Math.Max(MinCacheSeconds, response.ExpiresIn - ExpirySafetySeconds);
        DateTimeOffset expiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(ttlSeconds);

        return new GenesysTokenCacheEntry(response.AccessToken, expiresAtUtc);
    }

    #endregion
}
