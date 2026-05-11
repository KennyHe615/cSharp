using Microsoft.Extensions.Logging;

using Moq;

using Ntt.Analytics;
using Ntt.Analytics.Scheduling;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics;

/// <summary>
/// Unit tests for <see cref="Worker"/>.
/// </summary>
public sealed class WorkerTests
{
    /// <summary>
    /// Verifies that the host worker requires non-null constructor dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithNullDependencies_ThrowsArgumentNullException()
    {
        Mock<ILogger<Worker>> logger = new Mock<ILogger<Worker>>(MockBehavior.Strict);
        IScheduledWorkerLoop[] loops = [];

        Assert.Throws<ArgumentNullException>(() => new Worker(null!, logger.Object));
        Assert.Throws<ArgumentNullException>(() => new Worker(loops, null!));
    }

    /// <summary>
    /// Verifies that the worker accepts valid scheduled loop registrations.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        Mock<ILogger<Worker>> logger = new Mock<ILogger<Worker>>(MockBehavior.Loose);
        IScheduledWorkerLoop[] loops = [];

        Worker sut = new Worker(loops, logger.Object);

        Assert.NotNull(sut);
    }

    /// <summary>
    /// Verifies that the host worker starts every registered scheduled loop.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithRegisteredLoops_StartsAllLoops()
    {
        TestScheduledWorkerLoop first = new TestScheduledWorkerLoop();
        TestScheduledWorkerLoop second = new TestScheduledWorkerLoop();

        Worker sut = new Worker([first, second], new Mock<ILogger<Worker>>(MockBehavior.Loose).Object);

        await sut.StartAsync(CancellationToken.None);
        await Task.WhenAll(first.Started.Task, second.Started.Task);

        Assert.Equal(1, first.RunCount);
        Assert.Equal(1, second.RunCount);
    }

    #region ========== *** Private Section *** ==========

    private sealed class TestScheduledWorkerLoop : IScheduledWorkerLoop
    {
        public TaskCompletionSource Started { get; } =
            new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        public int RunCount { get; private set; }

        public Task RunAsync(CancellationToken ct)
        {
            RunCount++;
            Started.TrySetResult();

            return Task.CompletedTask;
        }
    }

    #endregion
}
