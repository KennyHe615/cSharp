using Flurl.Http;
using Flurl.Http.Configuration;

using Infrastructure.ExternalApis.Abstractions;
using Infrastructure.ExternalApis.Shared.Http;

using Microsoft.Extensions.Options;

using Polly;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Shared.Http;

public sealed class HttpApiClientFactoryTests
{
    #region Ctor

    [Fact]
    public void Ctor_Throws_WhenOptionsIsNull()
    {
        FakePolicyFactory policies = new FakePolicyFactory();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClientFactory(null!, policies));
    }

    [Fact]
    public void Ctor_Throws_WhenPolicyFactoryIsNull()
    {
        IOptions<HttpClientResilienceOptions> options = Options.Create(CreateOptions());

        Assert.Throws<ArgumentNullException>(() => new HttpApiClientFactory(options, null!));
    }

    [Fact]
    public void Ctor_Throws_WhenOptionsValueIsNull()
    {
        FakePolicyFactory policies = new FakePolicyFactory();
        IOptions<HttpClientResilienceOptions> options = new NullValueOptions();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClientFactory(options, policies));
    }

    #endregion

    #region GetOrAddClient

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetOrAddClient_Throws_WhenBaseUrlIsInvalid(string? baseUrl)
    {
        HttpApiClientFactory sut = CreateSut();

        ArgumentException ex = Assert.Throws<ArgumentException>(() => sut.GetOrAddClient(baseUrl!));

        Assert.Equal("baseUrl", ex.ParamName);
    }

    [Fact]
    public void GetOrAddClient_ReturnsSameInstance_ForSameBaseUrl()
    {
        HttpApiClientFactory sut = CreateSut();

        FlurlClient a = sut.GetOrAddClient("https://api.example.com");
        FlurlClient b = sut.GetOrAddClient("https://api.example.com");

        Assert.Same(a, b);
    }

    [Fact]
    public void GetOrAddClient_ReturnsDifferentInstances_ForDifferentBaseUrls()
    {
        HttpApiClientFactory sut = CreateSut();

        FlurlClient a = sut.GetOrAddClient("https://api-a.example.com");
        FlurlClient b = sut.GetOrAddClient("https://api-b.example.com");

        Assert.NotSame(a, b);
    }

    [Fact]
    public void GetOrAddClient_AppliesTimeoutFromOptions()
    {
        HttpClientResilienceOptions options = CreateOptions();
        options.TimeoutSeconds = 17;

        HttpApiClientFactory sut = CreateSut(options: options);

        FlurlClient client = sut.GetOrAddClient("https://api.example.com");

        Assert.Equal(TimeSpan.FromSeconds(17), client.Settings.Timeout);
    }

    [Fact]
    public void GetOrAddClient_ConfiguresJsonSerializer_WithSnakeUpperEnums()
    {
        HttpApiClientFactory sut = CreateSut();

        FlurlClient client = sut.GetOrAddClient("https://api.example.com");

        ISerializer serializer = client.Settings.JsonSerializer;
        Assert.NotNull(serializer);

        string json = serializer.Serialize(TestStatus.InQueue);
        Assert.Equal("\"IN_QUEUE\"", json);
    }

    [Fact]
    public void GetOrAddClient_ConfiguresCaseInsensitiveDeserializer()
    {
        HttpApiClientFactory sut = CreateSut();

        FlurlClient client = sut.GetOrAddClient("https://api.example.com");

        ISerializer serializer = client.Settings.JsonSerializer;

        Payload result = serializer.Deserialize<Payload>("{\"name\":\"kenny\"}");

        Assert.Equal("kenny", result.Name);
    }

    #endregion

    #region GetPolicy

    [Fact]
    public void GetSafePolicy_ReturnsPolicyFromPolicyFactory()
    {
        FakePolicyFactory policies = new FakePolicyFactory();
        HttpApiClientFactory sut = CreateSut(policyFactory: policies);

        IAsyncPolicy safe = sut.GetSafePolicy();

        Assert.Same(policies.SafePolicy, safe);
    }

    [Fact]
    public void GetUnsafePolicy_ReturnsPolicyFromPolicyFactory()
    {
        FakePolicyFactory policies = new FakePolicyFactory();
        HttpApiClientFactory sut = CreateSut(policyFactory: policies);

        IAsyncPolicy unsafePolicy = sut.GetUnsafePolicy();

        Assert.Same(policies.UnsafePolicy, unsafePolicy);
    }

    #endregion

    #region Dispose

    [Fact]
    public void Dispose_ClearsCache_AndAllowsFreshClientCreation()
    {
        HttpApiClientFactory sut = CreateSut();

        FlurlClient first = sut.GetOrAddClient("https://api.example.com");

        sut.Dispose();

        FlurlClient second = sut.GetOrAddClient("https://api.example.com");

        Assert.NotSame(first, second);
    }

    #endregion

    #region ========== *** Private Methods *** ==========

    private static HttpApiClientFactory CreateSut(HttpClientResilienceOptions? options =
                                                          null,
                                                  FakePolicyFactory? policyFactory = null)
    {
        return new HttpApiClientFactory(Options.Create(options ?? CreateOptions()),
                                        policyFactory ?? new FakePolicyFactory());
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
                                                           InitialDelay =
                                                                   TimeSpan
                                                                          .FromSeconds(1),
                                                           MaxDelay =
                                                                   TimeSpan.FromSeconds(1)
                                                       },
                                         RateLimited = new RetryStrategy
                                                       {
                                                           MaxAttempts = 1,
                                                           StatusCodes = [429],
                                                           UseExponentialBackoff = false,
                                                           InitialDelay =
                                                                   TimeSpan
                                                                          .FromSeconds(1),
                                                           MaxDelay =
                                                                   TimeSpan.FromSeconds(1)
                                                       },
                                         UnsafeMethods = new RetryStrategy
                                                         {
                                                             MaxAttempts = 1,
                                                             StatusCodes = [502, 503, 504],
                                                             UseExponentialBackoff = false,
                                                             InitialDelay =
                                                                     TimeSpan
                                                                            .FromSeconds(1),
                                                             MaxDelay =
                                                                     TimeSpan.FromSeconds(1)
                                                         }
                                     },
                   CircuitBreaker = new CircuitBreakerOptions
                                    {
                                        ExceptionsAllowedBeforeBreaking = 2,
                                        DurationOfBreak = TimeSpan.FromSeconds(5),
                                        HandledStatusCodes = [500, 502, 503, 504]
                                    }
               };
    }

    private sealed class FakePolicyFactory : IHttpResiliencePolicyFactory
    {
        public IAsyncPolicy SafePolicy { get; } = Policy.NoOpAsync();

        public IAsyncPolicy UnsafePolicy { get; } = Policy.NoOpAsync();

        public IAsyncPolicy CreateSafePolicy()
        {
            return SafePolicy;
        }

        public IAsyncPolicy CreateUnsafePolicy()
        {
            return UnsafePolicy;
        }
    }

    private sealed class NullValueOptions : IOptions<HttpClientResilienceOptions>
    {
        public HttpClientResilienceOptions Value => null!;
    }

    private enum TestStatus
    {
        InQueue
    }

    private sealed class Payload
    {
        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
