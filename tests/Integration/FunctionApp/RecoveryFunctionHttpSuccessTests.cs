using System.Net;

using Application.Contracts.InternalApis.Recovery;

using FunctionApp.Http;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace Tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpSuccessTests
{
    [Fact]
    public async Task Post_ValidIntervalPayload_ReturnsCreated()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"crc",
                                                                                      "category":"ConversationsDetails",
                                                                                      "interval":"2025-01-01T00:00Z/2025-12-31T23:59Z"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Equal(RecoveryCategory.ConversationsDetails, mediator.LastCommand!.Category);
        Assert.NotNull(mediator.LastCommand.Interval);
    }

    [Fact]
    public async Task Post_ValidJobIdOnlyPayload_ReturnsCreated()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req = RecoveryFunctionHttpTestFixture.CreateRequest("""
                                                                                    {
                                                                                      "lob":"lcl",
                                                                                      "category":"UsersDetails",
                                                                                      "jobId":"JOB-123"
                                                                                    }
                                                                                    """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Null(mediator.LastCommand!.Interval);
        Assert.Equal("JOB-123", mediator.LastCommand.JobId);
    }
}
