using Flurl.Http;

using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.Retry;


namespace FunctionApp.Infrastructure.ExternalServices.Genesys;

public sealed class TokenHttpClient
{
    private readonly ILogger<TokenHttpClient> _logger;
    private readonly FlurlClient _client;
    private readonly AsyncRetryPolicy _retryPolicy;

    public TokenHttpClient(IOptions<GenesysOptions> options, ILogger<TokenHttpClient> logger)
    {
        GenesysOptions options1 = options.Value;
        _logger = logger;

        _client = new FlurlClient(options1.OAuthEndpoint);
        _client.Settings.Timeout = TimeSpan.FromSeconds(30);

        _retryPolicy = Policy.Handle<FlurlHttpException>()
                             .Or<HttpRequestException>()
                             .WaitAndRetryAsync(3,
                                                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                                                (exception, timespan, retryCount, _) =>
                                                {
                                                    _logger.LogWarning(
                                                        "Token fetch retry {RetryCount}/3 after {Delay}ms | Exception: {Message}",
                                                        retryCount,
                                                        timespan.TotalMilliseconds,
                                                        exception.Message);
                                                });
    }

    public async Task<TResponse?> PostAsync<TResponse>(string endpoint,
                                                       Dictionary<string, string>? headers = null,
                                                       CancellationToken cancellationToken = default)
    {
        return await _retryPolicy.ExecuteAsync(async ct =>
                                               {
                                                   try
                                                   {
                                                       _logger.LogInformation(
                                                           "Fetching OAuth token from {Endpoint}",
                                                           endpoint);

                                                       IFlurlRequest request = _client.Request(endpoint);

                                                       if (headers != null)
                                                       {
                                                           request = headers.Aggregate(request,
                                                               (current, header) =>
                                                                   current.WithHeader(header.Key, header.Value));
                                                       }

                                                       return await request
                                                                    .PostAsync(new StringContent(string.Empty),
                                                                               cancellationToken: ct)
                                                                    .ReceiveJson<TResponse>()
                                                                    .ConfigureAwait(false);
                                                   }
                                                   catch (FlurlHttpException ex)
                                                   {
                                                       _logger.LogError(ex,
                                                                        "Token fetch failed | Endpoint: {Endpoint} | Status: {StatusCode}",
                                                                        endpoint,
                                                                        ex.StatusCode);

                                                       throw;
                                                   }
                                               },
                                               cancellationToken);
    }
}
