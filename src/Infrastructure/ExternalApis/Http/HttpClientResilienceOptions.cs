using System.ComponentModel.DataAnnotations;


namespace Infrastructure.ExternalApis.Http;

/// <summary>
/// Configuration for outbound HTTP client behavior and resiliency policies.
/// </summary>
public sealed class HttpClientResilienceOptions
{
    /// <summary>
    /// Configuration section name.
    /// </summary>
    public const string SectionName = "HttpClientResilience";

    /// <summary>
    /// Global request timeout (seconds) for Flurl clients.
    /// </summary>
    [Range(1, 180, ErrorMessage = "Timeout must be between 1 and 180 seconds.")]
    public int TimeoutSeconds { get; set; } = 60;

    /// <summary>
    /// Retry strategy groups by HTTP method safety.
    /// </summary>
    [Required]
    public HttpMethodRetryStrategies RetryStrategies { get; set; } = new HttpMethodRetryStrategies();

    /// <summary>
    /// Circuit breaker settings.
    /// </summary>
    [Required]
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();
}

/// <summary>
/// Retry strategies for safe, rate-limited, and unsafe HTTP operations.
/// </summary>
public sealed class HttpMethodRetryStrategies
{
    /// <summary>
    /// Safe/idempotent methods (GET/HEAD).
    /// </summary>
    [Required]
    public RetryStrategy SafeMethods { get; set; } = new RetryStrategy
                                                     {
                                                         MaxAttempts = 3,
                                                         StatusCodes = [408, 502, 503, 504],
                                                         UseExponentialBackoff = true,
                                                         InitialDelay = TimeSpan.FromSeconds(1),
                                                         MaxDelay = TimeSpan.FromSeconds(30)
                                                     };

    /// <summary>
    /// Rate-limited responses (429).
    /// </summary>
    [Required]
    public RetryStrategy RateLimited { get; set; } = new RetryStrategy
                                                     {
                                                         MaxAttempts = 3,
                                                         StatusCodes = [429],
                                                         UseExponentialBackoff = false,
                                                         InitialDelay = TimeSpan.FromMinutes(2),
                                                         MaxDelay = TimeSpan.FromMinutes(2)
                                                     };

    /// <summary>
    /// Unsafe/non-idempotent methods (POST/PUT/PATCH/DELETE).
    /// </summary>
    [Required]
    public RetryStrategy UnsafeMethods { get; set; } = new RetryStrategy
                                                       {
                                                           MaxAttempts = 1,
                                                           StatusCodes = [502, 503, 504],
                                                           UseExponentialBackoff = false,
                                                           InitialDelay = TimeSpan.FromSeconds(1),
                                                           MaxDelay = TimeSpan.FromSeconds(30)
                                                       };
}

/// <summary>
/// Retry behavior configuration.
/// </summary>
public sealed class RetryStrategy
{
    /// <summary>
    /// Retry attempts. Use 0 to disable this strategy.
    /// </summary>
    [Range(0, 5, ErrorMessage = "Max attempts must be between 0 and 5.")]
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// HTTP status codes eligible for retry.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one status code must be configured.")]
    public IReadOnlyCollection<int> StatusCodes { get; set; } = [502, 503, 504];

    /// <summary>
    /// Whether retry delay uses exponential backoff.
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;

    /// <summary>
    /// Initial retry delay.
    /// </summary>
    [Range(typeof(TimeSpan),
              "00:00:01",
              "00:05:00",
              ErrorMessage = "Initial delay must be between 1 second and 5 minutes.")]
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum retry delay cap.
    /// </summary>
    [Range(typeof(TimeSpan),
              "00:00:01",
              "00:10:00",
              ErrorMessage = "Max delay must be between 1 second and 10 minutes.")]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

/// <summary>
/// Circuit breaker behavior configuration.
/// </summary>
public sealed class CircuitBreakerOptions
{
    /// <summary>
    /// Number of handled failures before opening the circuit.
    /// </summary>
    [Range(1, 20, ErrorMessage = "Exceptions allowed must be between 1 and 20.")]
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    /// <summary>
    /// Duration for which the circuit remains open.
    /// </summary>
    [Range(typeof(TimeSpan),
              "00:00:30",
              "00:10:00",
              ErrorMessage = "Break duration must be between 30 seconds and 10 minutes.")]
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// HTTP status codes that should contribute to circuit-breaker failures.
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one handled status code must be configured.")]
    public IReadOnlyCollection<int> HandledStatusCodes { get; set; } = [500, 502, 503, 504];
}
