using System.Net;
using System.Reflection;

using FluentValidation;
using FluentValidation.Results;

using FunctionApp.Http;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace Tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpValidationTests
{
    [Fact]
    public async Task Post_MissingLob_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "category":"UsersDetails",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Lob is required.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MissingCategory_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Category is required.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidLob_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"abc",
                                                                                      "category":"UsersDetails",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'lob': 'abc'.", error);
        Assert.Contains("NTT / LCL / CRC", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidEnumText_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":"BadValue",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_CategoryNumericString_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":"1",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_CategoryNumericNumber_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":1,
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MissingIntervalAndJobId_ReturnsBadRequest_FromValidationException()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator
                                                                {
                                                                    OnSend = (_, _) =>
                                                                             {
                                                                                 List<ValidationFailure> failures =
                                                                                 [
                                                                                     new ValidationFailure("",
                                                                                      "Either Interval or JobId must be provided.")
                                                                                 ];

                                                                                 throw
                                                                                     new ValidationException(failures);
                                                                             }
                                                                };

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":"UsersDetails"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Either Interval or JobId must be provided.", error);
        Assert.Equal(1, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MalformedJson_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("{");

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid JSON payload.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidIntervalFormat_ReturnsBadRequest()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":"UsersDetails",
                                                                                      "interval":"not-an-interval"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionHttpTestFixture.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid interval format.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public void CreateRecoveryRequest_UsesFunctionAuthorizationLevel()
    {
        MethodInfo method = typeof(RecoveryFunction).GetMethod(nameof(RecoveryFunction.CreateRecoveryRequest))!;
        ParameterInfo requestParameter = method.GetParameters()
                                               .Single(p => p.ParameterType == typeof(HttpRequestData));

        CustomAttributeData trigger =
            requestParameter.CustomAttributes.Single(a => a.AttributeType.FullName
                                                          == "Microsoft.Azure.Functions.Worker.HttpTriggerAttribute");

        int authLevelValue = (int)trigger.ConstructorArguments[0].Value!;
        Assert.Equal(2, authLevelValue);// AuthorizationLevel.Function
    }
}
