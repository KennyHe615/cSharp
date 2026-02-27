using Infrastructure.ExternalApis.Providers.Genesys.Auth.Contracts;


namespace Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;

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
