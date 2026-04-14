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
    public async Task ExecuteAsync_WhenReferenceIncremental_RunsOrchestrator_AndWritesRunningThenCompleted()
    {
        Mock<ISyncCheckpointRepository> checkpointRepository = new Mock<ISyncCheckpointRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
            new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 10L;
        const string category = nameof(SyncReferenceCategory.Group);
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        const int pageNumber = 2;
        const string genesysJobId = "JOB-100";
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       interval,
                                                       pageNumber,
                                                       genesysJobId);

        MockSequence sequence = new MockSequence();

        checkpointRepository.InSequence(sequence)
                            .Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Running,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.InSequence(sequence)
                                  .Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.Group, ct))
                                  .Returns(Task.CompletedTask);

        checkpointRepository.InSequence(sequence)
                            .Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Completed,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
            new SyncExecutionDispatcher(checkpointRepository.Object, referencesSyncOrchestrator.Object);

        await sut.ExecuteAsync(runId,
                               category,
                               SyncMode.Incremental,
                               interval,
                               pageNumber,
                               genesysJobId,
                               ct);

        checkpointRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReferenceModeIsNotIncremental_WritesFailedCheckpoint_AndThrowsNotSupported()
    {
        Mock<ISyncCheckpointRepository> checkpointRepository = new Mock<ISyncCheckpointRepository>(MockBehavior.Strict);
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

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Running,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Failed,
                                                      It.Is<string>(m => m.Contains("Incremental mode only",
                                                                     StringComparison.Ordinal)),
                                                      CancellationToken.None))
                            .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
            new SyncExecutionDispatcher(checkpointRepository.Object, referencesSyncOrchestrator.Object);

        NotSupportedException ex =
            await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(runId,
                                                             category,
                                                             SyncMode.Recovery,
                                                             null,
                                                             null,
                                                             null,
                                                             ct));

        Assert.Contains("Incremental mode only", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                              It.IsAny<SyncReferenceCategory>(),
                                                              It.IsAny<CancellationToken>()),
                                          Times.Never);
        checkpointRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenAnalyticsCategory_WritesFailedCheckpoint_AndThrowsNotSupported()
    {
        Mock<ISyncCheckpointRepository> checkpointRepository = new Mock<ISyncCheckpointRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
            new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 30L;
        const string category = nameof(SyncAnalyticsCategory.UsersDetails);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Running,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Failed,
                                                      It.Is<string>(m =>
                                                                        m.Contains("Analytics dispatch is temporarily disabled",
                                                                         StringComparison.Ordinal)),
                                                      CancellationToken.None))
                            .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
            new SyncExecutionDispatcher(checkpointRepository.Object, referencesSyncOrchestrator.Object);

        NotSupportedException ex =
            await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(runId,
                                                             category,
                                                             SyncMode.Incremental,
                                                             null,
                                                             null,
                                                             null,
                                                             ct));

        Assert.Contains("Analytics dispatch is temporarily disabled", ex.Message, StringComparison.Ordinal);
        referencesSyncOrchestrator.VerifyNoOtherCalls();
        checkpointRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorThrowsOperationCanceled_WritesCanceledCheckpoint_AndRethrows()
    {
        Mock<ISyncCheckpointRepository> checkpointRepository = new Mock<ISyncCheckpointRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
            new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 40L;
        const string category = nameof(SyncReferenceCategory.Skill);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);
        OperationCanceledException expected = new OperationCanceledException("caller canceled");

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Running,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.Skill, ct))
                                  .ThrowsAsync(expected);

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Canceled,
                                                      expected.Message,
                                                      CancellationToken.None))
                            .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
            new SyncExecutionDispatcher(checkpointRepository.Object, referencesSyncOrchestrator.Object);

        OperationCanceledException actual =
            await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(runId,
                                                                  category,
                                                                  SyncMode.Incremental,
                                                                  null,
                                                                  null,
                                                                  null,
                                                                  ct));

        Assert.Same(expected, actual);
        checkpointRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenOrchestratorThrowsException_WritesFailedCheckpoint_AndRethrows()
    {
        Mock<ISyncCheckpointRepository> checkpointRepository = new Mock<ISyncCheckpointRepository>(MockBehavior.Strict);
        Mock<IReferencesSyncOrchestrator> referencesSyncOrchestrator =
            new Mock<IReferencesSyncOrchestrator>(MockBehavior.Strict);

        const long runId = 50L;
        const string category = nameof(SyncReferenceCategory.PresenceDefinition);
        CancellationToken ct = new CancellationTokenSource().Token;

        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       nameof(SyncMode.Incremental),
                                                       null,
                                                       null,
                                                       null);
        InvalidOperationException expected = new InvalidOperationException("dispatch failed");

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Running,
                                                      null,
                                                      ct))
                            .Returns(Task.CompletedTask);

        referencesSyncOrchestrator.Setup(x => x.ExecuteAsync(runId, SyncReferenceCategory.PresenceDefinition, ct))
                                  .ThrowsAsync(expected);

        checkpointRepository.Setup(x => x.UpsertAsync(runId,
                                                      SyncCheckpointSteps.Dispatch,
                                                      scopeKey,
                                                      SyncRunStatus.Failed,
                                                      expected.Message,
                                                      CancellationToken.None))
                            .Returns(Task.CompletedTask);

        SyncExecutionDispatcher sut =
            new SyncExecutionDispatcher(checkpointRepository.Object, referencesSyncOrchestrator.Object);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(runId,
                                                                 category,
                                                                 SyncMode.Incremental,
                                                                 null,
                                                                 null,
                                                                 null,
                                                                 ct));

        Assert.Same(expected, actual);
        checkpointRepository.VerifyAll();
        referencesSyncOrchestrator.VerifyAll();
    }
}
