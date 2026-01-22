using System.ComponentModel.DataAnnotations;


namespace FunctionApp.Configuration.Options;

/// <summary>
/// Configuration options for the Flurl HTTP client
/// </summary>
public sealed class FlurlClientOptions
{
    public const string SectionName = "FlurlClient";

    [Range(1, 180, ErrorMessage = "Timeout must be between 1-180 seconds")]
    public int TimeoutSeconds { get; set; } = 60;

    public HttpMethodRetryStrategies RetryStrategies { get; set; } = new();

    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();
}

public sealed class HttpMethodRetryStrategies
{
    public RetryStrategy SafeMethods { get; set; } = new()
                                                     {
                                                         MaxAttempts = 3,
                                                         StatusCodes = [408, 502, 503, 504],
                                                         UseExponentialBackoff = true,
                                                         InitialDelay = TimeSpan.FromSeconds(1),
                                                         MaxDelay = TimeSpan.FromSeconds(30)
                                                     };

    public RetryStrategy RateLimited { get; set; } = new()
                                                     {
                                                         MaxAttempts = 3,
                                                         StatusCodes = [429],
                                                         UseExponentialBackoff = false,
                                                         InitialDelay = TimeSpan.FromMinutes(2),
                                                         MaxDelay = TimeSpan.FromMinutes(2)
                                                     };

    public RetryStrategy UnsafeMethods { get; set; } = new()
                                                       {
                                                           MaxAttempts = 1,
                                                           StatusCodes = [502, 503, 504],
                                                           UseExponentialBackoff = false,
                                                           InitialDelay = TimeSpan.FromSeconds(1),
                                                           MaxDelay = TimeSpan.FromSeconds(30)
                                                       };
}

public sealed class RetryStrategy
{
    [Range(0, 5, ErrorMessage = "Max attempts must be between 0-5")]
    public int MaxAttempts { get; set; } = 3;

    public IReadOnlyCollection<int> StatusCodes { get; set; } = [];

    public bool UseExponentialBackoff { get; set; } = true;

    [Range(type: typeof(TimeSpan),
           "00:00:01",
           "00:05:00",
           ErrorMessage = "Initial delay must be between 1 second and 5 minutes")]
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);

    [Range(type: typeof(TimeSpan),
           "00:00:01",
           "00:10:00",
           ErrorMessage = "Max delay must be between 1 second and 10 minutes")]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
}

public class CircuitBreakerOptions
{
    [Range(1, 20, ErrorMessage = "Exceptions allowed must be between 1-20")]
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    [Range(type: typeof(TimeSpan),
           "00:00:30",
           "00:10:00",
           ErrorMessage = "Break duration must be between 30 seconds and 10 minutes")]
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromMinutes(1);

    public IReadOnlyCollection<int> HandledStatusCodes { get; set; } = [500, 502, 503, 504];
}
