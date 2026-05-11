using System.Net;
using System.Text.Json;

using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.Recovery;

using FunctionApps.Http;

using Microsoft.Azure.Functions.Worker.Http;

using tests.TestSupport.Functions;

using Xunit;


namespace tests.Integration.FunctionApp;

public sealed class RecoveryFunctionHttpSuccessTests
{
    /// <summary>
    /// Verifies that a valid interval payload creates a new recovery intake request.
    /// </summary>
    [Fact]
    public async Task Post_ValidIntervalPayload_WhenIntakeRequestCreated_ReturnsCreated()
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
        Assert.Equal(nameof(AnalyticsRecoveryRequestResolveAction.Created), ReadRequestAction(response));
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Equal(RecoveryCategory.ConversationsDetails, mediator.LastCommand!.Category);
        Assert.NotNull(mediator.LastCommand.Interval);
    }

    /// <summary>
    /// Verifies that a valid Genesys job payload creates a new recovery intake request.
    /// </summary>
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
        Assert.Equal(nameof(AnalyticsRecoveryRequestResolveAction.Created), ReadRequestAction(response));
        Assert.Equal(1, mediator.SendCount);
        Assert.NotNull(mediator.LastCommand);
        Assert.Null(mediator.LastCommand!.Interval);
        Assert.Equal("JOB-123", mediator.LastCommand.GenesysJobId);
    }

    /// <summary>
    /// Verifies that an active intake request is reused and returned as accepted.
    /// </summary>
    [Theory]
    [InlineData(AnalyticsRecoveryRequestResolveAction.ReusedActive)]
    public async Task Post_WhenIntakeRequestReused_ReturnsAccepted(AnalyticsRecoveryRequestResolveAction action)
    {
        FakeRecoveryMediator mediator = new FakeRecoveryMediator
                                        {
                                            OnSend =
                                                    (_, _) =>
                                                            Task
                                                                   .FromResult(RecoveryFunctionTestFactory
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
        Assert.Equal(nameof(AnalyticsRecoveryRequestResolveAction.ReusedActive), ReadRequestAction(response));
        Assert.Equal(1, mediator.SendCount);
    }

    #region ========== *** Private Section *** ==========

    private static string ReadRequestAction(HttpResponseData response)
    {
        using JsonDocument doc = JsonDocument.Parse(response.ReadBodyAsString());

        return doc.RootElement.GetProperty("Data")
                  .GetProperty("RequestAction")
                  .GetString()
               ?? string.Empty;
    }

    #endregion
}
