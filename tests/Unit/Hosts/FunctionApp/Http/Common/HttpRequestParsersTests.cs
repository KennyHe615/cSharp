using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

using FunctionApps.Http.Common;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Unit.Hosts.FunctionApp.Http.Common;

public sealed class HttpRequestParsersTests
{
    [Fact]
    public async Task DeserializeOrBadRequestAsync_WithValidJson_ReturnsRequest()
    {
        FakeHttpRequestData req = new FakeHttpRequestData(new FakeFunctionContext(),
                                                          "POST",
                                                          "http://localhost/api/test",
                                                          """{"name":"alice","kind":"User"}""");

        JsonSerializerOptions options = CreateOptions();

        SampleRequest result =
                await HttpRequestParsers.DeserializeOrBadRequestAsync<SampleRequest>(req,
                    options,
                    CancellationToken.None);

        Assert.Equal("alice", result.Name);
        Assert.Equal(SampleKind.User, result.Kind);
    }

    [Fact]
    public async Task DeserializeOrBadRequestAsync_WithNullBody_ThrowsBadRequestHandledException()
    {
        FakeHttpRequestData req = new FakeHttpRequestData(new FakeFunctionContext(),
                                                          "POST",
                                                          "http://localhost/api/test",
                                                          "null");

        JsonSerializerOptions options = CreateOptions();

        BadRequestHandledException ex =
                await Assert.ThrowsAsync<BadRequestHandledException>(() =>
                                                                             HttpRequestParsers
                                                                                    .DeserializeOrBadRequestAsync<
                                                                                             SampleRequest>(req,
                                                                                         options,
                                                                                         CancellationToken.None));

        string body = ex.Response.ReadBodyAsString();
        Assert.Contains("\"Error\":\"Invalid request body.\"", body);
    }

    [Fact]
    public async Task DeserializeOrBadRequestAsync_WithMalformedJson_ThrowsJsonException()
    {
        FakeHttpRequestData req = new FakeHttpRequestData(new FakeFunctionContext(),
                                                          "POST",
                                                          "http://localhost/api/test",
                                                          "{");

        JsonSerializerOptions options = CreateOptions();

        await Assert.ThrowsAsync<JsonException>(() =>
                                                        HttpRequestParsers
                                                               .DeserializeOrBadRequestAsync<SampleRequest>(req,
                                                                    options,
                                                                    CancellationToken.None));
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions
                                        {
                                            PropertyNameCaseInsensitive = true
                                        };

        options.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false));

        return options;
    }

    [ExcludeFromCodeCoverage]
    private sealed class SampleRequest
    {
        public string Name { get; set; } = string.Empty;

        public SampleKind Kind { get; set; }
    }

    private enum SampleKind
    {
        User,
        Admin
    }

    #endregion
}
