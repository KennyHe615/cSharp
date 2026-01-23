using Flurl.Http;

using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public class FlurlHttpClientFactory(IOptions<FlurlClientOptions> options, ILogger<FlurlHttpClient> logger)
    : IFlurlHttpClientFactory
{
    public IFlurlHttpClient CreateClient(string baseUrl,
                                         Func<CancellationToken, Task<string?>>? tokenProvider = null,
                                         Func<CancellationToken, Task>? refreshToken = null)
    {
        return new FlurlHttpClient(new FlurlClient(baseUrl), options, logger, tokenProvider, refreshToken);
    }
}
