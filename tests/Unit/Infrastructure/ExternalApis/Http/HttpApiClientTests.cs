using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;

using Flurl.Http;

using Infrastructure.ExternalApis.Http;

using Polly;
using Polly.CircuitBreaker;

using tests.TestSupport.Context;
using tests.TestSupport.Logging;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Http;

public sealed class HttpApiClientTests
{
    #region Ctor

    [Fact]
    public void Ctor_Throws_WhenClientIsNull()
    {
        FakeHttpApiClientFactory factory = new FakeHttpApiClientFactory();
        StubLobContext lob = new StubLobContext();
        TestLogger<HttpApiClient> logger = new TestLogger<HttpApiClient>();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClient(null!,
                                                                     factory,
                                                                     lob,
                                                                     logger));
    }

    [Fact]
    public void Ctor_Throws_WhenFactoryIsNull()
    {
        FlurlClient client = CreateFlurlClient(new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        StubLobContext lob = new StubLobContext();
        TestLogger<HttpApiClient> logger = new TestLogger<HttpApiClient>();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClient(client,
                                                                     null!,
                                                                     lob,
                                                                     logger));
    }

    [Fact]
    public void Ctor_Throws_WhenLobContextIsNull()
    {
        FlurlClient client = CreateFlurlClient(new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        FakeHttpApiClientFactory factory = new FakeHttpApiClientFactory();
        TestLogger<HttpApiClient> logger = new TestLogger<HttpApiClient>();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClient(client,
                                                                     factory,
                                                                     null!,
                                                                     logger));
    }

    [Fact]
    public void Ctor_Throws_WhenLoggerIsNull()
    {
        FlurlClient client = CreateFlurlClient(new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        FakeHttpApiClientFactory factory = new FakeHttpApiClientFactory();
        StubLobContext lob = new StubLobContext();

        Assert.Throws<ArgumentNullException>(() => new HttpApiClient(client,
                                                                     factory,
                                                                     lob,
                                                                     null!));
    }

    #endregion

    #region BaseUrl

    [Fact]
    public void BaseUrl_ReturnsClientBaseUrl()
    {
        FlurlClient client = CreateFlurlClient(new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        HttpApiClient sut = CreateSut(client);

        Assert.Equal("https://example.test/", sut.BaseUrl);
    }

    #endregion

    #region Methods

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.GetAsync<object>(endpoint!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PostAsync<object, object>(endpoint!, new {}));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostUrlEncodedAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PostUrlEncodedAsync<object>(endpoint!, new {}));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PutAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PutAsync<object, object>(endpoint!, new {}));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PatchAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.PatchAsync<object, object>(endpoint!, new {}));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DeleteAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.DeleteAsync(endpoint!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task HeadAsync_ThrowsArgumentException_WhenEndpointInvalid(string? endpoint)
    {
        HttpApiClient sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentException>(() => sut.HeadAsync(endpoint!));
    }

    [Fact]
    public async Task GetAsync_ReturnsDeserializedPayload_AndAppliesHeaders()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
                                                {
                                                    Content = new StringContent("{\"id\":7,\"name\":\"alpha\"}",
                                                                                Encoding.UTF8,
                                                                                "application/json")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        ItemDto? result = await sut.GetAsync<ItemDto>("items/7",
                                                      new Dictionary<string, string> { ["x-tenant"] = "NTT" });

        Assert.NotNull(result);
        Assert.Equal(7, result.Id);
        Assert.Equal("alpha", result.Name);

        HttpRequestMessage sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, sent.Method);
        Assert.Equal("https://example.test/items/7", sent.RequestUri!.ToString());
        Assert.True(sent.Headers.TryGetValues("x-tenant", out IEnumerable<string>? values));
        Assert.Equal("NTT", Assert.Single(values));
    }

    [Fact]
    public async Task DeleteAsync_Completes_WhenResponseSuccessful()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.NoContent));
        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        await sut.DeleteAsync("items/7");

        HttpRequestMessage sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Delete, sent.Method);
    }

    [Fact]
    public async Task HeadAsync_Completes_WhenResponseSuccessful()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK));
        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        await sut.HeadAsync("items/7");

        HttpRequestMessage sent = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Head, sent.Method);
    }

    [Fact]
    public async Task PostAsync_UsesUnsafePolicy_WhenSafePolicyIsOpenCircuit()
    {
        AsyncCircuitBreakerPolicy safeBreaker = Policy.Handle<Exception>()
                                                      .CircuitBreakerAsync(1, TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<Exception>(() => safeBreaker.ExecuteAsync(() => throw new Exception("trip")));

        IAsyncPolicy unsafePolicy = Policy.NoOpAsync();

        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
                                                {
                                                    Content = new StringContent("{\"id\":9,\"name\":\"beta\"}",
                                                                                Encoding.UTF8,
                                                                                "application/json")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler),
                                      new FakeHttpApiClientFactory(safeBreaker, unsafePolicy));

        ItemDto? result = await sut.PostAsync<object, ItemDto>("items", new { name = "beta" });

        Assert.NotNull(result);
        Assert.Equal(9, result.Id);
    }

    [Fact]
    public async Task GetAsync_UsesSafePolicy_WhenUnsafePolicyIsOpenCircuit()
    {
        AsyncCircuitBreakerPolicy unsafeBreaker = Policy.Handle<Exception>()
                                                        .CircuitBreakerAsync(1, TimeSpan.FromMinutes(1));

        await Assert.ThrowsAsync<Exception>(() => unsafeBreaker.ExecuteAsync(() => throw new Exception("trip")));

        IAsyncPolicy safePolicy = Policy.NoOpAsync();

        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
                                                {
                                                    Content = new StringContent("{\"id\":3,\"name\":\"gamma\"}",
                                                                                Encoding.UTF8,
                                                                                "application/json")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler),
                                      new FakeHttpApiClientFactory(safePolicy, unsafeBreaker));

        ItemDto? result = await sut.GetAsync<ItemDto>("items/3");

        Assert.NotNull(result);
        Assert.Equal(3, result.Id);
    }

    [Fact]
    public async Task GetAsync_WrapsFlurlHttpException_AsExternalServiceHttpException()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.BadGateway)
                                                {
                                                    Content = new StringContent("{\"error\":\"upstream failed\"}",
                                                                                Encoding.UTF8,
                                                                                "application/json")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        ExternalServiceHttpException ex =
            await Assert.ThrowsAsync<ExternalServiceHttpException>(() => sut.GetAsync<ItemDto>("items/77"));

        Assert.Equal(HttpStatusCode.BadGateway, ex.StatusCode);
        Assert.Equal("GET", ex.Method);
        Assert.Equal("https://example.test/items/77", ex.Url);
        Assert.Equal("HttpApiClient", ex.OperationName);
        Assert.Contains("External API request failed", ex.Message);
        Assert.NotNull(ex.ResponseSummary);
        Assert.Contains("len=", ex.ResponseSummary!);
        Assert.Contains("sha256_8=", ex.ResponseSummary!);
    }

    [Fact]
    public async Task GetAsync_RethrowsUnexpectedException_WhenDeserializationFails()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK)
                                                {
                                                    Content = new StringContent("not-json",
                                                                                Encoding.UTF8,
                                                                                "application/json")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        await Assert.ThrowsAnyAsync<Exception>(() => sut.GetAsync<ItemDto>("items/1"));
    }

    [Fact]
    public async Task GetAsync_WrapsFlurlHttpException_WithNullSummary_WhenBodyIsEmpty()
    {
        QueueHandler handler = new QueueHandler(new HttpResponseMessage(HttpStatusCode.InternalServerError)
                                                {
                                                    Content = new StringContent(string.Empty,
                                                                                Encoding.UTF8,
                                                                                "text/plain")
                                                });

        HttpApiClient sut = CreateSut(CreateFlurlClient(handler));

        ExternalServiceHttpException ex =
            await Assert.ThrowsAsync<ExternalServiceHttpException>(() => sut.GetAsync<ItemDto>("items/500"));

        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Null(ex.ResponseSummary);
    }

    #endregion

    #region ========== *** Private Methods *** ==========

    private static HttpApiClient CreateSut(FlurlClient? client =
                                               null,
                                           FakeHttpApiClientFactory? factory = null,
                                           StubLobContext? lobContext = null,
                                           TestLogger<HttpApiClient>? logger = null)
    {
        return new HttpApiClient(client
                                 ?? CreateFlurlClient(new QueueHandler(new HttpResponseMessage(HttpStatusCode.OK))),
                                 factory    ?? new FakeHttpApiClientFactory(),
                                 lobContext ?? new StubLobContext(),
                                 logger     ?? new TestLogger<HttpApiClient>());
    }

    private static FlurlClient CreateFlurlClient(HttpMessageHandler handler, string baseUrl = "https://example.test/")
    {
        HttpClient httpClient = new HttpClient(handler)
                                {
                                    BaseAddress = new Uri(baseUrl)
                                };

        return new FlurlClient(httpClient);
    }

    [ExcludeFromCodeCoverage]
    private sealed class FakeHttpApiClientFactory(IAsyncPolicy? safe = null,
                                                  IAsyncPolicy? unsafePolicy = null) : IHttpApiClientFactory
    {
        private readonly IAsyncPolicy _safe = safe           ?? Policy.NoOpAsync();
        private readonly IAsyncPolicy _unsafe = unsafePolicy ?? Policy.NoOpAsync();

        public FlurlClient GetOrAddClient(string baseUrl)
        {
            throw new NotSupportedException();
        }

        public IAsyncPolicy GetSafePolicy()
        {
            return _safe;
        }

        public IAsyncPolicy GetUnsafePolicy()
        {
            return _unsafe;
        }

        public void Dispose()
        {
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new Queue<HttpResponseMessage>(responses);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            Requests.Add(CloneRequestWithoutBody(request));

            return Task.FromResult(_responses.Count == 0
                                       ? new HttpResponseMessage(HttpStatusCode.OK)
                                       : _responses.Dequeue());
        }

        private static HttpRequestMessage CloneRequestWithoutBody(HttpRequestMessage source)
        {
            HttpRequestMessage clone = new HttpRequestMessage(source.Method, source.RequestUri);

            foreach (KeyValuePair<string, IEnumerable<string>> h in source.Headers)
            {
                clone.Headers.TryAddWithoutValidation(h.Key, h.Value);
            }

            return clone;
        }
    }

    private sealed class ItemDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    #endregion
}
