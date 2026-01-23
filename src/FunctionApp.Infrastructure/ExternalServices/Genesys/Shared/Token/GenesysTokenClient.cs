using System.Text;

using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys.Shared.Token;

public class GenesysTokenClient(IFlurlHttpClientFactory factory, IOptions<GenesysOptions> genesysOptions)
{
    private readonly IFlurlHttpClient _client = factory.CreateClient(genesysOptions.Value.OAuthEndpoint);

    public async Task<GenesysTokenResponseDto?> FetchTokenAsync(string clientId,
                                                                string clientSecret,
                                                                CancellationToken cancellationToken = default)
    {
        string credentials = $"{clientId}:{clientSecret}";
        string base64Credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials));

        Dictionary<string, string> headers = new()
                                             {
                                                 { "Authorization", $"Basic {base64Credentials}" },
                                                 { "Content-Type", "application/x-www-form-urlencoded" }
                                             };

        return await _client.PostAsync<object, GenesysTokenResponseDto>("/oauth/token?grant_type=client_credentials",
                                                                        new { },
                                                                        headers,
                                                                        cancellationToken);
    }
}
