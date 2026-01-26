using System.Collections.Concurrent;
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

public class FlurlHttpClientFactory : IFlurlHttpClientFactory
{
    private readonly FlurlClientOptions _options;
    private readonly ILogger<FlurlHttpClient> _logger;
    private readonly ConcurrentDictionary<string, FlurlClient> _clients = new();
    private readonly AsyncPolicyWrap _safeMethodPolicy;
    private readonly AsyncPolicyWrap _unsafeMethodPolicy;
    public const string RefreshFuncKey = "RefreshTokenFunc";

    public FlurlHttpClientFactory(IOptions<FlurlClientOptions> options, ILogger<FlurlHttpClient> logger)
    {
        _options = options.Value;
        _logger = logger;

        // 1. Build Shared Policies once for the application lifetime
        AsyncCircuitBreakerPolicy circuitBreaker = BuildCircuitBreaker();
        IAsyncPolicy rateLimitPolicy = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "RateLimit");
        IAsyncPolicy safeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.SafeMethods, "Safe");
        IAsyncPolicy unsafeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.UnsafeMethods, "Unsafe");

        _safeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, safeRetryPolicy);
        _unsafeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, unsafeRetryPolicy);
    }

    public FlurlClient GetOrAddClient(string baseUrl)
    {
        return _clients.GetOrAdd(baseUrl,
                                 url =>
                                 {
                                     FlurlClient client = new(url);
                                     ConfigureClient(client);

                                     return client;
                                 });
    }

    public AsyncPolicyWrap GetSafePolicy()
    {
        return _safeMethodPolicy;
    }

    public AsyncPolicyWrap GetUnsafePolicy()
    {
        return _unsafeMethodPolicy;
    }

    #region ========== *** Private Methods / Polly Builders *** ==========

    private void ConfigureClient(FlurlClient client)
    {
        // Only configure if not already configured (prevent duplicate settings)
        if (client.Settings.JsonSerializer is not null) return;

        client.Settings.JsonSerializer = new DefaultJsonSerializer(new JsonSerializerOptions
                                                                   {
                                                                       PropertyNamingPolicy =
                                                                           JsonNamingPolicy.CamelCase,
                                                                       PropertyNameCaseInsensitive = true
                                                                   });
        client.Settings.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    private IAsyncPolicy BuildRetryPolicy(RetryStrategy strategy, string type)
    {
        if (strategy.MaxAttempts <= 0) return Policy.NoOpAsync();

        return Policy.Handle<FlurlHttpException>(ex => ShouldRetryOnStatusCode(ex.StatusCode, strategy.StatusCodes))
                     .Or<HttpRequestException>()
                     .WaitAndRetryAsync(strategy.MaxAttempts,
                                        attempt =>
                                        {
                                            TimeSpan delay = strategy.UseExponentialBackoff
                                                ? TimeSpan.FromMilliseconds(
                                                    strategy.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1))
                                                : strategy.InitialDelay;

                                            return delay > strategy.MaxDelay ? strategy.MaxDelay : delay;
                                        },
                                        async (ex, time, count, context) =>
                                        {
                                            // Handle 401 Refresh using the function passed via context
                                            if (ex is FlurlHttpException { StatusCode: 401 } &&
                                                count == 1 &&
                                                context.TryGetValue(RefreshFuncKey, out object? funcObj) &&
                                                funcObj is Func<CancellationToken, Task> refreshFunc)
                                            {
                                                _logger.LogWarning(
                                                    "[{Type}] 401 Unauthorized - Triggering token refresh.",
                                                    type);

                                                await refreshFunc(CancellationToken.None);
                                            }

                                            _logger.LogWarning(
                                                "[{Type}] Retry {Count}/{Max} after {Delay}ms due to: {Msg}",
                                                type,
                                                count,
                                                strategy.MaxAttempts,
                                                time.TotalMilliseconds,
                                                ex.Message);
                                        });
    }

    private AsyncCircuitBreakerPolicy BuildCircuitBreaker()
    {
        return Policy.Handle<FlurlHttpException>(ex => ShouldBreakOnStatusCode(ex.StatusCode))
                     .Or<HttpRequestException>()
                     .CircuitBreakerAsync(_options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                          _options.CircuitBreaker.DurationOfBreak,
                                          (exception, duration) =>
                                              _logger.LogError(
                                                  "Circuit Breaker OPEN for {Duration}s due to {ExceptionType}: {Message}",
                                                  duration.TotalSeconds,
                                                  exception.GetType().Name,
                                                  exception.Message),
                                          () => _logger.LogInformation("Circuit Breaker CLOSED (Normal operation)"),
                                          () => _logger.LogInformation(
                                              "Circuit Breaker HALF-OPEN (Testing connectivity)"));
    }

    private bool ShouldBreakOnStatusCode(int? statusCode)
    {
        return statusCode.HasValue && _options.CircuitBreaker.HandledStatusCodes.Contains(statusCode.Value);
    }

    private static bool ShouldRetryOnStatusCode(int? statusCode, IReadOnlyCollection<int> allowedCodes)
    {
        return statusCode.HasValue && allowedCodes.Contains(statusCode.Value);
    }

    #endregion
}
