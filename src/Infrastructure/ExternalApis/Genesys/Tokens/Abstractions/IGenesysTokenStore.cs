using Infrastructure.ExternalApis.Genesys.Tokens.Models;


namespace Infrastructure.ExternalApis.Genesys.Tokens.Abstractions;

/// <summary>
/// Persists and retrieves Genesys tokens from storage layers (memory cache + Key Vault).
/// </summary>
public interface IGenesysTokenStore
{
    /// <summary>
    /// Tries to load a valid token for the given LOB.
    /// Lookup order should be memory cache first, then Key Vault.
    /// </summary>
    Task<GenesysTokenCacheEntry?> TryGetValidAsync(string lobKey, CancellationToken ct = default);

    /// <summary>
    /// Upserts token to memory cache and Key Vault.
    /// </summary>
    Task UpsertAsync(string lobKey, GenesysTokenCacheEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Removes token from memory cache and Key Vault.
    /// </summary>
    Task RemoveAsync(string lobKey, CancellationToken ct = default);
}
