using System.Net;

using Infrastructure.ExternalApis.Shared.Http;

using Xunit;


namespace tests.Unit.Infrastructure.ExternalApis.Shared.Http;

public sealed class ExternalServiceHttpExceptionTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Throws_WhenMethodIsInvalid(string? method)
    {
        ArgumentException ex =
                Assert.Throws<ArgumentException>(() => new ExternalServiceHttpException(HttpStatusCode.BadGateway,
                                                     method!,
                                                     "https://api.example.com/orders",
                                                     "failure"));

        Assert.Equal("method", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Throws_WhenUrlIsInvalid(string? url)
    {
        ArgumentException ex =
                Assert.Throws<ArgumentException>(() => new ExternalServiceHttpException(HttpStatusCode.BadGateway,
                                                     "GET",
                                                     url!,
                                                     "failure"));

        Assert.Equal("url", ex.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Throws_WhenMessageIsInvalid(string? message)
    {
        ArgumentException ex =
                Assert.Throws<ArgumentException>(() => new ExternalServiceHttpException(HttpStatusCode.BadGateway,
                                                     "GET",
                                                     "https://api.example.com/orders",
                                                     message!));

        Assert.Equal("message", ex.ParamName);
    }

    [Fact]
    public void Ctor_Normalizes_Method_Url_And_OperationName()
    {
        ExternalServiceHttpException ex =
                new ExternalServiceHttpException(HttpStatusCode.ServiceUnavailable,
                                                 "  post  ",
                                                 "  https://api.example.com/orders  ",
                                                 "upstream failed",
                                                 responseSummary: "len=120,sha256_8=ABCDEF12",
                                                 operationName: "  CreateOrder  ");

        Assert.Equal("POST", ex.Method);
        Assert.Equal("https://api.example.com/orders", ex.Url);
        Assert.Equal("CreateOrder", ex.OperationName);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, ex.StatusCode);
        Assert.Equal("len=120,sha256_8=ABCDEF12", ex.ResponseSummary);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Ctor_Sets_OperationName_Null_WhenMissingOrWhitespace(string? operationName)
    {
        ExternalServiceHttpException ex =
                new ExternalServiceHttpException(HttpStatusCode.BadRequest,
                                                 "GET",
                                                 "https://api.example.com/orders/1",
                                                 "failure",
                                                 operationName: operationName);

        Assert.Null(ex.OperationName);
    }

    [Fact]
    public void Ctor_Preserves_InnerException()
    {
        InvalidOperationException inner = new InvalidOperationException("root cause");

        ExternalServiceHttpException ex =
                new ExternalServiceHttpException(HttpStatusCode.GatewayTimeout,
                                                 "GET",
                                                 "https://api.example.com/orders/1",
                                                 "failure",
                                                 inner);

        Assert.Same(inner, ex.InnerException);
    }

    [Fact]
    public void Ctor_Allows_Nullables_ForStatusAndSummary()
    {
        ExternalServiceHttpException ex = new ExternalServiceHttpException(null,
                                                                           "GET",
                                                                           "/relative/path",
                                                                           "failure",
                                                                           responseSummary: null,
                                                                           operationName: null);

        Assert.Null(ex.StatusCode);
        Assert.Null(ex.ResponseSummary);
        Assert.Null(ex.OperationName);
        Assert.Equal("GET", ex.Method);
        Assert.Equal("/relative/path", ex.Url);
    }
}
