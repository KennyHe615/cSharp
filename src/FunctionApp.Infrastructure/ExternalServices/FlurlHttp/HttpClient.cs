using System.Diagnostics;

using Flurl.Http;

using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public class HttpClient : IHttpClient
{
    #region ========== *** Properties & Constructor *** ==========

    private readonly FlurlClientOptions _options;
    private readonly ILogger<HttpClient> _logger;
    private readonly FlurlClient _client;
    private readonly AsyncPolicyWrap _safeMethodPolicy;
    private readonly AsyncPolicyWrap _unsafeMethodPolicy;
    private readonly ITokenProvider? _tokenProvider;

    public HttpClient(IOptions<FlurlClientOptions> options,
                      IOptions<GenesysOptions> genesysOptions,
                      ILogger<HttpClient> logger,
                      ITokenProvider? tokenProvider = null)
    {
        _options = options.Value;
        _logger = logger;
        _tokenProvider = tokenProvider;

        GenesysOptions genesys = genesysOptions.Value;
        _client = new FlurlClient(genesys.ApiEndpoint);
        _client.Settings.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _client.BeforeCall(LogRequest);
        _client.AfterCall(LogResponse);
        _client.OnError(HandleError);

        AsyncCircuitBreakerPolicy circuitBreaker = BuildCircuitBreaker();

        IAsyncPolicy rateLimitPolicy = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "RateLimit");
        IAsyncPolicy safeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.SafeMethods, "Safe");
        IAsyncPolicy unsafeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.UnsafeMethods, "Unsafe");

        _safeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, safeRetryPolicy);
        _unsafeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, unsafeRetryPolicy);
    }

    public string BaseUrl => _client.BaseUrl;

    #endregion

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_safeMethodPolicy,
                                            operation: async (token, ct) =>
                                                       {
                                                           _logger.LogInformation(
                                                               "Sending GET request to {Endpoint}",
                                                               endpoint);

                                                           return await _client.Request(endpoint)
                                                                               .WithOAuthBearerToken(
                                                                                   token: token ?? string.Empty)
                                                                               .GetJsonAsync<T>(cancellationToken: ct)
                                                                               .ConfigureAwait(false);
                                                       },
                                            endpoint,
                                            "GET",
                                            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                                 TRequest payload,
                                                                 CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            operation: async (token, ct) =>
                                                       {
                                                           _logger.LogInformation(
                                                               "Sending POST request to {Endpoint}",
                                                               endpoint);

                                                           return await _client.Request(endpoint)
                                                                               .WithOAuthBearerToken(
                                                                                   token: token ?? string.Empty)
                                                                               .PostJsonAsync(
                                                                                   payload,
                                                                                   cancellationToken: ct)
                                                                               .ReceiveJson<TResponse>()
                                                                               .ConfigureAwait(false);
                                                       },
                                            endpoint,
                                            "POST",
                                            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                                TRequest payload,
                                                                CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            operation: async (token, ct) =>
                                                       {
                                                           _logger.LogInformation(
                                                               "Sending PUT request to {Endpoint}",
                                                               endpoint);

                                                           return await _client.Request(endpoint)
                                                                               .WithOAuthBearerToken(
                                                                                   token: token ?? string.Empty)
                                                                               .PutJsonAsync(
                                                                                   payload,
                                                                                   cancellationToken: ct)
                                                                               .ReceiveJson<TResponse>()
                                                                               .ConfigureAwait(false);
                                                       },
                                            endpoint,
                                            "PUT",
                                            cancellationToken);
    }

    public async Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            operation: async (token, ct) =>
                                                       {
                                                           _logger.LogInformation(
                                                               "Sending DELETE request to {Endpoint}",
                                                               endpoint);

                                                           IFlurlResponse response = await _client.Request(endpoint)
                                                               .WithOAuthBearerToken(token: token ?? string.Empty)
                                                               .DeleteAsync(cancellationToken: ct)
                                                               .ConfigureAwait(false);

                                                           _logger.LogInformation(
                                                               "DELETE request completed | Endpoint: {Endpoint} | Status: {StatusCode}",
                                                               endpoint,
                                                               response.StatusCode);

                                                           return response.ResponseMessage.IsSuccessStatusCode;
                                                       },
                                            endpoint,
                                            "DELETE",
                                            cancellationToken);
    }

    #region ========== *** Private Methods *** ==========

    private async Task<T?> ExecuteWithPolicyAsync<T>(AsyncPolicyWrap policy,
                                                     Func<string?, CancellationToken, Task<T?>> operation,
                                                     string endpoint,
                                                     string method,
                                                     CancellationToken cancellationToken)
    {
        return await policy.ExecuteAsync(action: async ct =>
                                                 {
                                                     try
                                                     {
                                                         string? currentToken = await GetTokenIfNeededAsync(ct);

                                                         return await operation(currentToken, ct);
                                                     }
                                                     catch (FlurlHttpException ex)
                                                     {
                                                         _logger.LogError(ex,
                                                                          "{Method} request failed | Endpoint: {Endpoint} | Status: {StatusCode}",
                                                                          method,
                                                                          endpoint,
                                                                          ex.StatusCode);

                                                         throw;
                                                     }
                                                     catch (Exception ex)
                                                     {
                                                         _logger.LogError(ex,
                                                                          "{Method} request failed | Endpoint: {Endpoint}",
                                                                          method,
                                                                          endpoint);

                                                         throw new HttpRequestException(
                                                             message: $"{method} request failed: {ex.Message}",
                                                             ex);
                                                     }
                                                 },
                                         cancellationToken);
    }

    private async Task<string?> GetTokenIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_tokenProvider != null) return await _tokenProvider.GetValidTokenAsync(cancellationToken);

        return null;
    }

    private IAsyncPolicy BuildRetryPolicy(RetryStrategy strategy, string methodType)
    {
        if (strategy.MaxAttempts == 0) return Policy.NoOpAsync();

        return Policy
               .Handle<FlurlHttpException>(
                   exceptionPredicate: ex => ShouldRetryOnStatusCode(ex.StatusCode, strategy.StatusCodes))
               .Or<HttpRequestException>()
               .WaitAndRetryAsync(strategy.MaxAttempts,
                                  sleepDurationProvider: retryAttempt =>
                                                         {
                                                             TimeSpan delay = strategy.UseExponentialBackoff
                                                                 ? TimeSpan.FromMilliseconds(
                                                                     value: strategy.InitialDelay.TotalMilliseconds *
                                                                            Math.Pow(2, y: retryAttempt - 1))
                                                                 : strategy.InitialDelay;

                                                             return delay > strategy.MaxDelay
                                                                 ? strategy.MaxDelay
                                                                 : delay;
                                                         },
                                  onRetry: (exception, timespan, retryCount, _) =>
                                           {
                                               string statusCode = exception is FlurlHttpException flurlEx
                                                   ? flurlEx.StatusCode?.ToString() ?? "N/A"
                                                   : "N/A";

                                               if (exception is FlurlHttpException { StatusCode: 401 } &&
                                                   retryCount == 1 &&
                                                   _tokenProvider != null)
                                               {
                                                   _logger.LogWarning(
                                                       "[{MethodType}] 401 Unauthorized - Refreshing token",
                                                       methodType);

                                                   _tokenProvider.RefreshTokenAsync(CancellationToken.None)
                                                                 .GetAwaiter()
                                                                 .GetResult();
                                               }

                                               _logger.LogWarning(
                                                   "[{MethodType}] Retry {RetryCount}/{MaxAttempts} after {Delay}ms | Status: {StatusCode} | Exception: {ExceptionType} | Message: {Message}",
                                                   methodType,
                                                   retryCount,
                                                   strategy.MaxAttempts,
                                                   timespan.TotalMilliseconds,
                                                   statusCode,
                                                   exception.GetType().Name,
                                                   exception.Message);
                                           });
    }

    private AsyncCircuitBreakerPolicy BuildCircuitBreaker()
    {
        return Policy.Handle<FlurlHttpException>(exceptionPredicate: ex => ShouldBreakOnStatusCode(ex.StatusCode))
                     .Or<HttpRequestException>()
                     .CircuitBreakerAsync(_options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                          _options.CircuitBreaker.DurationOfBreak,
                                          onBreak: (exception, duration) =>
                                                   {
                                                       _logger.LogError(
                                                           "Circuit breaker opened for {Duration}s due to {ExceptionType}: {Message}",
                                                           duration.TotalSeconds,
                                                           exception.GetType().Name,
                                                           exception.Message);
                                                   },
                                          onReset: () =>
                                                   {
                                                       _logger.LogInformation(
                                                           "Circuit breaker reset - normal operation resumed");
                                                   },
                                          onHalfOpen: () =>
                                                      {
                                                          _logger.LogInformation(
                                                              "Circuit breaker half-open - testing service availability");
                                                      });
    }

    private static bool ShouldRetryOnStatusCode(int? statusCode, IReadOnlyCollection<int> allowedCodes)
    {
        return statusCode.HasValue && allowedCodes.Contains(statusCode.Value);
    }

    private bool ShouldBreakOnStatusCode(int? statusCode)
    {
        return statusCode.HasValue && _options.CircuitBreaker.HandledStatusCodes.Contains(statusCode.Value);
    }

    private void LogRequest(FlurlCall call)
    {
        string correlationId = Activity.Current?.Id ?? Guid.NewGuid().ToString();
        _logger.LogDebug("HTTP Request [{CorrelationId}]: {Method} {Url}",
                         correlationId,
                         call.Request.Verb,
                         call.Request.Url);
    }

    private void LogResponse(FlurlCall call)
    {
        _logger.LogDebug("HTTP Response: {StatusCode} for {Method} {Url}",
                         call.Response?.StatusCode,
                         call.Request.Verb,
                         call.Request.Url);
    }

    private void HandleError(FlurlCall call)
    {
        _logger.LogWarning("HTTP Error: {StatusCode} for {Method} {Url} - {ErrorMessage}",
                           call.Response?.StatusCode,
                           call.Request.Verb,
                           call.Request.Url,
                           call.Exception?.Message);
    }

    #endregion
}
