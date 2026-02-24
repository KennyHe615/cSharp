using Infrastructure.ExternalApis.Genesys.Tokens.Models;


namespace Infrastructure.ExternalApis.Genesys.Tokens.Abstractions;

/// <summary>
/// Executes OAuth token requests against Genesys.
/// </summary>
public interface IGenesysTokenApiClient
{
    /// <summary>
    /// Requests a new access token using client credentials.
    /// </summary>
    Task<GenesysTokenResponse> RequestClientCredentialsTokenAsync(string clientId,
                                                                  string clientSecret,
                                                                  CancellationToken ct = default);
}
