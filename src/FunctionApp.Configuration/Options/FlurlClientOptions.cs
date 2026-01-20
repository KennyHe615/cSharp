using System.ComponentModel.DataAnnotations;


namespace FunctionApp.Configuration.Options;

/// <summary>
/// Configuration options for the Flurl HTTP client
/// </summary>
public sealed class FlurlClientOptions
{
    public const string SectionName = "FlurlClient";

    // General/shared configuration fields
    [Required(ErrorMessage = "Base URL is required")]
    [Url(ErrorMessage = "Must be a valid URL")]
    public string BaseUrl { get; set; } = string.Empty;

    [Range(1, 180, ErrorMessage = "Timeout must be between 1-180 seconds")]
    public int TimeoutSeconds { get; set; } = 60;

    public string? OAuthToken { get; set; }

    public FlurlRetryPolicyOptions RetryPolicyOptions { get; set; } = new FlurlRetryPolicyOptions();

    // Circuit breaker settings for outbound requests
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new CircuitBreakerOptions();
}

public sealed class FlurlRetryPolicyOptions
{
    [Range(1, 5, ErrorMessage = "Max attempts must be between 1-5")]
    public int MaxAttempts { get; set; } = 3;

    [Range(typeof(TimeSpan), "00:00:01", "00:05:00",
              ErrorMessage = "Delay must be between 1 second and 5 minutes")]
    public TimeSpan Delay { get; set; } = TimeSpan.FromSeconds(2);

    [Range(typeof(TimeSpan), "00:00:01", "00:10:00",
              ErrorMessage = "Max delay must be between 1 second and 10 minutes")]
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    public bool UseExponentialBackoff { get; set; } = true;

    public IReadOnlyCollection<int> RetryStatusCodes { get; set; } = [401, 429, 502, 503, 504];

    public Dictionary<string, RetryScenario> NamedRetryPolicies { get; set; } = new Dictionary<string, RetryScenario>
                                                                                {
                                                                                    ["Idempotent"] = new RetryScenario
                                                                                                     {
                                                                                                         MaxAttempts = 3,
                                                                                                         StatusCodes =
                                                                                                         [
                                                                                                             408, 429, 502, 503,
                                                                                                             504
                                                                                                         ],
                                                                                                         UseExponentialBackoff = true
                                                                                                     },
                                                                                    ["NonIdempotent"] = new RetryScenario
                                                                                                        {
                                                                                                            MaxAttempts = 1,
                                                                                                            StatusCodes = [502, 503, 504]
                                                                                                        }
                                                                                };
}

public class RetryScenario
{
    public int MaxAttempts { get; set; }

    public IReadOnlyCollection<int> StatusCodes { get; set; } = [];

    public bool UseExponentialBackoff { get; set; }

    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
}

public class CircuitBreakerOptions
{
    [Range(1, 20, ErrorMessage = "Exceptions allowed must be between 1-20")]
    public int ExceptionsAllowedBeforeBreaking { get; set; } = 5;

    [Range(typeof(TimeSpan), "00:00:30", "00:10:00",
              ErrorMessage = "Break duration must be between 30 seconds and 10 minutes")]
    public TimeSpan DurationOfBreak { get; set; } = TimeSpan.FromMinutes(1);

    public IReadOnlyCollection<int> HandledStatusCodes { get; set; } = [500, 502, 503, 504];
}
