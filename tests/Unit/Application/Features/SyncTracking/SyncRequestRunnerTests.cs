using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Shared;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class SyncRequestRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_Success_DispatchesAndMarksCompleted()
    {
        const long requestId = 100L;
        const long runId = 10L;
        CancellationToken ct = CancellationToken.None;

        SyncRequestDto request = BuildRequest(requestId);

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, ct))
                             .ReturnsAsync(request);

        syncRunCoordinator.Setup(x => x.StartNewRunAsync(requestId, ct))
                          .ReturnsAsync(runId);

        syncRunCoordinator.Setup(x => x.IsCurrentRunAsync(runId, ct))
                          .ReturnsAsync(true);

        syncExecutionDispatcher.Setup(x => x.ExecuteAsync(runId,
                                                          request.Category,
                                                          request.Mode,
                                                          request.Interval,
                                                          request.PageNumber,
                                                          request.GenesysJobId,
                                                          ct))
                               .Returns(Task.CompletedTask);

        syncRunCoordinator.Setup(x => x.MarkCompletedAsync(runId, ct))
                          .Returns(Task.CompletedTask);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        await sut.ExecuteAsync(requestId, ct);

        syncRequestRepository.VerifyAll();
        syncRunCoordinator.VerifyAll();
        syncExecutionDispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_NotCurrentRun_ReturnsWithoutDispatchOrFinalStatus()
    {
        const long requestId = 200L;
        const long runId = 20L;
        CancellationToken ct = CancellationToken.None;

        SyncRequestDto request = BuildRequest(requestId);

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, ct))
                             .ReturnsAsync(request);

        syncRunCoordinator.Setup(x => x.StartNewRunAsync(requestId, ct))
                          .ReturnsAsync(runId);

        syncRunCoordinator.Setup(x => x.IsCurrentRunAsync(runId, ct))
                          .ReturnsAsync(false);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        await sut.ExecuteAsync(requestId, ct);

        syncExecutionDispatcher.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<SyncMode>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<int?>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<CancellationToken>()),
                                       Times.Never);

        syncRunCoordinator.Verify(x => x.MarkCompletedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncRunCoordinator.Verify(x => x.MarkFailedAsync(It.IsAny<long>(),
                                                         It.IsAny<string>(),
                                                         It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncRunCoordinator.Verify(x => x.MarkCanceledAsync(It.IsAny<long>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<CancellationToken>()),
                                  Times.Never);

        syncRequestRepository.VerifyAll();
        syncRunCoordinator.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_DispatchThrowsException_MarksFailedWithCancellationTokenNone_AndRethrows()
    {
        const long requestId = 300L;
        const long runId = 30L;
        using CancellationTokenSource cts = new CancellationTokenSource();
        CancellationToken ct = cts.Token;

        SyncRequestDto request = BuildRequest(requestId);
        InvalidOperationException expected = new InvalidOperationException("dispatch failed");

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, ct))
                             .ReturnsAsync(request);

        syncRunCoordinator.Setup(x => x.StartNewRunAsync(requestId, ct))
                          .ReturnsAsync(runId);

        syncRunCoordinator.Setup(x => x.IsCurrentRunAsync(runId, ct))
                          .ReturnsAsync(true);

        syncExecutionDispatcher.Setup(x => x.ExecuteAsync(runId,
                                                          request.Category,
                                                          request.Mode,
                                                          request.Interval,
                                                          request.PageNumber,
                                                          request.GenesysJobId,
                                                          ct))
                               .ThrowsAsync(expected);

        syncRunCoordinator.Setup(x => x.MarkFailedAsync(runId, expected.Message, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(requestId, ct));

        Assert.Same(expected, actual);

        syncRunCoordinator.Verify(x => x.MarkFailedAsync(runId, expected.Message, CancellationToken.None), Times.Once);
        syncRunCoordinator.Verify(x => x.MarkFailedAsync(runId, expected.Message, ct), Times.Never);
        syncRunCoordinator.Verify(x => x.MarkCompletedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncRunCoordinator.Verify(x => x.MarkCanceledAsync(It.IsAny<long>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<CancellationToken>()),
                                  Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_CallerCancellation_MarksCanceledByHostAndRethrows()
    {
        const long requestId = 400L;
        const long runId = 40L;

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();
        CancellationToken callerCt = cts.Token;

        SyncRequestDto request = BuildRequest(requestId);
        OperationCanceledException expected = new OperationCanceledException("caller canceled");

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, callerCt))
                             .ReturnsAsync(request);

        syncRunCoordinator.Setup(x => x.StartNewRunAsync(requestId, callerCt))
                          .ReturnsAsync(runId);

        syncRunCoordinator.Setup(x => x.IsCurrentRunAsync(runId, callerCt))
                          .ReturnsAsync(true);

        syncExecutionDispatcher.Setup(x => x.ExecuteAsync(runId,
                                                          request.Category,
                                                          request.Mode,
                                                          request.Interval,
                                                          request.PageNumber,
                                                          request.GenesysJobId,
                                                          callerCt))
                               .ThrowsAsync(expected);

        syncRunCoordinator.Setup(x => x.MarkCanceledAsync(runId,
                                                          "Canceled by host/user request.",
                                                          CancellationToken.None))
                          .Returns(Task.CompletedTask);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        OperationCanceledException actual =
            await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(requestId, callerCt));

        Assert.Same(expected, actual);

        syncRunCoordinator.Verify(x => x.MarkFailedAsync(It.IsAny<long>(),
                                                         It.IsAny<string>(),
                                                         It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncRunCoordinator.Verify(x => x.MarkCompletedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                                  Times.Never);

        syncRequestRepository.VerifyAll();
        syncRunCoordinator.VerifyAll();
        syncExecutionDispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_OrchestrationCancellation_MarksCanceledBySignalAndRethrows()
    {
        const long requestId = 500L;
        const long runId = 50L;
        CancellationToken ct = CancellationToken.None;

        SyncRequestDto request = BuildRequest(requestId);
        OperationCanceledException expected = new OperationCanceledException("orchestration canceled");

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, ct))
                             .ReturnsAsync(request);

        syncRunCoordinator.Setup(x => x.StartNewRunAsync(requestId, ct))
                          .ReturnsAsync(runId);

        syncRunCoordinator.Setup(x => x.IsCurrentRunAsync(runId, ct))
                          .ReturnsAsync(true);

        syncExecutionDispatcher.Setup(x => x.ExecuteAsync(runId,
                                                          request.Category,
                                                          request.Mode,
                                                          request.Interval,
                                                          request.PageNumber,
                                                          request.GenesysJobId,
                                                          ct))
                               .ThrowsAsync(expected);

        syncRunCoordinator.Setup(x => x.MarkCanceledAsync(runId,
                                                          "Canceled by orchestration signal.",
                                                          CancellationToken.None))
                          .Returns(Task.CompletedTask);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        OperationCanceledException actual =
            await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(requestId, ct));

        Assert.Same(expected, actual);

        syncRunCoordinator.Verify(x => x.MarkFailedAsync(It.IsAny<long>(),
                                                         It.IsAny<string>(),
                                                         It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncRunCoordinator.Verify(x => x.MarkCompletedAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                                  Times.Never);

        syncRequestRepository.VerifyAll();
        syncRunCoordinator.VerifyAll();
        syncExecutionDispatcher.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_RequestNotFound_ThrowsInvalidOperationException()
    {
        const long requestId = 999L;
        CancellationToken ct = CancellationToken.None;

        Mock<ISyncRunCoordinator> syncRunCoordinator = new Mock<ISyncRunCoordinator>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncExecutionDispatcher> syncExecutionDispatcher =
            new Mock<ISyncExecutionDispatcher>(MockBehavior.Strict);

        syncRequestRepository.Setup(x => x.GetByIdAsync(requestId, ct))
                             .ReturnsAsync((SyncRequestDto?)null);

        SyncRequestRunner sut = new SyncRequestRunner(syncRunCoordinator.Object,
                                                      syncRequestRepository.Object,
                                                      syncExecutionDispatcher.Object);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(requestId, ct));

        Assert.Contains("Sync request '999' was not found.", ex.Message, StringComparison.Ordinal);

        syncRunCoordinator.Verify(x => x.StartNewRunAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()),
                                  Times.Never);
        syncExecutionDispatcher.Verify(x => x.ExecuteAsync(It.IsAny<long>(),
                                                           It.IsAny<string>(),
                                                           It.IsAny<SyncMode>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<int?>(),
                                                           It.IsAny<string?>(),
                                                           It.IsAny<CancellationToken>()),
                                       Times.Never);

        syncRequestRepository.VerifyAll();
    }

    #region ========== *** Private Section *** ==========

    private static SyncRequestDto BuildRequest(long id)
    {
        return new SyncRequestDto
               {
                   Id = id,
                   Category = nameof(SyncReferenceCategory.Group),
                   Mode = SyncMode.Incremental,
                   Interval = "2026-01-01T00:00Z/2026-01-01T00:30Z",
                   PageNumber = null,
                   GenesysJobId = null,
                   ScopeKey = "Group|Incremental|2026-01-01T00:00Z/2026-01-01T00:30Z|-|-",
                   CurrentRunId = null
               };
    }

    #endregion
}
