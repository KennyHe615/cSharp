using System.Security.Cryptography;

using Flurl.Http;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;


namespace Infrastructure.ExternalApis.Http.Policies;

/// <summary>
/// Builds HTTP resiliency policy pipelines from <see cref="HttpClientResilienceOptions"/>.
/// </summary>
public sealed class HttpResiliencePolicyFactory : IHttpResiliencePolicyFactory
{
    private readonly HttpClientResilienceOptions _options;
    private readonly ILogger<HttpResiliencePolicyFactory> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResiliencePolicyFactory"/> class.
    /// </summary>
    /// <param name="options">Resilience configuration options.</param>
    /// <param name="logger">Logger for retry/circuit-breaker diagnostics.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> or <paramref name="logger"/> is null.
    /// </exception>
    public HttpResiliencePolicyFactory(IOptions<HttpClientResilienceOptions> options,
                                       ILogger<HttpResiliencePolicyFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger         ?? throw new ArgumentNullException(nameof(logger));
        _options = options.Value ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public IAsyncPolicy CreateSafePolicy()
    {
        AsyncCircuitBreakerPolicy breaker = BuildCircuitBreaker();
        IAsyncPolicy rateLimit = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "rate-limit");
        IAsyncPolicy safeRetry = BuildRetryPolicy(_options.RetryStrategies.SafeMethods, "safe");

        return Policy.WrapAsync(breaker, rateLimit, safeRetry);
    }

    /// <inheritdoc />
    public IAsyncPolicy CreateUnsafePolicy()
    {
        AsyncCircuitBreakerPolicy breaker = BuildCircuitBreaker();
        IAsyncPolicy rateLimit = BuildRetryPolicy(_options.RetryStrategies.RateLimited, "rate-limit");
        IAsyncPolicy unsafeRetry = BuildRetryPolicy(_options.RetryStrategies.UnsafeMethods, "unsafe");

        return Policy.WrapAsync(breaker, rateLimit, unsafeRetry);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Builds a retry policy for the supplied strategy.
    /// </summary>
    /// <param name="strategy">Retry strategy settings.</param>
    /// <param name="kind">Policy label used in log messages.</param>
    /// <returns>The configured retry policy, or a no-op policy when retries are disabled.</returns>
    private IAsyncPolicy BuildRetryPolicy(RetryStrategy strategy, string kind)
    {
        if (strategy.MaxAttempts <= 0) return Policy.NoOpAsync();

        return Policy
              .Handle<Exception>(ex => ShouldRetry(ex, strategy.StatusCodes))
              .WaitAndRetryAsync(strategy.MaxAttempts,
                                 (attempt, ex, _) => CalculateDelay(attempt, ex, strategy),
                                 async (ex, delay, attempt, context) =>
                                 {
                                     string lob = context.TryGetValue(HttpPolicyContextKeys.Lob, out object? lobObj)
                                         ? lobObj?.ToString() ?? "unknown"
                                         : "unknown";

                                     int? status = GetStatusCode(ex);

                                     bool hasRefresh =
                                         context.TryGetValue(HttpPolicyContextKeys.RefreshFunc, out object? refreshObj)
                                         && refreshObj is Func<CancellationToken, Task>;

                                     _logger
                                        .LogWarning("HTTP retry | lob={Lob} kind={Kind} attempt={Attempt} delayMs={DelayMs} status={Status} hasRefresh={HasRefresh} exception={ExceptionType}",
                                                    lob,
                                                    kind,
                                                    attempt,
                                                    delay.TotalMilliseconds,
                                                    status?.ToString() ?? "none",
                                                    hasRefresh,
                                                    ex.GetType().Name);

                                     if (status     == 401
                                         && attempt == 1
                                         && refreshObj is Func<CancellationToken, Task> refreshFunc)
                                     {
                                         await refreshFunc(CancellationToken.None).ConfigureAwait(false);
                                     }
                                 });
    }

    /// <summary>
    /// Builds the shared circuit-breaker policy.
    /// </summary>
    /// <returns>An async circuit-breaker policy configured from <see cref="HttpClientResilienceOptions"/>.</returns>
    private AsyncCircuitBreakerPolicy BuildCircuitBreaker()
    {
        return Policy
              .Handle<Exception>(ex => ShouldBreak(GetStatusCode(ex)))
              .CircuitBreakerAsync(_options.CircuitBreaker.ExceptionsAllowedBeforeBreaking,
                                   _options.CircuitBreaker.DurationOfBreak,
                                   (ex, duration) =>
                                       _logger.LogError(ex,
                                                        "Circuit breaker OPEN for {BreakSeconds}s",
                                                        duration.TotalSeconds),
                                   () => _logger.LogInformation("Circuit breaker CLOSED"),
                                   () => _logger.LogInformation("Circuit breaker HALF-OPEN"));
    }

    /// <summary>
    /// Calculates retry delay with optional exponential backoff and jitter.
    /// </summary>
    /// <param name="attempt">Current retry attempt (1-based).</param>
    /// <param name="ex">The exception that triggered the retry.</param>
    /// <param name="strategy">Retry strategy settings.</param>
    /// <returns>Delay before next retry.</returns>
    private static TimeSpan CalculateDelay(int attempt, Exception ex, RetryStrategy strategy)
    {
        int? status = GetStatusCode(ex);

        if (status == 429) return TimeSpan.FromMinutes(2);

        double baseMs = strategy.UseExponentialBackoff
            ? strategy.InitialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1)
            : strategy.InitialDelay.TotalMilliseconds;

        int jitterMs = RandomNumberGenerator.GetInt32(0, 100);
        TimeSpan delay = TimeSpan.FromMilliseconds(baseMs + jitterMs);

        return delay > strategy.MaxDelay ? strategy.MaxDelay : delay;
    }

    /// <summary>
    /// Determines whether the circuit breaker should open for a given HTTP status code.
    /// </summary>
    /// <param name="statusCode">HTTP status code (if available).</param>
    /// <returns><c>true</c> if the status code is configured as breaker-handled; otherwise <c>false</c>.</returns>
    private bool ShouldBreak(int? statusCode)
    {
        return statusCode.HasValue && _options.CircuitBreaker.HandledStatusCodes.Contains(statusCode.Value);
    }

    /// <summary>
    /// Determines whether a retry should be attempted for the given exception.
    /// </summary>
    /// <param name="ex">The thrown exception.</param>
    /// <param name="allowedCodes">HTTP status codes eligible for retry.</param>
    /// <returns><c>true</c> if retry should occur; otherwise <c>false</c>.</returns>
    private static bool ShouldRetry(Exception ex, IReadOnlyCollection<int> allowedCodes)
    {
        int? status = GetStatusCode(ex);

        if (status.HasValue) return allowedCodes.Contains(status.Value);

        return ex switch
               {
                   FlurlHttpException { StatusCode: null } or HttpRequestException => true,

                   // Retry only if cancellation is not caused by user token cancellation.
                   TaskCanceledException tce => tce.CancellationToken == CancellationToken.None
                                                || !tce.CancellationToken.IsCancellationRequested,
                   _ => false
               };
    }

    /// <summary>
    /// Attempts to extract an HTTP status code from an exception chain.
    /// </summary>
    /// <param name="ex">The exception to inspect.</param>
    /// <returns>The status code when available; otherwise <c>null</c>.</returns>
    private static int? GetStatusCode(Exception ex)
    {
        Exception current = ex;

        while (true)
        {
            switch (current)
            {
                case FlurlHttpException flurlEx:
                    return flurlEx.StatusCode;

                case ExternalServiceHttpException { StatusCode: not null } externalEx:
                    return (int)externalEx.StatusCode.Value;

                case HttpRequestException { StatusCode: not null } httpEx:
                    return (int)httpEx.StatusCode.Value;

                default:
                    if (current.InnerException is null) return null;

                    current = current.InnerException;

                    break;
            }
        }
    }

    #endregion
}
