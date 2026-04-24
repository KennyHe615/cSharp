using System.Net;

using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.SyncTracking;

using FunctionApps.Http;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpSuccessTests
{
    [Fact]
    public async Task Post_ValidIntervalPayload_WhenRequestCreated_ReturnsCreated()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator();

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
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
    public async Task Post_ValidGenesysJobIdOnlyPayload_WithAsyncMediatorContinuation_ReturnsCreated()
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend = async (_, _) =>
                                                     {
                                                         await Task.Yield();

                                                         return RecoveryFunctionTestFactory
                                                                .CreateResponse();
                                                     }
                                        };

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"lcl",
                                                                              "category":"ConversationsDetails",
                                                                              "GenesysJobId":"JOB-123"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Null(mediator.LastCommand!.Interval);
        Assert.Equal("JOB-123", mediator.LastCommand.GenesysJobId);
    }

    [Theory]
    [InlineData(SyncRequestResolveAction.ReusedActive)]
    [InlineData(SyncRequestResolveAction.ReusedFailed)]
    public async Task Post_WhenRequestReusedOrReopened_ReturnsAccepted(SyncRequestResolveAction action)
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend = (_, _) => Task.FromResult(
                                                             RecoveryFunctionTestFactory
                                                                    .CreateResponse(action))
                                        };

        RecoveryFunction sut = RecoveryFunctionTestFactory.Create(mediator);
        FakeHttpRequestData req = RecoveryFunctionTestFactory.CreateRequest("""
                                                                            {
                                                                              "lob":"crc",
                                                                              "category":"ConversationsDetails",
                                                                              "GenesysJobId":"JOB-123"
                                                                            }
                                                                            """);

        HttpResponseData response = await sut.CreateRecoveryRequest(req, CancellationToken.None);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        Assert.Equal(1, mediator.SendCount);
    }
}
