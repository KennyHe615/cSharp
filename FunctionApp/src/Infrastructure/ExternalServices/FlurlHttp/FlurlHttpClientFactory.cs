using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.Json;

using Configuration.Options;

using Flurl.Http;
using Flurl.Http.Configuration;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;

using Shared.Extensions;


namespace Infrastructure.ExternalServices.FlurlHttp;

/// <summary>
/// Default implementation of <see cref="IFlurlHttpClientFactory"/> that caches <see cref="FlurlClient"/>
/// instances per base URL and builds shared Polly policies (circuit breaker, rate limiting, and retries).
/// </summary>
public sealed class FlurlHttpClientFactory : IFlurlHttpClientFactory
{
    private readonly FlurlClientOptions _options;
    private readonly ILogger<FlurlHttpClientFactory> _logger;
    private readonly ConcurrentDictionary<string, FlurlClient> _clients = new();
    private readonly IAsyncPolicy _safeMethodPolicy;
    private readonly IAsyncPolicy _unsafeMethodPolicy;

    /// <summary>
    /// Policy context key used to pass a token refresh callback for handling HTTP 401 during retries.
    /// </summary>
    public const string RefreshFuncKey = "RefreshTokenFunc";

    /// <summary>
    /// Initializes a new instance of the <see cref="FlurlHttpClientFactory"/> class and builds
    /// shared resiliency policies for the application lifetime.
    /// </summary>
    /// <param name="options">Flurl client and policy options.</param>
    /// <param name="logger">Logger used for policy diagnostics.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.</exception>
    public FlurlHttpClientFactory(IOptions<FlurlClientOptions> options, ILogger<FlurlHttpClientFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        // 1. Build Shared Policies once for the application lifetime
        AsyncCircuitBreakerPolicy circuitBreaker = BuildCircuitBreaker();
        IAsyncPolicy rateLimitPolicy = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "RateLimit");
        IAsyncPolicy safeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.SafeMethods, "Safe");
        IAsyncPolicy unsafeRetryPolicy = BuildRetryPolicy(_options.RetryStrategies.UnsafeMethods, "Unsafe");

        _safeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, safeRetryPolicy);
        _unsafeMethodPolicy = Policy.WrapAsync(circuitBreaker, rateLimitPolicy, unsafeRetryPolicy);
    }

    /// <summary>
    /// Gets an existing <see cref="FlurlClient"/> for the given base URL or creates and configures a new one.
    /// </summary>
    /// <param name="baseUrl">Base URL used as the cache key.</param>
    /// <returns>A configured <see cref="FlurlClient"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, empty, or whitespace.</exception>
    public FlurlClient GetOrAddClient(string baseUrl)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("Base URL must be provided.", nameof(baseUrl));
        }

        return _clients.GetOrAdd(baseUrl,
                                 url =>
                                 {
                                     FlurlClient client = new(url);
                                     ConfigureClient(client);

                                     return client;
                                 });
    }

    /// <summary>
    /// Gets the shared Polly policy used for safe/idempotent HTTP methods (e.g., GET, HEAD).
    /// </summary>
    /// <returns>A composed <see cref="IAsyncPolicy"/> used for safe HTTP operations.</returns>
    public IAsyncPolicy GetSafePolicy()
    {
        return _safeMethodPolicy;
    }

    /// <summary>
    /// Gets the shared Polly policy used for unsafe/non\-idempotent HTTP methods (e.g., POST, PUT, PATCH, DELETE).
    /// </summary>
    /// <returns>A composed <see cref="IAsyncPolicy"/> used for unsafe HTTP operations.</returns>
    public IAsyncPolicy GetUnsafePolicy()
    {
        return _unsafeMethodPolicy;
    }

    /// <summary>
    /// Disposes all cached <see cref="FlurlClient"/> instances and clears the cache.
    /// </summary>
    public void Dispose()
    {
        foreach (FlurlClient client in _clients.Values)
        {
            client.Dispose();
        }

        _clients.Clear();
    }

    #region ========== *** Private Methods / Polly Builders *** ==========

    /// <summary>
    /// Applies shared Flurl client settings (serializer + timeout) once per client instance.
    /// </summary>
    /// <param name="client">The client to configure.</param>
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

    /// <summary>
    /// Builds a retry policy for the provided <paramref name="strategy"/>.
    /// Handles <see cref="FlurlHttpException"/>, <see cref="ExternalServiceHttpException"/>, and <see cref="HttpRequestException"/>.
    /// </summary>
    /// <param name="strategy">Retry strategy options (max attempts, delays, handled status codes).</param>
    /// <param name="type">Label used in logs to identify the policy usage.</param>
    /// <returns>An <see cref="IAsyncPolicy"/> implementing wait\-and\-retry, or no\-op if disabled.</returns>
    /// <remarks>
    /// Special cases:
    /// \- HTTP 429 uses a fixed 2\-minute delay.
    /// \- HTTP 401 triggers a refresh callback on the first retry if one is present in the Polly context
    ///   under <see cref="RefreshFuncKey"/>.
    /// </remarks>
    private IAsyncPolicy BuildRetryPolicy(RetryStrategy strategy, string type)
    {
        if (strategy.MaxAttempts <= 0) return Policy.NoOpAsync();

        IReadOnlyCollection<int> allowedCodes = strategy.StatusCodes;

        return Policy.Handle<Exception>(ex => ShouldRetryOnStatusCode(GetStatusCode(ex), allowedCodes))
                     .Or<HttpRequestException>()
                     .WaitAndRetryAsync(strategy.MaxAttempts,
                                        (attempt, ex, _) =>
                                        {
                                            // SPECIAL CASE: Fixed 2-minute freeze for HTTP 429 (Rate Limited)
                                            int? statusCode = GetStatusCode(ex);

                                            if (statusCode == 429)
                                            {
                                                return TimeSpan.FromMinutes(2);
                                            }

                                            double baseMs = strategy.UseExponentialBackoff
                                                ? strategy.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)
                                                : strategy.InitialDelay.TotalMilliseconds;

                                            // Thread\-safe jitter (0\-99ms).
                                            int jitterMs = RandomNumberGenerator.GetInt32(0, 100);
                                            TimeSpan delay = TimeSpan.FromMilliseconds(baseMs + jitterMs);

                                            return delay > strategy.MaxDelay ? strategy.MaxDelay : delay;
                                        },
                                        async (ex, time, count, context) =>
                                        {
                                            // Handle 401 Refresh using the function passed via context
                                            int? statusCode = GetStatusCode(ex);

                                            // Instrumentation: prove whether the refresh callback is present in Context.
                                            bool hasRefresh =
                                                context.TryGetValue(RefreshFuncKey, out object? funcObj) &&
                                                funcObj is Func<CancellationToken, Task>;

                                            _logger.LogWarning(
                                                "[{Type}] onRetry {Count}/{Max} after {Delay}ms | status={Status} | hasRefresh={HasRefresh} | exType={ExType} | msg = {Msg}",
                                                type,
                                                count,
                                                strategy.MaxAttempts,
                                                time.TotalMilliseconds,
                                                statusCode,
                                                hasRefresh,
                                                ex.GetType().FullName,
                                                ex.Message);

                                            // Handle 401 refresh using the function passed via policy context.
                                            if (statusCode == 401 &&
                                                count == 1 &&
                                                hasRefresh &&
                                                funcObj is Func<CancellationToken, Task> refreshFunc)
                                            {
                                                _logger.LogDebug(
                                                    "[{Type}] 401 Unauthorized - Triggering token refresh.",
                                                    type);

                                                // Intentionally decoupled from request cancellation to allow refresh to complete.
                                                await refreshFunc(CancellationToken.None).ConfigureAwait(false);
                                            }
                                        });
    }

    /// <summary>
    /// Builds the shared circuit breaker policy.
    /// </summary>
    /// <returns>An <see cref="AsyncCircuitBreakerPolicy"/> configured from <see cref="FlurlClientOptions"/>.</returns>
    /// <remarks>
    /// The breaker will open for configured handled HTTP status codes and for network exceptions.
    /// </remarks>
    private AsyncCircuitBreakerPolicy BuildCircuitBreaker()
    {
        return Policy.Handle<FlurlHttpException>(ex => ShouldBreakOnStatusCode(ex.StatusCode))
                     .Or<ExternalServiceHttpException>(ex => ShouldBreakOnStatusCode((int)ex.StatusCode))
                     .Or<HttpRequestException>()
                     .CircuitBreakerAsync(_options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                          _options.CircuitBreaker.DurationOfBreak,
                                          (exception, duration) =>
                                          {
                                              _logger.LogErrorWithDetails(exception,
                                                                          "Circuit Breaker OPEN for {Duration}s.",
                                                                          duration.TotalSeconds);
                                          },
                                          () =>
                                          {
                                              _logger.LogInformation("Circuit Breaker CLOSED (Normal operation)");
                                          },
                                          () =>
                                          {
                                              _logger.LogInformation(
                                                  "Circuit Breaker HALF-OPEN (Testing connectivity)");
                                          });
    }

    /// <summary>
    /// Extracts the HTTP status code from an exception by unwrapping nested exceptions.
    /// </summary>
    /// <param name="ex">The exception to extract the status code from.</param>
    /// <returns>The HTTP status code if found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// This method handles both <see cref="FlurlHttpException"/> and <see cref="ExternalServiceHttpException"/>
    /// by traversing the exception chain to find the first occurrence of either type.
    /// </remarks>
    private static int? GetStatusCode(Exception ex)
    {
        // Unwrap: ExternalServiceHttpException can have an inner FlurlHttpException.
        // Also handle the reverse (FlurlHttpException with an inner ExternalServiceHttpException), just in case.
        while (true)
        {
            switch (ex)
            {
                case FlurlHttpException fx:
                    return fx.StatusCode;

                case ExternalServiceHttpException hx:
                    return (int)hx.StatusCode;

                default:
                    if (ex.InnerException is null) return null;
                    ex = ex.InnerException;

                    break;
            }
        }
    }

    /// <summary>
    /// Determines whether the circuit breaker should open for a given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code (if available).</param>
    /// <returns><c>true</c> if the status code is configured as breaker\-handled; otherwise <c>false</c>.</returns>
    private bool ShouldBreakOnStatusCode(int? statusCode)
    {
        return statusCode.HasValue && _options.CircuitBreaker.HandledStatusCodes.Contains(statusCode.Value);
    }

    /// <summary>
    /// Determines whether a retry should occur for a given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code (if available).</param>
    /// <param name="allowedCodes">Set of status codes eligible for retry.</param>
    /// <returns><c>true</c> if the status code is present in <paramref name="allowedCodes"/>; otherwise <c>false</c>.</returns>
    private static bool ShouldRetryOnStatusCode(int? statusCode, IReadOnlyCollection<int> allowedCodes)
    {
        return statusCode.HasValue && allowedCodes.Contains(statusCode.Value);
    }

    #endregion
}
