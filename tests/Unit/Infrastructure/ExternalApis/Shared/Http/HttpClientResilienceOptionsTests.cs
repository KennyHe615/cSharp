using System.ComponentModel.DataAnnotations;

using Infrastructure.ExternalApis.Shared.Http;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Shared.Http;

public sealed class HttpClientResilienceOptionsTests
{
    [Fact]
    public void SectionName_IsExpected()
    {
        Assert.Equal("HttpClientResilience", HttpClientResilienceOptions.SectionName);
    }

    [Fact]
    public void Defaults_AreValid_Recursively()
    {
        HttpClientResilienceOptions options = new HttpClientResilienceOptions();

        List<ValidationResult> results = ValidateRecursively(options);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(181)]
    public void TimeoutSeconds_FailsRangeValidation(int value)
    {
        HttpClientResilienceOptions options = new HttpClientResilienceOptions
                                              {
                                                  TimeoutSeconds = value
                                              };

        List<ValidationResult> results = ValidateRecursively(options);

        AssertContainsMember(results);
    }

    #region RetryStrategy

    [Fact]
    public void RetryStrategies_IsRequired()
    {
        HttpClientResilienceOptions options = new HttpClientResilienceOptions
                                              {
                                                  RetryStrategies = null!
                                              };

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(6)]
    public void RetryStrategy_MaxAttempts_FailsRangeValidation(int value)
    {
        RetryStrategy strategy = ValidRetryStrategy();
        strategy.MaxAttempts = value;

        List<ValidationResult> results = ValidateObjectOnly(strategy);

        AssertContainsMember(results);
    }

    [Fact]
    public void RetryStrategy_StatusCodes_IsRequired()
    {
        RetryStrategy strategy = ValidRetryStrategy();
        strategy.StatusCodes = null!;

        List<ValidationResult> results = ValidateObjectOnly(strategy);

        AssertContainsMember(results);
    }

    [Fact]
    public void RetryStrategy_StatusCodes_MinLengthValidation_FailsWhenEmpty()
    {
        RetryStrategy strategy = ValidRetryStrategy();
        strategy.StatusCodes = Array.Empty<int>();

        List<ValidationResult> results = ValidateObjectOnly(strategy);

        AssertContainsMember(results);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("00:05:01")]
    public void RetryStrategy_InitialDelay_FailsRangeValidation(string value)
    {
        RetryStrategy strategy = ValidRetryStrategy();
        strategy.InitialDelay = TimeSpan.Parse(value);

        List<ValidationResult> results = ValidateObjectOnly(strategy);

        AssertContainsMember(results);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("00:10:01")]
    public void RetryStrategy_MaxDelay_FailsRangeValidation(string value)
    {
        RetryStrategy strategy = ValidRetryStrategy();
        strategy.MaxDelay = TimeSpan.Parse(value);

        List<ValidationResult> results = ValidateObjectOnly(strategy);

        AssertContainsMember(results);
    }

    [Fact]
    public void RetryStrategies_NestedSafeMethods_IsRequired()
    {
        HttpMethodRetryStrategies strategies = new HttpMethodRetryStrategies
                                               {
                                                   SafeMethods = null!,
                                                   RateLimited = ValidRetryStrategy(),
                                                   UnsafeMethods = ValidRetryStrategy()
                                               };

        List<ValidationResult> results = ValidateObjectOnly(strategies);

        AssertContainsMember(results);
    }

    [Fact]
    public void RetryStrategies_NestedRateLimited_IsRequired()
    {
        HttpMethodRetryStrategies strategies = new HttpMethodRetryStrategies
                                               {
                                                   SafeMethods = ValidRetryStrategy(),
                                                   RateLimited = null!,
                                                   UnsafeMethods = ValidRetryStrategy()
                                               };

        List<ValidationResult> results = ValidateObjectOnly(strategies);

        AssertContainsMember(results);
    }

    [Fact]
    public void RetryStrategies_NestedUnsafeMethods_IsRequired()
    {
        HttpMethodRetryStrategies strategies = new HttpMethodRetryStrategies
                                               {
                                                   SafeMethods = ValidRetryStrategy(),
                                                   RateLimited = ValidRetryStrategy(),
                                                   UnsafeMethods = null!
                                               };

        List<ValidationResult> results = ValidateObjectOnly(strategies);

        AssertContainsMember(results);
    }

    #endregion

    #region CircuitBreaker

    [Fact]
    public void CircuitBreaker_IsRequired()
    {
        HttpClientResilienceOptions options = new HttpClientResilienceOptions
                                              {
                                                  CircuitBreaker = null!
                                              };

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(21)]
    public void CircuitBreaker_ExceptionsAllowedBeforeBreaking_FailsRangeValidation(int value)
    {
        CircuitBreakerOptions options = ValidCircuitBreakerOptions();
        options.ExceptionsAllowedBeforeBreaking = value;

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    [Theory]
    [InlineData("00:00:29")]
    [InlineData("00:10:01")]
    public void CircuitBreaker_DurationOfBreak_FailsRangeValidation(string value)
    {
        CircuitBreakerOptions options = ValidCircuitBreakerOptions();
        options.DurationOfBreak = TimeSpan.Parse(value);

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    [Fact]
    public void CircuitBreaker_HandledStatusCodes_IsRequired()
    {
        CircuitBreakerOptions options = ValidCircuitBreakerOptions();
        options.HandledStatusCodes = null!;

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    [Fact]
    public void CircuitBreaker_HandledStatusCodes_MinLengthValidation_FailsWhenEmpty()
    {
        CircuitBreakerOptions options = ValidCircuitBreakerOptions();
        options.HandledStatusCodes = Array.Empty<int>();

        List<ValidationResult> results = ValidateObjectOnly(options);

        AssertContainsMember(results);
    }

    #endregion

    [Fact]
    public void ValidCustomizedObjectGraph_IsValid()
    {
        HttpClientResilienceOptions options = new HttpClientResilienceOptions
                                              {
                                                  TimeoutSeconds = 30,
                                                  RetryStrategies =
                                                          new HttpMethodRetryStrategies
                                                          {
                                                              SafeMethods =
                                                                      new RetryStrategy
                                                                      {
                                                                          MaxAttempts =
                                                                                  2,
                                                                          StatusCodes =
                                                                          [
                                                                              408, 503
                                                                          ],
                                                                          UseExponentialBackoff =
                                                                                  true,
                                                                          InitialDelay =
                                                                                  TimeSpan
                                                                                         .FromSeconds(1),
                                                                          MaxDelay =
                                                                                  TimeSpan
                                                                                         .FromSeconds(10)
                                                                      },
                                                              RateLimited =
                                                                      new RetryStrategy
                                                                      {
                                                                          MaxAttempts =
                                                                                  1,
                                                                          StatusCodes =
                                                                                  [429],
                                                                          UseExponentialBackoff =
                                                                                  false,
                                                                          InitialDelay =
                                                                                  TimeSpan
                                                                                         .FromMinutes(2),
                                                                          MaxDelay =
                                                                                  TimeSpan
                                                                                         .FromMinutes(2)
                                                                      },
                                                              UnsafeMethods =
                                                                      new RetryStrategy
                                                                      {
                                                                          MaxAttempts =
                                                                                  1,
                                                                          StatusCodes =
                                                                          [
                                                                              502, 503,
                                                                              504
                                                                          ],
                                                                          UseExponentialBackoff =
                                                                                  false,
                                                                          InitialDelay =
                                                                                  TimeSpan
                                                                                         .FromSeconds(1),
                                                                          MaxDelay =
                                                                                  TimeSpan
                                                                                         .FromSeconds(5)
                                                                      }
                                                          },
                                                  CircuitBreaker =
                                                          new CircuitBreakerOptions
                                                          {
                                                              ExceptionsAllowedBeforeBreaking =
                                                                      3,
                                                              DurationOfBreak =
                                                                      TimeSpan
                                                                             .FromMinutes(1),
                                                              HandledStatusCodes =
                                                                      [500, 503]
                                                          }
                                              };

        List<ValidationResult> results = ValidateRecursively(options);

        Assert.Empty(results);
    }

    #region ========== *** Private Methods *** ==========

    private static RetryStrategy ValidRetryStrategy()
    {
        return new RetryStrategy
               {
                   MaxAttempts = 1,
                   StatusCodes = [502],
                   UseExponentialBackoff = false,
                   InitialDelay = TimeSpan.FromSeconds(1),
                   MaxDelay = TimeSpan.FromSeconds(2)
               };
    }

    private static CircuitBreakerOptions ValidCircuitBreakerOptions()
    {
        return new CircuitBreakerOptions
               {
                   ExceptionsAllowedBeforeBreaking = 2,
                   DurationOfBreak = TimeSpan.FromMinutes(1),
                   HandledStatusCodes = [500]
               };
    }

    private static List<ValidationResult> ValidateObjectOnly(object instance)
    {
        ValidationContext context = new ValidationContext(instance);
        List<ValidationResult> results = new List<ValidationResult>();

        _ = Validator.TryValidateObject(instance,
                                        context,
                                        results,
                                        true);

        return results;
    }

    private static List<ValidationResult> ValidateRecursively(HttpClientResilienceOptions options)
    {
        List<ValidationResult> results = ValidateObjectOnly(options);

        results.AddRange(ValidateObjectOnly(options.RetryStrategies));

        results.AddRange(ValidateObjectOnly(options.RetryStrategies.SafeMethods));

        results.AddRange(ValidateObjectOnly(options.RetryStrategies.RateLimited));

        results.AddRange(ValidateObjectOnly(options.RetryStrategies.UnsafeMethods));

        results.AddRange(ValidateObjectOnly(options.CircuitBreaker));

        return results;
    }

    private static void AssertContainsMember(IEnumerable<ValidationResult> results)
    {
        Assert.Contains(results, r => r.MemberNames.Any());
    }

    #endregion
}
