using System.Net;

using Infrastructure.ExternalApis.Http;
using Infrastructure.ExternalApis.Http.Policies;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;
using Polly.CircuitBreaker;

using tests.TestSupport.Logging;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Http.Policies;

public sealed class HttpResiliencePolicyFactoryTests
{
    #region Ctor

    [Fact]
    public void Ctor_Throws_WhenOptionsIsNull()
    {
        TestLogger<HttpResiliencePolicyFactory> logger = new TestLogger<HttpResiliencePolicyFactory>();

        Assert.Throws<ArgumentNullException>(() => new HttpResiliencePolicyFactory(null!, logger));
    }

    [Fact]
    public void Ctor_Throws_WhenLoggerIsNull()
    {
        IOptions<HttpClientResilienceOptions> options = Options.Create(CreateOptions());

        Assert.Throws<ArgumentNullException>(() => new HttpResiliencePolicyFactory(options, null!));
    }

    #endregion

    #region CreatePolicy

    [Fact]
    public void CreateSafePolicy_ReturnsPolicy()
    {
        HttpResiliencePolicyFactory sut = CreateSut();

        IAsyncPolicy policy = sut.CreateSafePolicy();

        Assert.NotNull(policy);
    }

    [Fact]
    public void CreateUnsafePolicy_ReturnsPolicy()
    {
        HttpResiliencePolicyFactory sut = CreateSut();

        IAsyncPolicy policy = sut.CreateUnsafePolicy();

        Assert.NotNull(policy);
    }

    #endregion

    #region SafePolicy

    [Fact]
    public async Task SafePolicy_Retries_ForConfiguredStatusCode()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 2;
        options.RetryStrategies.SafeMethods.StatusCodes = [503];
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int attempts = 0;

        await policy.ExecuteAsync(async _ =>
                                  {
                                      attempts++;
                                      if (attempts == 1)
                                      {
                                          throw CreateExternalHttpException(HttpStatusCode.ServiceUnavailable);
                                      }

                                      await Task.CompletedTask;
                                  },
                                  new Polly.Context());

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SafePolicy_Retries_ForHttpRequestException_WithoutStatus()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 2;
        options.RetryStrategies.SafeMethods.StatusCodes = [503];
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int attempts = 0;

        await policy.ExecuteAsync(async _ =>
                                  {
                                      attempts++;
                                      if (attempts == 1)
                                      {
                                          throw new HttpRequestException("transient network issue");
                                      }

                                      await Task.CompletedTask;
                                  },
                                  new Polly.Context());

        Assert.Equal(2, attempts);
    }

    [Fact]
    public async Task SafePolicy_DoesNotRetry_WhenTaskCanceledByCaller()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 3;
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int attempts = 0;
        CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        TaskCanceledException canceledByCaller = new TaskCanceledException("caller canceled", null, cts.Token);

        await Assert.ThrowsAsync<TaskCanceledException>(() => policy.ExecuteAsync(_ =>
                                                         {
                                                             attempts++;

                                                             throw canceledByCaller;
                                                         },
                                                         new Polly.Context()));

        Assert.Equal(1, attempts);
    }

    [Fact]
    public async Task SafePolicy_InvokesRefreshFunc_OnFirst401Retry()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 2;
        options.RetryStrategies.SafeMethods.StatusCodes = [401];
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int attempts = 0;
        int refreshCalls = 0;

        Polly.Context context = new Polly.Context
                                {
                                    [HttpPolicyContextKeys.Lob] = "NTT",
                                    [HttpPolicyContextKeys.RefreshFunc] =
                                        new Func<CancellationToken, Task>(_ =>
                                                                          {
                                                                              refreshCalls++;

                                                                              return Task.CompletedTask;
                                                                          })
                                };

        await policy.ExecuteAsync(async (_, _) =>
                                  {
                                      attempts++;
                                      if (attempts == 1)
                                      {
                                          throw CreateExternalHttpException(HttpStatusCode.Unauthorized);
                                      }

                                      await Task.CompletedTask;
                                  },
                                  context,
                                  CancellationToken.None);

        Assert.Equal(2, attempts);
        Assert.Equal(1, refreshCalls);
    }

    [Fact]
    public void SafePolicy_CircuitBreaker_Opens_ForHandledStatus()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 0;
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;
        options.CircuitBreaker.ExceptionsAllowedBeforeBreaking = 1;
        options.CircuitBreaker.DurationOfBreak = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.HandledStatusCodes = [500];

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int executed = 0;

        Assert.Throws<ExternalServiceHttpException>(() => policy.ExecuteAsync(_ =>
                                                                              {
                                                                                  executed++;

                                                                                  throw
                                                                                      CreateExternalHttpException(HttpStatusCode
                                                                                         .InternalServerError);
                                                                              },
                                                                              new Polly.Context())
                                                                .GetAwaiter()
                                                                .GetResult());

        Assert.Throws<BrokenCircuitException>(() => policy.ExecuteAsync(ExecuteNoOp, new Polly.Context())
                                                          .GetAwaiter()
                                                          .GetResult());

        Assert.Equal(1, executed);
    }

    [Fact]
    public async Task SafePolicy_CircuitBreaker_DoesNotOpen_ForUnhandledStatus()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.RetryStrategies.SafeMethods.MaxAttempts = 0;
        options.RetryStrategies.RateLimited.MaxAttempts = 0;
        options.RetryStrategies.UnsafeMethods.MaxAttempts = 0;
        options.CircuitBreaker.ExceptionsAllowedBeforeBreaking = 1;
        options.CircuitBreaker.DurationOfBreak = TimeSpan.FromSeconds(30);
        options.CircuitBreaker.HandledStatusCodes = [500];

        HttpResiliencePolicyFactory sut = CreateSut(options);
        IAsyncPolicy policy = sut.CreateSafePolicy();

        int executed = 0;

        await Assert.ThrowsAsync<ExternalServiceHttpException>(() => policy.ExecuteAsync(_ =>
                                                                {
                                                                    executed++;

                                                                    throw
                                                                        CreateExternalHttpException(HttpStatusCode
                                                                           .BadRequest);
                                                                },
                                                                new Polly.Context()));

        await Assert.ThrowsAsync<ExternalServiceHttpException>(() => policy.ExecuteAsync(_ =>
                                                                {
                                                                    executed++;

                                                                    throw
                                                                        CreateExternalHttpException(HttpStatusCode
                                                                           .BadRequest);
                                                                },
                                                                new Polly.Context()));

        Assert.Equal(2, executed);
    }

    #endregion

    #region ExecuteNoOp

    [Fact]
    public async Task ExecuteNoOp_CompletesSuccessfully()
    {
        await ExecuteNoOp(new Polly.Context());
    }

    #endregion

    #region TestLogger

    [Fact]
    public void TestLogger_BeginScope_ReturnsDisposable_AndDispose_IsCallable()
    {
        TestLogger<HttpResiliencePolicyFactory> logger = new TestLogger<HttpResiliencePolicyFactory>();

        IDisposable scope = logger.BeginScope("scope-state");

        Assert.NotNull(scope);
        scope.Dispose();// covers NoopDisposable.Dispose()
    }

    [Theory]
    [InlineData(LogLevel.Trace)]
    [InlineData(LogLevel.Debug)]
    [InlineData(LogLevel.Information)]
    [InlineData(LogLevel.Warning)]
    [InlineData(LogLevel.Error)]
    [InlineData(LogLevel.Critical)]
    [InlineData(LogLevel.None)]
    public void TestLogger_IsEnabled_ReturnsTrue(LogLevel level)
    {
        TestLogger<HttpResiliencePolicyFactory> logger = new TestLogger<HttpResiliencePolicyFactory>();

        bool enabled = logger.IsEnabled(level);

        Assert.True(enabled);
    }

    [Fact]
    public void TestLogger_Log_InvokesFormatter()
    {
        TestLogger<HttpResiliencePolicyFactory> logger = new TestLogger<HttpResiliencePolicyFactory>();
        bool formatterCalled = false;

        logger.Log(LogLevel.Information,
                   new EventId(42, "evt"),
                   "state",
                   null,
                   (state, _) =>
                   {
                       formatterCalled = true;

                       return state;
                   });

        Assert.True(formatterCalled);
    }

    #endregion

    #region ========== *** Private Methods *** ==========

    private static HttpResiliencePolicyFactory CreateSut(HttpClientResilienceOptions? options = null)
    {
        return new HttpResiliencePolicyFactory(Options.Create(options ?? CreateOptions()),
                                               new TestLogger<HttpResiliencePolicyFactory>());
    }

    private static HttpClientResilienceOptions CreateOptions()
    {
        return new HttpClientResilienceOptions
               {
                   TimeoutSeconds = 60,
                   RetryStrategies = new HttpMethodRetryStrategies
                                     {
                                         SafeMethods = new RetryStrategy
                                                       {
                                                           MaxAttempts = 1,
                                                           StatusCodes =
                                                               [408, 502, 503, 504],
                                                           UseExponentialBackoff = false,
                                                           InitialDelay = TimeSpan.Zero,
                                                           MaxDelay = TimeSpan.Zero
                                                       },
                                         RateLimited = new RetryStrategy
                                                       {
                                                           MaxAttempts = 0,
                                                           StatusCodes = [429],
                                                           UseExponentialBackoff = false,
                                                           InitialDelay = TimeSpan.Zero,
                                                           MaxDelay = TimeSpan.Zero
                                                       },
                                         UnsafeMethods = new RetryStrategy
                                                         {
                                                             MaxAttempts = 1,
                                                             StatusCodes = [502, 503, 504],
                                                             UseExponentialBackoff = false,
                                                             InitialDelay = TimeSpan.Zero,
                                                             MaxDelay = TimeSpan.Zero
                                                         }
                                     },
                   CircuitBreaker = new CircuitBreakerOptions
                                    {
                                        ExceptionsAllowedBeforeBreaking = 10,
                                        DurationOfBreak = TimeSpan.FromSeconds(1),
                                        HandledStatusCodes = [500, 502, 503, 504]
                                    }
               };
    }

    private static ExternalServiceHttpException CreateExternalHttpException(HttpStatusCode statusCode)
    {
        return new ExternalServiceHttpException(statusCode,
                                                "GET",
                                                "https://example.test/resource",
                                                "upstream failed");
    }

    private static Task ExecuteNoOp(Polly.Context _)
    {
        return Task.CompletedTask;
    }

    #endregion
}
