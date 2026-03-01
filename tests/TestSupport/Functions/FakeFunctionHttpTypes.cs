using System.Collections.Immutable;
using System.Net;
using System.Security.Claims;
using System.Text;

using Azure.Core.Serialization;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.DependencyInjection;


namespace tests.TestSupport.Functions;

public sealed class FakeFunctionContext : FunctionContext
{
    public FakeFunctionContext()
    {
        ServiceCollection services = [];
        services.AddOptions();

        services.Configure<WorkerOptions>(options =>
                                          {
                                              options.Serializer = new JsonObjectSerializer();
                                          });

        InstanceServices = services.BuildServiceProvider();
    }

    public override string InvocationId { get; } = Guid.NewGuid()
                                                       .ToString();

    public override string FunctionId { get; } = "tests-function-id";

    public override TraceContext TraceContext { get; } = new FakeTraceContext();

    public override BindingContext BindingContext { get; } = new FakeBindingContext();

    public override RetryContext RetryContext { get; } = new FakeRetryContext();

    public override IServiceProvider InstanceServices { get; set; }

    public override FunctionDefinition FunctionDefinition { get; } = new FakeFunctionDefinition();

    public override IDictionary<object, object> Items { get; set; } = new Dictionary<object, object>();

    public override IInvocationFeatures Features { get; } = new FakeInvocationFeatures();
}

public sealed class FakeHttpRequestData(FunctionContext functionContext,
                                        string method,
                                        string url,
                                        string bodyJson) : HttpRequestData(functionContext)
{
    public override Stream Body { get; } = new MemoryStream(Encoding.UTF8.GetBytes(bodyJson));

    public override HttpHeadersCollection Headers { get; } = [];

    public override IReadOnlyCollection<IHttpCookie> Cookies { get; } = Array.Empty<IHttpCookie>();

    public override Uri Url { get; } = new Uri(url);

    public override IEnumerable<ClaimsIdentity> Identities { get; } = Array.Empty<ClaimsIdentity>();

    public override string Method { get; } = method;

    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
}

public sealed class FakeHttpResponseData(FunctionContext functionContext) : HttpResponseData(functionContext)
{
    public override HttpStatusCode StatusCode { get; set; }

    public override HttpHeadersCollection Headers { get; set; } = [];

    public override Stream Body { get; set; } = new MemoryStream();

    public override HttpCookies Cookies { get; } = new FakeHttpCookies();
}

public static class FakeHttpResponseDataExtensions
{
    public static string ReadBodyAsString(this HttpResponseData response)
    {
        response.Body.Position = 0;

        using StreamReader reader = new StreamReader(response.Body,
                                                     Encoding.UTF8,
                                                     true,
                                                     1024,
                                                     true);
        string payload = reader.ReadToEnd();

        response.Body.Position = 0;

        return payload;
    }
}

internal sealed class FakeTraceContext : TraceContext
{
    public override string TraceParent => string.Empty;

    public override string TraceState => string.Empty;
}

internal sealed class FakeBindingContext : BindingContext
{
    public override IReadOnlyDictionary<string, object?> BindingData { get; } = new Dictionary<string, object?>();
}

internal sealed class FakeRetryContext : RetryContext
{
    public override int RetryCount => 0;

    public override int MaxRetryCount => 0;
}

internal sealed class FakeFunctionDefinition : FunctionDefinition
{
    public override ImmutableArray<FunctionParameter> Parameters => ImmutableArray<FunctionParameter>.Empty;

    public override string PathToAssembly => string.Empty;

    public override string EntryPoint => string.Empty;

    public override string Id => "tests-function-definition";

    public override string Name => "tests";

    public override IImmutableDictionary<string, BindingMetadata> InputBindings =>
        ImmutableDictionary<string, BindingMetadata>.Empty;

    public override IImmutableDictionary<string, BindingMetadata> OutputBindings =>
        ImmutableDictionary<string, BindingMetadata>.Empty;
}

internal sealed class FakeInvocationFeatures : IInvocationFeatures
{
    private readonly Dictionary<Type, object> _values = new Dictionary<Type, object>();

    public void Set<T>(T instance)
    {
        _values[typeof(T)] = instance!;
    }

    public T Get<T>()
    {
        return _values.TryGetValue(typeof(T), out object? value) ? (T)value : default!;
    }

    public IEnumerator<KeyValuePair<Type, object>> GetEnumerator()
    {
        return _values.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return _values.GetEnumerator();
    }
}

internal sealed class FakeHttpCookies : HttpCookies
{
    public override void Append(string name, string value)
    {
    }

    public override void Append(IHttpCookie cookie)
    {
    }

    public override IHttpCookie CreateNew()
    {
        return new FakeHttpCookie();
    }
}

internal sealed class FakeHttpCookie : IHttpCookie
{
    public string Domain { get; } = string.Empty;

    public DateTimeOffset? Expires { get; } = null;

    public bool? HttpOnly { get; } = null;

    public double? MaxAge { get; } = null;

    public string Name { get; } = string.Empty;

    public string Path { get; } = string.Empty;

    public SameSite SameSite { get; } = SameSite.None;

    public bool? Secure { get; } = null;

    public string Value { get; } = string.Empty;
}
