using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsIncrementalSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_ReturnsIncrementalRequestId_AndDoesNotCreateRecovery()
    {
        SyncRequestResolveResult incrementalResult = BuildResolveResult(101L, SyncRequestResolveAction.Created);

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   1,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(incrementalResult);

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(101L, It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

        RunAnalyticsIncrementalSyncCommandHandler sut =
            new RunAnalyticsIncrementalSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   1);

        long result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(101L, result);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Incremental,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    1,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(101L, It.IsAny<CancellationToken>()), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_CallerCanceled_RethrowsOperationCanceledException_WithoutRecovery()
    {
        SyncRequestResolveResult incrementalResult = BuildResolveResult(201L, SyncRequestResolveAction.ReusedActive);

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(incrementalResult);

        OperationCanceledException cancellation = new OperationCanceledException("caller canceled");

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(201L, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromException(cancellation));

        RunAnalyticsIncrementalSyncCommandHandler sut =
            new RunAnalyticsIncrementalSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   null);

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Handle(command, cts.Token));

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Incremental,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(201L, It.IsAny<CancellationToken>()), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_OrchestrationCanceled_RethrowsOperationCanceledException_WithoutRecovery()
    {
        SyncRequestResolveResult incrementalResult = BuildResolveResult(301L, SyncRequestResolveAction.ReusedActive);

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(incrementalResult);

        OperationCanceledException cancellation = new OperationCanceledException("orchestration canceled");

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(301L, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromException(cancellation));

        RunAnalyticsIncrementalSyncCommandHandler sut =
            new RunAnalyticsIncrementalSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   null);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Handle(command, CancellationToken.None));

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Incremental,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(301L, It.IsAny<CancellationToken>()), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_AnalyticsFailure_ResolvesRecoveryScope_AndRethrowsOriginalException()
    {
        SyncRequestResolveResult incrementalResult = BuildResolveResult(401L, SyncRequestResolveAction.Created);
        SyncRequestResolveResult recoveryResult = BuildResolveResult(402L, SyncRequestResolveAction.ReusedActive);

        InvalidOperationException original = new InvalidOperationException("incremental failed");

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                   SyncMode.Incremental,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   2,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(incrementalResult);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                   SyncMode.Recovery,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   2,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(recoveryResult);

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(401L, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromException(original));

        RunAnalyticsIncrementalSyncCommandHandler sut =
            new RunAnalyticsIncrementalSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   2);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                    SyncMode.Incremental,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    2,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                    SyncMode.Recovery,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    2,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(401L, It.IsAny<CancellationToken>()), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_AnalyticsFailure_RecoveryResolutionFails_StillRethrowsOriginalException()
    {
        SyncRequestResolveResult incrementalResult = BuildResolveResult(501L, SyncRequestResolveAction.Created);

        InvalidOperationException original = new InvalidOperationException("incremental failed");

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(incrementalResult);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ThrowsAsync(new Exception("recovery resolution failed"));

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(501L, It.IsAny<CancellationToken>()))
                         .Returns(Task.FromException(original));

        RunAnalyticsIncrementalSyncCommandHandler sut =
            new RunAnalyticsIncrementalSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Incremental,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Recovery,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    null,
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(501L, It.IsAny<CancellationToken>()), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    #region ========== *** Private Section *** ==========

    private static SyncRequestResolveResult BuildResolveResult(long id, SyncRequestResolveAction action)
    {
        return new SyncRequestResolveResult
               {
                   Id = id,
                   PublicId = Guid.NewGuid(),
                   RequestAction = action
               };
    }

    #endregion
}
