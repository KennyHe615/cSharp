using Application.Abstractions.Orchestration;
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

        string scopeKey =
                        SyncScopeKeyFormatter.Format(category,
                                                     nameof(SyncMode.Full),
                                                     interval,
                                                     pageNumber,
                                                     genesysJobId);

        MockSequence sequence = new MockSequence();

        runItemRepository.InSequence(sequence)
                         .Setup(x =>
                                x.UpsertAsync(runId,
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
                         .Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Completed,
                                              null,
                                              ct))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                        new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object);

        SyncExecutionResult actual =
                        await sut.ExecuteAsync(runId,
                                               category,
                                               SyncMode.Full,
                                               interval,
                                               pageNumber,
                                               genesysJobId,
                                               ct);

        Assert.False(actual.CompletedWithRecoveryItems);
        runItemRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
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

        string scopeKey =
                        SyncScopeKeyFormatter.Format(category,
                                                     nameof(SyncMode.Recovery),
                                                     null,
                                                     null,
                                                     null);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Running,
                                              null,
                                              ct))
                         .Returns(Task.CompletedTask);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Failed,
                                              It.Is<string>(m =>
                                                                            m.Contains("Full mode only",
                                                                                StringComparison.Ordinal)),
                                              CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                        new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object);

        NotSupportedException ex =
                        await Assert.ThrowsAsync<NotSupportedException>(() =>
                                                                                        sut.ExecuteAsync(runId,
                                                                                            category,
                                                                                            SyncMode.Recovery,
                                                                                            null,
                                                                                            null,
                                                                                            null,
                                                                                            ct));

        Assert.Contains("Full mode only", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.Verify(x =>
                                                          x.ExecuteAsync(It.IsAny<long>(),
                                                                         It.IsAny<SyncReferenceCategory>(),
                                                                         It.IsAny<CancellationToken>()),
                                          Times.Never);
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalyticsCategory_WritesFailedRunItem_AndThrowsNotSupported()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                        new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 30L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey =
                        SyncScopeKeyFormatter.Format(category,
                                                     nameof(SyncMode.Incremental),
                                                     null,
                                                     null,
                                                     null);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Running,
                                              null,
                                              ct))
                         .Returns(Task.CompletedTask);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Failed,
                                              It.Is<string>(m =>
                                                                            m.Contains("Analytics dispatch is temporarily disabled",
                                                                                StringComparison.Ordinal)),
                                              CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                        new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object);

        NotSupportedException ex =
                        await Assert.ThrowsAsync<NotSupportedException>(() =>
                                                                                        sut.ExecuteAsync(runId,
                                                                                            category,
                                                                                            SyncMode.Incremental,
                                                                                            null,
                                                                                            null,
                                                                                            null,
                                                                                            ct));

        Assert.Contains("Analytics dispatch is temporarily disabled", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        runItemRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorThrowsOperationCanceled_WritesCanceledRunItem_AndRethrows()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                        new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 40L;
        const string category = nameof(SyncReferenceCategory.Skill);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey =
                        SyncScopeKeyFormatter.Format(category,
                                                     nameof(SyncMode.Full),
                                                     null,
                                                     null,
                                                     null);
        OperationCanceledException expected = new OperationCanceledException("caller canceled");

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Running,
                                              null,
                                              ct))
                         .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.Skill, ct))
                                  .ThrowsAsync(expected);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Canceled,
                                              expected.Message,
                                              CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                        new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object);

        OperationCanceledException actual =
                        await Assert.ThrowsAsync<OperationCanceledException>(() =>
                                                                                             sut.ExecuteAsync(runId,
                                                                                                 category,
                                                                                                 SyncMode.Full,
                                                                                                 null,
                                                                                                 null,
                                                                                                 null,
                                                                                                 ct));

        Assert.Same(expected, actual);
        runItemRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorThrowsException_WritesFailedRunItem_AndRethrows()
    {
        Mock<ISyncRunItemRepository> runItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
                        new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 50L;
        const string category = nameof(SyncReferenceCategory.PresenceDefinition);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey =
                        SyncScopeKeyFormatter.Format(category,
                                                     nameof(SyncMode.Full),
                                                     null,
                                                     null,
                                                     null);
        InvalidOperationException expected = new InvalidOperationException("dispatch failed");

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Running,
                                              null,
                                              ct))
                         .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.PresenceDefinition, ct))
                                  .ThrowsAsync(expected);

        runItemRepository.Setup(x =>
                                x.UpsertAsync(runId,
                                              SyncRunItemSteps.Dispatch,
                                              scopeKey,
                                              SyncRunStatus.Failed,
                                              expected.Message,
                                              CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
                        new SyncExecutionDispatcher(runItemRepository.Object, referencesSyncOrchestrator.Object);

        InvalidOperationException actual =
                        await Assert.ThrowsAsync<InvalidOperationException>(() =>
                                                                                            sut.ExecuteAsync(runId,
                                                                                                category,
                                                                                                SyncMode.Full,
                                                                                                null,
                                                                                                null,
                                                                                                null,
                                                                                                ct));

        Assert.Same(expected, actual);
        runItemRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
    }
}
