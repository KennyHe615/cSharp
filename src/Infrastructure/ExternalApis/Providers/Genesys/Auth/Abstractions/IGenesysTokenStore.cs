using Infrastructure.ExternalApis.Providers.Genesys.Auth.Contracts;


namespace Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;

/// <summary>
/// Persists and retrieves Genesys OAuth tokens across storage layers
/// (in-memory cache and backing secret store).
/// </summary>
public interface IGenesysTokenStore
{
    /// <summary>
    /// Attempts to retrieve a currently valid token for the specified LOB key.
    /// Implementations should prefer cache-first lookup, then fallback to backing store.
    /// </summary>
    /// <param name="lobKey">Line-of-business key used to resolve token scope.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A valid token entry when available; otherwise <c>null</c>.</returns>
    Task<GenesysTokenCacheEntry?> TryGetValidAsync(string lobKey, CancellationToken ct = default);

    /// <summary>
    /// Persists the provided token entry for the specified LOB key.
    /// Implementations should update cache and backing store.
    /// </summary>
    /// <param name="lobKey">Line-of-business key used to resolve token scope.</param>
    /// <param name="entry">Token payload to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpsertAsync(string lobKey, GenesysTokenCacheEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Removes token data for the specified LOB key from cache and backing store.
    /// </summary>
    /// <param name="lobKey">Line-of-business key used to resolve token scope.</param>
    /// <param name="ct">Cancellation token.</param>
    Task RemoveAsync(string lobKey, CancellationToken ct = default);
}
