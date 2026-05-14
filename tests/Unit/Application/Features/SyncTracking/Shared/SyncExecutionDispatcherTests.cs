using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.References;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Features.SyncTracking.Shared;

using Moq;

using SharedKernel.Sync;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.Shared;

public sealed class SyncExecutionDispatcherTests
{
    [Fact]
    public async Task ExecuteAsync_WhenReferenceFull_RunsOrchestrator_AndWritesRunningThenCompleted()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 10L;
        const string category = nameof(SyncReferenceCategory.Group);
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        const int pageNumber = 2;
        const string genesysJobId = "JOB-100";
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Full),
                                                       interval,
                                                       pageNumber,
                                                       genesysJobId);

        MockSequence sequence = new MockSequence();

        runItemRepository.InSequence(sequence)
                         .Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.InSequence(sequence)
                                  .Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.Group, ct))
                                  .Returns(Task.CompletedTask);

        runItemRepository.InSequence(sequence)
                         .Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Completed,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object, []);

        SyncExecutionResult actual = await sut.ExecuteAsync(runId,
                                                            category,
                                                            SyncMode.Full,
                                                            interval,
                                                            pageNumber,
                                                            genesysJobId,
                                                            ct);

        Assert.False(actual.CompletedWithRecoveryItems);
        Assert.False(actual.Failed);
        referencesSyncOrchestrator.VerifyAll();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReferenceModeIsNotFull_WritesFailedRunItem_AndThrowsNotSupported()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 20L;
        const string category = nameof(SyncReferenceCategory.Group);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Recovery),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Failed,
                                                   It.Is<string>(m => m.Contains("Full mode only",
                                                                     StringComparison.Ordinal)),
                                                   CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object, []);

        NotSupportedException ex =
                await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(runId,
                                                                    category,
                                                                    SyncMode.Recovery,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct));

        Assert.Contains("Full mode only", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                              It.IsAny<SyncReferenceCategory>(),
                                                              It.IsAny<CancellationToken>()),
                                          Times.Never);
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalyticsIncremental_RunsMatchingExecutor_AndPreservesResult()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);
        Mock<IAnalyticsSyncExecutor> conversationsExecutor =
                BuildAnalyticsExecutor(SyncAnalyticsCategory.ConversationsDetails);

        const long runId = 30L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        CancellationToken ct = new CancellationTokenSource().Token;
        SyncExecutionResult expected = new SyncExecutionResult(CompletedWithRecoveryItems: true);

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       interval,
                                                       null,
                                                       null);

        MockSequence sequence = new MockSequence();

        runItemRepository.InSequence(sequence)
                         .Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        usersDetailsExecutor.InSequence(sequence)
                            .Setup(x => x.ExecuteAsync(runId,
                                                       SyncMode.Incremental,
                                                       interval,
                                                       null,
                                                       null,
                                                       ct))
                            .ReturnsAsync(expected);

        runItemRepository.InSequence(sequence)
                         .Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Completed,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [
                                                                      usersDetailsExecutor.Object,
                                                                      conversationsExecutor.Object
                                                                  ]);

        SyncExecutionResult actual = await sut.ExecuteAsync(runId,
                                                            category,
                                                            SyncMode.Incremental,
                                                            interval,
                                                            null,
                                                            null,
                                                            ct);

        Assert.Same(expected, actual);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.VerifyAll();
        conversationsExecutor.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                         It.IsAny<SyncMode>(),
                                                         It.IsAny<string?>(),
                                                         It.IsAny<int?>(),
                                                         It.IsAny<string?>(),
                                                         It.IsAny<CancellationToken>()),
                                     Times.Never);
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalyticsRecovery_RunsMatchingExecutor_WithPageNumber()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);

        const long runId = 31L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        const int pageNumber = 7;
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Recovery),
                                                       interval,
                                                       pageNumber,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        usersDetailsExecutor.Setup(x => x.ExecuteAsync(runId,
                                                       SyncMode.Recovery,
                                                       interval,
                                                       pageNumber,
                                                       null,
                                                       ct))
                            .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Completed,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [usersDetailsExecutor.Object]);

        SyncExecutionResult actual = await sut.ExecuteAsync(runId,
                                                            category,
                                                            SyncMode.Recovery,
                                                            interval,
                                                            pageNumber,
                                                            null,
                                                            ct);

        Assert.False(actual.CompletedWithRecoveryItems);
        Assert.False(actual.Failed);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.VerifyAll();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task
            ExecuteAsync_WhenAnalyticsModeIsNotIncrementalOrRecovery_WritesFailedRunItem_AndThrowsNotSupported()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);

        const long runId = 40L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Full),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Failed,
                                                   It.Is<string>(m => m.Contains("Incremental or Recovery mode only",
                                                                     StringComparison.Ordinal)),
                                                   CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [usersDetailsExecutor.Object]);

        NotSupportedException ex =
                await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(runId,
                                                                    category,
                                                                    SyncMode.Full,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct));

        Assert.Contains("Incremental or Recovery mode only", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                        It.IsAny<SyncMode>(),
                                                        It.IsAny<string?>(),
                                                        It.IsAny<int?>(),
                                                        It.IsAny<string?>(),
                                                        It.IsAny<CancellationToken>()),
                                    Times.Never);
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalyticsExecutorIsMissing_WritesFailedRunItem_AndThrowsNotSupported()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 41L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Failed,
                                                   It.Is<string>(m => m.Contains("No analytics executor is registered",
                                                                     StringComparison.Ordinal)),
                                                   CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object, []);

        NotSupportedException ex =
                await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(runId,
                                                                    category,
                                                                    SyncMode.Incremental,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct));

        Assert.Contains("No analytics executor is registered", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutorReturnsFailedResult_WritesFailedDispatchItem_AndReturnsResult()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);

        const long runId = 50L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        const string failureReason = "known terminal failure";
        CancellationToken ct = new CancellationTokenSource().Token;

        SyncExecutionResult expected = new SyncExecutionResult(false, true, failureReason);

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        usersDetailsExecutor.Setup(x => x.ExecuteAsync(runId,
                                                       SyncMode.Incremental,
                                                       null,
                                                       null,
                                                       null,
                                                       ct))
                            .ReturnsAsync(expected);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Failed,
                                                   failureReason,
                                                   ct))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [usersDetailsExecutor.Object]);

        SyncExecutionResult actual = await sut.ExecuteAsync(runId,
                                                            category,
                                                            SyncMode.Incremental,
                                                            null,
                                                            null,
                                                            null,
                                                            ct);

        Assert.Same(expected, actual);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.VerifyAll();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutorThrowsOperationCanceled_WritesCanceledRunItem_AndRethrows()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);

        const long runId = 60L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;
        OperationCanceledException expected = new OperationCanceledException("caller canceled");

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        usersDetailsExecutor.Setup(x => x.ExecuteAsync(runId,
                                                       SyncMode.Incremental,
                                                       null,
                                                       null,
                                                       null,
                                                       ct))
                            .ThrowsAsync(expected);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Canceled,
                                                   expected.Message,
                                                   CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [usersDetailsExecutor.Object]);

        OperationCanceledException actual =
                await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(runId,
                                                                         category,
                                                                         SyncMode.Incremental,
                                                                         null,
                                                                         null,
                                                                         null,
                                                                         ct));

        Assert.Same(expected, actual);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.VerifyAll();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenExecutorThrowsException_WritesFailedRunItem_AndRethrows()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);
        Mock<IAnalyticsSyncExecutor> usersDetailsExecutor = BuildAnalyticsExecutor(SyncAnalyticsCategory.UsersDetails);

        const long runId = 70L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;
        InvalidOperationException expected = new InvalidOperationException("dispatch failed");

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct))
                         .Returns(Task.CompletedTask);

        usersDetailsExecutor.Setup(x => x.ExecuteAsync(runId,
                                                       SyncMode.Incremental,
                                                       null,
                                                       null,
                                                       null,
                                                       ct))
                            .ThrowsAsync(expected);

        runItemRepository.Setup(x => x.UpsertAsync(runId,
                                                   SyncRunItemSteps.Dispatch,
                                                   scopeKey,
                                                   SyncRunStatus.Failed,
                                                   expected.Message,
                                                   CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut = new SyncExecutionDispatcher(runItemRepository.Object,
                                                                  referencesSyncOrchestrator.Object,
                                                                  [usersDetailsExecutor.Object]);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(runId,
                                                                        category,
                                                                        SyncMode.Incremental,
                                                                        null,
                                                                        null,
                                                                        null,
                                                                        ct));

        Assert.Same(expected, actual);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        usersDetailsExecutor.VerifyAll();
        runItemRepository.VerifyAll();
    }

    #region ========== *** Private Section *** ==========

    private static Mock<IAnalyticsSyncExecutor> BuildAnalyticsExecutor(SyncAnalyticsCategory category)
    {
        Mock<IAnalyticsSyncExecutor> executor = new Mock<IAnalyticsSyncExecutor>(MockBehavior.Strict);
        executor.SetupGet(x => x.Category)
                .Returns(category);

        return executor;
    }

    #endregion
}
