using Ntt.Analytics.Scheduling;

using tests.TestSupport.Logging;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics.Scheduling;

public sealed class ScheduledWorkerLoopRunnerTests
{
    [Fact]
    public async Task RunPeriodicAsync_RunsWorkImmediatelyBeforeWaitingForNextTick()
    {
        ScheduledWorkerLoopRunner sut = new ScheduledWorkerLoopRunner(new TestLogger<ScheduledWorkerLoopRunner>());

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        int runCount = 0;

        Task runTask = sut.RunPeriodicAsync("Test worker",
                                            TimeSpan.FromHours(1),
                                            _ =>
                                            {
                                                runCount++;

                                                return Task.CompletedTask;
                                            },
                                            cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        Assert.Equal(1, runCount);
    }

    [Fact]
    public async Task RunPeriodicAsync_WhenWorkThrows_LogsAndContinuesUntilCanceled()
    {
        ScheduledWorkerLoopRunner sut = new ScheduledWorkerLoopRunner(new TestLogger<ScheduledWorkerLoopRunner>());

        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        int runCount = 0;

        Task runTask = sut.RunPeriodicAsync("Test worker",
                                            TimeSpan.FromMilliseconds(1),
                                            _ =>
                                            {
                                                runCount++;

                                                return runCount == 1
                                                               ? throw new
                                                                         InvalidOperationException("First attempt failed.")
                                                               : Task.CompletedTask;
                                            },
                                            cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => runTask);

        Assert.True(runCount >= 2);
    }
}
