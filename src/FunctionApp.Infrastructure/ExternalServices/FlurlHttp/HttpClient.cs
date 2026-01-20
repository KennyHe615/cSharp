using System.Diagnostics;

using Flurl.Http;

using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public class HttpClient : IHttpClient
{
    private readonly FlurlClientOptions _options;
    private readonly ILogger<HttpClient> _logger;
    private readonly FlurlClient _client;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;

    public HttpClient(IOptions<FlurlClientOptions> options, ILogger<HttpClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // Configure for outbound operations (calling external services)
        _client = new FlurlClient(_options.BaseUrl);
        _client.Settings.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _client.BeforeCall(LogRequest);
        _client.AfterCall(LogResponse);
        _client.OnError(HandleError);

        // Apply retry policy for outbound operations
        _retryPolicy = Policy.Handle<FlurlHttpException>()
                             .Or<HttpRequestException>()
                             .WaitAndRetryAsync(retryCount: _options.RetryPolicyOptions.MaxAttempts,
                                                sleepDurationProvider: retryAttempt => _options.RetryPolicyOptions.UseExponentialBackoff
                                                                           ? TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
                                                                           : _options.RetryPolicyOptions.Delay, onRetry: (exception, timespan, retryCount, context) =>
                                                {
                                                    _logger.LogWarning("Retry {RetryCount} for outbound request after {Delay}ms due to {ExceptionType}: {Message}",
                                                                       retryCount, timespan.TotalMilliseconds, exception.GetType()
                                                                          .Name, exception.Message);
                                                });

        // Apply circuit breaker for outbound protection
        _circuitBreaker = Policy.Handle<HttpRequestException>()
                                .CircuitBreakerAsync(exceptionsAllowedBeforeBreaking: _options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                                     durationOfBreak: _options.CircuitBreaker.DurationOfBreak);
    }

    public string BaseUrl => _options.BaseUrl;

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async ct => await _retryPolicy.ExecuteAsync(async ct2 =>
                                                                                              {
                                                                                                  try
                                                                                                  {
                                                                                                      _logger.LogInformation("Sending outbound GET request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      return await _client.Request(endpoint)
                                                                                                         .WithOAuthBearerToken(_options.OAuthToken ?? string.Empty)
                                                                                                         .GetJsonAsync<T>(cancellationToken: ct2)
                                                                                                         .ConfigureAwait(false);
                                                                                                  }
                                                                                                  catch (Exception ex)
                                                                                                  {
                                                                                                      _logger.LogError(ex,
                                                                                                       "Failed to execute outbound GET request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      throw new
                                                                                                          HttpRequestException($"Outbound GET request failed: {ex.Message}",
                                                                                                           ex);
                                                                                                  }
                                                                                              }, ct)
                                                                                .ConfigureAwait(false), cancellationToken)
                                    .ConfigureAwait(false);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async ct => await _retryPolicy.ExecuteAsync(async ct2 =>
                                                                                              {
                                                                                                  try
                                                                                                  {
                                                                                                      _logger.LogInformation("Sending outbound POST request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      return await _client.Request(endpoint)
                                                                                                         .WithOAuthBearerToken(_options.OAuthToken ?? string.Empty)
                                                                                                         .PostJsonAsync(payload, cancellationToken: ct2)
                                                                                                         .ReceiveJson<TResponse>()
                                                                                                         .ConfigureAwait(false);
                                                                                                  }
                                                                                                  catch (Exception ex)
                                                                                                  {
                                                                                                      _logger.LogError(ex,
                                                                                                       "Failed to execute outbound POST request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      throw new
                                                                                                          HttpRequestException($"Outbound POST request failed: {ex.Message}",
                                                                                                           ex);
                                                                                                  }
                                                                                              }, ct)
                                                                                .ConfigureAwait(false), cancellationToken)
                                    .ConfigureAwait(false);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest payload, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async ct => await _retryPolicy.ExecuteAsync(async ct2 =>
                                                                                              {
                                                                                                  try
                                                                                                  {
                                                                                                      _logger.LogInformation("Sending outbound PUT request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      return await _client.Request(endpoint)
                                                                                                         .WithOAuthBearerToken(_options.OAuthToken ?? string.Empty)
                                                                                                         .PutJsonAsync(payload, cancellationToken: ct2)
                                                                                                         .ReceiveJson<TResponse>()
                                                                                                         .ConfigureAwait(false);
                                                                                                  }
                                                                                                  catch (Exception ex)
                                                                                                  {
                                                                                                      _logger.LogError(ex,
                                                                                                       "Failed to execute outbound PUT request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      throw new
                                                                                                          HttpRequestException($"Outbound PUT request failed: {ex.Message}",
                                                                                                           ex);
                                                                                                  }
                                                                                              }, ct)
                                                                                .ConfigureAwait(false), cancellationToken)
                                    .ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await _circuitBreaker.ExecuteAsync(async ct => await _retryPolicy.ExecuteAsync(async ct2 =>
                                                                                              {
                                                                                                  try
                                                                                                  {
                                                                                                      _logger
                                                                                                         .LogInformation("Sending outbound DELETE request to {Endpoint}",
                                                                                                           endpoint);

                                                                                                      IFlurlResponse? response = await _client.Request(endpoint)
                                                                                                         .WithOAuthBearerToken(_options.OAuthToken ?? string.Empty)
                                                                                                         .DeleteAsync(cancellationToken: ct2)
                                                                                                         .ConfigureAwait(false);

                                                                                                      _logger
                                                                                                         .LogInformation("Outbound DELETE request to {Endpoint} completed with status {StatusCode}",
                                                                                                           endpoint, response.StatusCode);

                                                                                                      return response.ResponseMessage.IsSuccessStatusCode;
                                                                                                  }
                                                                                                  catch (Exception ex)
                                                                                                  {
                                                                                                      _logger.LogError(ex,
                                                                                                       "Failed to execute outbound DELETE request to {Endpoint}",
                                                                                                       endpoint);

                                                                                                      throw new
                                                                                                          HttpRequestException($"Outbound DELETE request failed: {ex.Message}",
                                                                                                           ex);
                                                                                                  }
                                                                                              }, ct)
                                                                                .ConfigureAwait(false), cancellationToken)
                                    .ConfigureAwait(false);
    }

    private void LogRequest(FlurlCall call)
    {
        string correlationId = Activity.Current?.Id
                               ?? Guid.NewGuid()
                                      .ToString();
        _logger.LogDebug("Outbound HTTP Request [{CorrelationId}]: {Method} {Url}", correlationId, call.Request.Verb,
                         call.Request.Url);
    }

    private void LogResponse(FlurlCall call)
    {
        _logger.LogDebug("Outbound HTTP Response: {StatusCode} for {Method} {Url}", call.Response?.StatusCode, call.Request.Verb,
                         call.Request.Url);
    }

    private void HandleError(FlurlCall call)
    {
        _logger.LogWarning("Outbound HTTP Error: {StatusCode} for {Method} {Url} - {ErrorMessage}", call.Response?.StatusCode, call.Request.Verb,
                           call.Request.Url, call.Exception?.Message);
    }
}
