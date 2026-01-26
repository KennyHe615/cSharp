using System.Text;

using Flurl.Http;

using FunctionApp.Application.Shared.Context;
using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Shared.Token;

/// <summary>
/// Resilient client for Genesys OAuth token acquisition.
/// Inherits retry and circuit breaker logic from FlurlHttpClient.
/// </summary>
public class GenesysTokenClient(IOptions<MultiLobOptions> multiLobOptions,
                                IFlurlHttpClientFactory factory,
                                ILobContext lobContext,
                                ILogger<GenesysTokenClient> logger) : FlurlHttpClient(
    factory.GetOrAddClient(multiLobOptions.Value.GenesysOAuthEndpoint),
    factory,
    lobContext,
    logger)
{
    public async Task<GenesysTokenResponseDto?> FetchTokenAsync(string clientId,
                                                                string clientSecret,
                                                                CancellationToken cancellationToken = default)
    {
        string credentials = $"{clientId}:{clientSecret}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        Dictionary<string, string> headers = new()
                                             {
                                                 { "Authorization", $"Basic {base64Credentials}" }
                                             };

        try
        {
            // Uses the inherited PostUrlEncodedAsync which includes retries and circuit breaking
            return await PostUrlEncodedAsync<GenesysTokenResponseDto>("/oauth/token",
                                                                      new
                                                                      {
                                                                          grant_type = "client_credentials"
                                                                      },
                                                                      headers,
                                                                      cancellationToken);
        }
        catch (FlurlHttpException ex)
        {
            string? responseBody = await ex.GetResponseStringAsync();
            logger.LogError(ex,
                            "Genesys OAuth token request failed. Status: {StatusCode}. Response: {ResponseBody}",
                            ex.StatusCode,
                            responseBody);

            throw;
        }
    }
}
