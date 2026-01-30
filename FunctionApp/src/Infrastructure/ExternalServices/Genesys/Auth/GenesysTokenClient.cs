using System.Text;

using Application.Shared.Context;

using Configuration.Options;

using Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.Genesys.Auth;

/// <summary>
/// Client responsible for obtaining OAuth access tokens from Genesys.
/// Uses <see cref="FlurlHttpClient"/> with shared resiliency policies provided by <see cref="IFlurlHttpClientFactory"/>.
/// </summary>
public class GenesysTokenClient(IOptions<GenesysOptions> genesysOptions,
                                IFlurlHttpClientFactory factory,
                                ILobContext lobContext,
                                ILogger<GenesysTokenClient> logger)
{
    private readonly FlurlHttpClient _httpClient = new(
        factory.GetOrAddClient(genesysOptions.Value.OAuthEndpoint),
        factory,
        lobContext,
        logger);

    private const string Endpoint = "/oauth/token";

    /// <summary>
    /// Requests an OAuth token using the client credentials grant.
    /// Adds a Basic Authorization header built from <paramref name="clientId"/> and <paramref name="clientSecret"/>,
    /// and posts <c>grant\_type=client\_credentials</c> as a URL\-encoded form body.
    /// </summary>
    /// <param name="clientId">Genesys OAuth client id.</param>
    /// <param name="clientSecret">Genesys OAuth client secret.</param>
    /// <param name="cancellationToken">Cancellation token for the outgoing HTTP request.</param>
    /// <returns>The deserialized <see cref="GenesysTokenResponse"/>, or <c>null</c> if the response body is empty.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="clientId"/> or <paramref name="clientSecret"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ExternalServiceHttpException">
    /// Thrown when the Genesys OAuth request fails; the underlying engine logs raw response details.
    /// </exception>
    public async Task<GenesysTokenResponse?> FetchTokenAsync(string clientId,
                                                             string clientSecret,
                                                             CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("ClientId must be provided.", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ArgumentException("ClientSecret must be provided.", nameof(clientSecret));
        }

        string credentials = $"{clientId}:{clientSecret}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        Dictionary<string, string> headers = new()
                                             {
                                                 { "Authorization", $"Basic {base64Credentials}" }
                                             };

        try
        {
            return await _httpClient.PostUrlEncodedAsync<GenesysTokenResponse>(Endpoint,
                                                                               new
                                                                               {
                                                                                   grant_type = "client_credentials"
                                                                               },
                                                                               headers,
                                                                               cancellationToken);
        }
        catch (ExternalServiceHttpException ex)
        {
            // The engine (FlurlHttpClient) already logged the raw response and exception.
            // Log a high-level OAuth-specific failure signal here.
            logger.LogError(ex, "Genesys OAuth token request failed fundamentally. {ErrorMessage}", ex.ToJson());

            throw;
        }
    }
}
