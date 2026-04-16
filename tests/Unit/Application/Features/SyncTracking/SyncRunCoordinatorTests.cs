using Application.Abstractions.Persistence;
using Application.Features.SyncTracking.Shared;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class SyncRunCoordinatorTests
{
    [Fact]
    public async Task StartNewRunAsync_DelegatesToRepository()
    {
        const long requestId = 11L;
        const long expectedRunId = 88L;

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.StartNewRunAsync(requestId, CancellationToken.None))
                         .ReturnsAsync(expectedRunId);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        long actualRunId = await sut.StartNewRunAsync(requestId, CancellationToken.None);

        Assert.Equal(expectedRunId, actualRunId);
        syncRunRepository.Verify(x => x.StartNewRunAsync(requestId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task IsCurrentRunAsync_DelegatesToRepository()
    {
        const long runId = 22L;

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.IsCurrentRunAsync(runId, CancellationToken.None))
                         .ReturnsAsync(true);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        bool actual = await sut.IsCurrentRunAsync(runId, CancellationToken.None);

        Assert.True(actual);
        syncRunRepository.Verify(x => x.IsCurrentRunAsync(runId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task MarkCompletedAsync_DelegatesToRepository()
    {
        const long runId = 33L;

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.MarkCompletedAsync(runId, CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        await sut.MarkCompletedAsync(runId, CancellationToken.None);

        syncRunRepository.Verify(x => x.MarkCompletedAsync(runId, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task MarkFailedAsync_DelegatesToRepository()
    {
        const long runId = 44L;
        const string reason = "boom";

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.MarkFailedAsync(runId, reason, CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        await sut.MarkFailedAsync(runId, reason, CancellationToken.None);

        syncRunRepository.Verify(x => x.MarkFailedAsync(runId, reason, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task MarkSupersededAsync_DelegatesToRepository()
    {
        const long runId = 55L;
        const long supersededByRunId = 66L;

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.MarkSupersededAsync(runId, supersededByRunId, CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        await sut.MarkSupersededAsync(runId, supersededByRunId, CancellationToken.None);

        syncRunRepository.Verify(x => x.MarkSupersededAsync(runId, supersededByRunId, CancellationToken.None),
                                 Times.Once);
    }

    [Fact]
    public async Task MarkCanceledAsync_DelegatesToRepository()
    {
        const long runId = 77L;
        const string reason = "cancel";

        Mock<ISyncRunRepository> syncRunRepository = new Mock<ISyncRunRepository>(MockBehavior.Strict);
        syncRunRepository.Setup(x => x.MarkCanceledAsync(runId, reason, CancellationToken.None))
                         .Returns(Task.CompletedTask);

        SyncRunCoordinator sut = new SyncRunCoordinator(syncRunRepository.Object);

        await sut.MarkCanceledAsync(runId, reason, CancellationToken.None);

        syncRunRepository.Verify(x => x.MarkCanceledAsync(runId, reason, CancellationToken.None), Times.Once);
    }
}
