using System.Net;

using Application.Contracts.InternalApis.Recovery;

using FunctionApp.Http;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpSuccessTests
{
    [Fact]
    public async Task Post_ValidIntervalPayload_ReturnsCreated()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator();

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req =
            RecoveryFunctionHttpTestFixture.CreateRequest("{\n"
                                                          + "  \"lob\":\"crc\",\n"
                                                          + "  \"category\":\"ConversationsDetails\",\n"
                                                          + "  \"interval\":\"2025-01-01T00:00Z/2025-12-31T23:59Z\"\n"
                                                          + "}");

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Equal(RecoveryCategory.ConversationsDetails, mediator.LastCommand!.Category);
        Assert.NotNull(mediator.LastCommand.Interval);
    }

    [Fact]
    public async Task Post_ValidGenesysJobIdOnlyPayload_WithAsyncMediatorContinuation_ReturnsCreated()
    {
        RecoveryFunctionHttpTestFixture.StubMediator mediator = new RecoveryFunctionHttpTestFixture.StubMediator
                                                                {
                                                                    OnSend = async (_, _) =>
                                                                             {
                                                                                 await Task.Yield();

                                                                                 return new
                                                                                     CreateRecoveryRequestResponse(true,
                                                                                      "ok",
                                                                                      new {});
                                                                             }
                                                                };

        RecoveryFunction sut = RecoveryFunctionHttpTestFixture.CreateSut(mediator);
        FakeHttpRequestData req =
            RecoveryFunctionHttpTestFixture.CreateRequest("{\n"
                                                          + "  \"lob\":\"lcl\",\n"
                                                          + "  \"category\":\"UsersDetails\",\n"
                                                          + "  \"genesysJobId\":\"JOB-123\"\n"
                                                          + "}");

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Null(mediator.LastCommand!.Interval);
        Assert.Equal("JOB-123", mediator.LastCommand.GenesysJobId);
    }
}
