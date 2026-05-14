using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;

using FunctionApps.Http.Common;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Unit.Hosts.FunctionApp.Http.Common;

public sealed class HttpResponseFactoryTests
{
    [Fact]
    public async Task CreatedAsync_Returns201_WithPayload()
    {
        FakeHttpRequestData req = CreateRequest();

        HttpResponseData response =
                await HttpResponseFactory.CreatedAsync(req, new { Id = 123, Name = "ok" }, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using JsonDocument doc = JsonDocument.Parse(response.ReadBodyAsString());
        Assert.Equal(123,
                     doc.RootElement.GetProperty("Id")
                        .GetInt32());
        Assert.Equal("ok",
                     doc.RootElement.GetProperty("Name")
                        .GetString());
    }

    [Fact]
    public async Task BadRequestAsync_Returns400_WithStandardErrorShape()
    {
        FakeHttpRequestData req = CreateRequest();

        HttpResponseData response =
                await HttpResponseFactory.BadRequestAsync(req, "validation failed", CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        using JsonDocument doc = JsonDocument.Parse(response.ReadBodyAsString());
        Assert.Equal("validation failed",
                     doc.RootElement.GetProperty("Error")
                        .GetString());
    }

    [Fact]
    public async Task InternalServerErrorAsync_Returns500_WithStandardErrorShape()
    {
        FakeHttpRequestData req = CreateRequest();

        HttpResponseData response =
                await HttpResponseFactory.InternalServerErrorAsync(req, "unexpected error", CancellationToken.None);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        using JsonDocument doc = JsonDocument.Parse(response.ReadBodyAsString());
        Assert.Equal("unexpected error",
                     doc.RootElement.GetProperty("Error")
                        .GetString());
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static FakeHttpRequestData CreateRequest()
    {
        return new FakeHttpRequestData(new FakeFunctionContext(),
                                       "POST",
                                       "http://localhost/api/recovery",
                                       "{}");
    }

    #endregion
}
