using System.Text;

using Application.Abstractions.Context;

using Infrastructure.ExternalApis.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Abstractions;
using Infrastructure.ExternalApis.Providers.Genesys.Auth.Contracts;
using Infrastructure.ExternalApis.Providers.Genesys.Configuration;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.ExternalApis.Providers.Genesys.Auth;

/// <summary>
/// HTTP client wrapper responsible only for calling Genesys OAuth token endpoint.
/// </summary>
public sealed class GenesysTokenApiClient : IGenesysTokenApiClient
{
    private const string OAuthGrantType = "client_credentials";

    private readonly HttpApiClient _oauthClient;

    public GenesysTokenApiClient(ILobContext lobContext,
                                 IHttpApiClientFactory httpApiClientFactory,
                                 IOptions<GenesysOptions> genesysOptions,
                                 ILogger<HttpApiClient> httpApiClientLogger)
    {
        ArgumentNullException.ThrowIfNull(lobContext);
        ArgumentNullException.ThrowIfNull(httpApiClientFactory);
        ArgumentNullException.ThrowIfNull(genesysOptions);
        ArgumentNullException.ThrowIfNull(httpApiClientLogger);

        GenesysOptions options =
            genesysOptions.Value ?? throw new InvalidOperationException("GenesysOptions is not configured.");

        _oauthClient =
            new HttpApiClient(httpApiClientFactory.GetOrAddClient(options.OAuthBaseUrl),
                              httpApiClientFactory,
                              lobContext,
                              httpApiClientLogger);
    }

    /// <inheritdoc />
    public async Task<GenesysTokenResponse> RequestClientCredentialsTokenAsync(
        string clientId,
        string clientSecret,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new ArgumentException("Genesys client id cannot be null, empty, or whitespace.", nameof(clientId));
        }

        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new ArgumentException("Genesys client secret cannot be null, empty, or whitespace.",
                                        nameof(clientSecret));
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

        return response;
    }
}
