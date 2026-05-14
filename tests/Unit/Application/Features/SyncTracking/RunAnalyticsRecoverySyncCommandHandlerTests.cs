using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Sync;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsRecoverySyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_AnalyticsCategoryWithInterval_ExecutesClaimedRequestAndReturnsRequestId()
    {
        const long requestId = 101L;
        CancellationToken ct = CancellationToken.None;

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(requestId, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(requestId,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    2,
                                                    null);

        long result = await sut.Handle(command, ct);

        Assert.Equal(requestId, result);

        syncRequestRunner.Verify(x => x.ExecuteAsync(requestId, ct), Times.Once);
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ConversationsDetailsWithGenesysJobId_ExecutesClaimedRequestAndReturnsRequestId()
    {
        const long requestId = 151L;
        CancellationToken ct = CancellationToken.None;

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(requestId, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(requestId,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        long result = await sut.Handle(command, ct);

        Assert.Equal(requestId, result);

        syncRequestRunner.Verify(x => x.ExecuteAsync(requestId, ct), Times.Once);
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_GenesysJobIdForNonConversationsDetails_ThrowsInvalidOperationException_WithoutRunnerCall()
    {
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(201L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal("GenesysJobId is only supported for ConversationsDetails recovery.", ex.Message);

        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException_WithoutRunnerCall()
    {
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(202L,
                                                    (SyncAnalyticsCategory)999,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Recovery mode is not supported for category", ex.Message);

        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_RunExecutionThrows_RethrowsSameException()
    {
        const long requestId = 301L;
        CancellationToken ct = CancellationToken.None;
        InvalidOperationException original = new InvalidOperationException("run failed");

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(requestId, ct))
                         .ThrowsAsync(original);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(requestId,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-999");

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, ct));

        Assert.Same(original, ex);

        syncRequestRunner.Verify(x => x.ExecuteAsync(requestId, ct), Times.Once);
        syncRequestRunner.VerifyNoOtherCalls();
    }
}
