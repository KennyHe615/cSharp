using System.Net;
using System.Reflection;

using FluentValidation;
using FluentValidation.Results;

using FunctionApps.Http;

using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpValidationTests
{
    [Fact]
    public async Task Post_MissingLob_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "category":"UsersDetails",
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Lob is required.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MissingCategory_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Category is required.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidLob_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"abc",
                                                                              "category":"UsersDetails",
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'lob': 'abc'.", error);

        foreach (string allowed in LobName.AllowedValues)
        {
            Assert.Contains(allowed, error);
        }

        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidEnumText_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"BadValue",
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_CategoryNumericString_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"1",
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_CategoryNumericNumber_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":1,
                                                                              "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid value for 'category'", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_UnsupportedJobIdField_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails",
                                                                              "jobId":"JOB-123"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Unsupported field 'jobId'.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MissingIntervalAndGenesysJobId_ReturnsBadRequest_FromValidationException()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend = (_, _) =>
                                                     {
                                                         List<ValidationFailure> failures =
                                                         [
                                                             new ValidationFailure("",
                                                                 "Either Interval or GenesysJobId must be provided.")
                                                         ];

                                                         throw new
                                                                         ValidationException(failures);
                                                     }
                                        };

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Either Interval or GenesysJobId must be provided.", error);
        Assert.Equal(1, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MalformedJson_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("{");

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid JSON payload.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_InvalidIntervalFormat_ReturnsBadRequest()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails",
                                                                              "interval":"not-an-interval"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid interval format.", error);
        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_WhenCanceledBeforeProcessing_ThrowsOperationCanceledException()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails",
                                                                              "GenesysJobId":"JOB-123"
                                                                            }
                                                                            """);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.CreateRecoveryRequest(req, cts.Token));

        Assert.Equal(0, mediator.SendCount);
    }

    [Fact]
    public async Task Post_MalformedJson_LogsWarningOnce_ForInvalidJsonPayloadMessage()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();
        CapturingLogger<RecoveryFunction> logger = new CapturingLogger<RecoveryFunction>();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator, logger);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("{");

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, mediator.SendCount);

        List<CapturingLogger<RecoveryFunction>.LogEntry> warningEntries =
                        logger.Entries.Where(e => e.Level == LogLevel.Warning)
                              .ToList();

        Assert.Contains(warningEntries, e => e.Message.Contains("Invalid JSON payload for recovery request."));
        Assert.Equal(1, warningEntries.Count(e => e.Message.Contains("Invalid JSON payload for recovery request.")));
    }

    [Fact]
    public async Task Post_UnexpectedException_ReturnsInternalServerError_AndLogsError()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend =
                                                            (_, _) =>
                                                                            throw new
                                                                                            InvalidOperationException("boom")
                                        };

        CapturingLogger<RecoveryFunction> logger = new CapturingLogger<RecoveryFunction>();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator, logger);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails",
                                                                              "GenesysJobId":"JOB-123"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);
        string error = RecoveryFunctionTestFactory.ReadError(response);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("An error occurred processing your request.", error);
        Assert.Equal(1, mediator.SendCount);

        Assert.Contains(logger.Entries,
                        e => e.Level == LogLevel.Error && e.Message.Contains("Error processing request."));
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Error && e.Message.Contains("Exception Summary:"));
    }

    [Fact]
    public async Task Post_ValidationException_DoesNotLogWarningOrError()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend = (_, _) => throw new ValidationException([
                                                                   new
                                                                                   ValidationFailure("",
                                                                                       "Either Interval or GenesysJobId must be provided.")
                                                               ])
                                        };

        CapturingLogger<RecoveryFunction> logger = new CapturingLogger<RecoveryFunction>();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator, logger);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"UsersDetails",
                                                                              "GenesysJobId":"JOB-123"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(logger.Entries, e => e.Level is LogLevel.Warning or LogLevel.Error or LogLevel.Critical);
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
        Assert.Equal(2, authLevelValue);
    }
}
