namespace Infrastructure.ExternalApis.Genesys.Tokens.Abstractions;

/// <summary>
/// Provides OAuth access tokens for Genesys API calls.
/// </summary>
public interface IGenesysTokenProvider
{
    /// <summary>
    /// Gets a valid access token using this lookup order:
    /// memory cache -> Key Vault -> Genesys OAuth API.
    /// </summary>
    Task<string> GetValidTokenAsync(CancellationToken ct = default);

    /// <summary>
    /// Forces a token refresh from Genesys OAuth API,
    /// then updates both Key Vault and memory cache.
    /// </summary>
    Task RefreshTokenAsync(CancellationToken ct = default);
}
