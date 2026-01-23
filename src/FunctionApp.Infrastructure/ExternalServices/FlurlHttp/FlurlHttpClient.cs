using System.Diagnostics;
using System.Text.Json;

using Flurl.Http;
using Flurl.Http.Configuration;

using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;
using Polly.Wrap;


namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

public class FlurlHttpClient : IFlurlHttpClient
{
    #region ========== *** Properties & Constructor *** ==========

    private readonly FlurlClient _client;
    private readonly FlurlClientOptions _options;
    private readonly ILogger _logger;
    private readonly Func<CancellationToken, Task<string?>>? _tokenProviderFunc;
    private readonly Func<CancellationToken, Task>? _refreshTokenFunc;
    private readonly AsyncPolicyWrap _safeMethodPolicy;
    private readonly AsyncPolicyWrap _unsafeMethodPolicy;

    public FlurlHttpClient(FlurlClient client,
                           IOptions<FlurlClientOptions> options,
                           ILogger logger,
                           Func<CancellationToken, Task<string?>>? tokenProviderFunc = null,
                           Func<CancellationToken, Task>? refreshTokenFunc = null)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _tokenProviderFunc = tokenProviderFunc;
        _refreshTokenFunc = refreshTokenFunc;

        // --- Core Flurl Configuration ---
        _client.Settings.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                                                                    {
                                                                        PropertyNamingPolicy =
                                                                            JsonNamingPolicy.CamelCase,
                                                                        PropertyNameCaseInsensitive = true
                                                                    });
        _client.Settings.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
        _client.BeforeCall(LogRequest);
        _client.AfterCall(LogResponse);
        _client.OnError(HandleError);

        // --- Resilience Policies ---
        AsyncCircuitBreakerPolicy circuitBreaker = BuildCircuitBreaker();
        IAsyncPolicy rateLimitPolicy = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "RateLimit");
        IAsyncPolicy safeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.SafeMethods, "Safe");
        IAsyncPolicy unsafeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.UnsafeMethods, "Unsafe");

        _safeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, safeRetryPolicy);
        _unsafeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, unsafeRetryPolicy);
    }

    public string BaseUrl => _client.BaseUrl;

    #endregion

    public async Task<T?> GetAsync<T>(string endpoint,
                                      Dictionary<string, string>? headers = null,
                                      CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_safeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                _logger.LogInformation("Sending GET request to {Endpoint}", endpoint);

                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                return await request.GetJsonAsync<T>(cancellationToken: ct)
                                                                    .ConfigureAwait(false);
                                            },
                                            endpoint,
                                            "GET",
                                            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                                 TRequest payload,
                                                                 Dictionary<string, string>? headers = null,
                                                                 CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                _logger.LogInformation("Sending POST request to {Endpoint}", endpoint);

                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                return await request.PostJsonAsync(payload, cancellationToken: ct)
                                                                    .ReceiveJson<TResponse>()
                                                                    .ConfigureAwait(false);
                                            },
                                            endpoint,
                                            "POST",
                                            cancellationToken);
    }

    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                                TRequest payload,
                                                                Dictionary<string, string>? headers = null,
                                                                CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                _logger.LogInformation("Sending PUT request to {Endpoint}", endpoint);

                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                return await request.PutJsonAsync(payload, cancellationToken: ct)
                                                                    .ReceiveJson<TResponse>()
                                                                    .ConfigureAwait(false);
                                            },
                                            endpoint,
                                            "PUT",
                                            cancellationToken);
    }

    public async Task<bool> DeleteAsync(string endpoint,
                                        Dictionary<string, string>? headers = null,
                                        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithPolicyAsync(_unsafeMethodPolicy,
                                            async (token, ct) =>
                                            {
                                                _logger.LogInformation("Sending DELETE request to {Endpoint}",
                                                                       endpoint);

                                                IFlurlRequest request = _client.Request(endpoint);

                                                ApplyHeadersAndAuth(request, headers, token);

                                                IFlurlResponse response = await request
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

    private static void ApplyHeadersAndAuth(IFlurlRequest request, Dictionary<string, string>? headers, string? token)
    {
        bool hasExternalAuth = false;

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.WithHeader(header.Key, header.Value);
                if (header.Key.Equals("Authorization", StringComparison.OrdinalIgnoreCase))
                {
                    hasExternalAuth = true;
                }
            }
        }

        if (!hasExternalAuth && !string.IsNullOrEmpty(token))
        {
            request.WithOAuthBearerToken(token);
        }
    }

    private async Task<T?> ExecuteWithPolicyAsync<T>(AsyncPolicyWrap policy,
                                                     Func<string?, CancellationToken, Task<T?>> operation,
                                                     string endpoint,
                                                     string method,
                                                     CancellationToken cancellationToken)
    {
        return await policy.ExecuteAsync(async ct =>
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
                                                     $"{method} request failed: {ex.Message}",
                                                     ex);
                                             }
                                         },
                                         cancellationToken);
    }

    private async Task<string?> GetTokenIfNeededAsync(CancellationToken cancellationToken)
    {
        if (_tokenProviderFunc != null) return await _tokenProviderFunc(cancellationToken);

        return null;
    }

    private IAsyncPolicy BuildRetryPolicy(RetryStrategy strategy, string methodType)
    {
        if (strategy.MaxAttempts == 0) return Policy.NoOpAsync();

        return Policy.Handle<FlurlHttpException>(ex => ShouldRetryOnStatusCode(ex.StatusCode, strategy.StatusCodes))
                     .Or<HttpRequestException>()
                     .WaitAndRetryAsync(strategy.MaxAttempts,
                                        retryAttempt =>
                                        {
                                            TimeSpan delay = strategy.UseExponentialBackoff
                                                ? TimeSpan.FromMilliseconds(
                                                    strategy.InitialDelay.TotalMilliseconds *
                                                    Math.Pow(2, retryAttempt - 1))
                                                : strategy.InitialDelay;

                                            return delay > strategy.MaxDelay ? strategy.MaxDelay : delay;
                                        },
                                        async (exception, timespan, retryCount, _) =>
                                        {
                                            string statusCode = exception is FlurlHttpException flurlEx
                                                ? flurlEx.StatusCode?.ToString() ?? "N/A"
                                                : "N/A";

                                            if (exception is FlurlHttpException { StatusCode: 401 } &&
                                                retryCount == 1 &&
                                                _refreshTokenFunc != null)
                                            {
                                                _logger.LogWarning("[{MethodType}] 401 Unauthorized - Refreshing token",
                                                                   methodType);

                                                await _refreshTokenFunc(CancellationToken.None);
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
        return Policy.Handle<FlurlHttpException>(ex => ShouldBreakOnStatusCode(ex.StatusCode))
                     .Or<HttpRequestException>()
                     .CircuitBreakerAsync(_options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                          _options.CircuitBreaker.DurationOfBreak,
                                          (exception, duration) =>
                                          {
                                              _logger.LogError(
                                                  "Circuit breaker opened for {Duration}s due to {ExceptionType}: {Message}",
                                                  duration.TotalSeconds,
                                                  exception.GetType().Name,
                                                  exception.Message);
                                          },
                                          () =>
                                          {
                                              _logger.LogInformation(
                                                  "Circuit breaker reset - normal operation resumed");
                                          },
                                          () =>
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
