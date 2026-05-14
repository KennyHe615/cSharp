using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

using Ntt.Analytics;
using Ntt.Analytics.Scheduling;

using Xunit;


namespace tests.Unit.Hosts.AppServices.Ntt.Analytics;

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
        ServiceCollection services = [];
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        IServiceScopeFactory scopeFactory = serviceProvider.GetRequiredService<IServiceScopeFactory>();
        ILogger<Worker> logger = NullLogger<Worker>.Instance;

        Assert.Throws<ArgumentNullException>(() => new Worker(null!, logger));
        Assert.Throws<ArgumentNullException>(() => new Worker(scopeFactory, null!));
    }

    /// <summary>
    /// Verifies that the worker accepts valid scoped service provider dependencies.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        ServiceCollection services = [];
        using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Worker sut = new Worker(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                                NullLogger<Worker>.Instance);

        Assert.NotNull(sut);
    }

    /// <summary>
    /// Verifies that the host worker completes when no scheduled loops are registered.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenNoScheduledLoopsRegistered_CompletesWithoutError()
    {
        ServiceCollection services = [];
        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Worker sut = new Worker(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                                NullLogger<Worker>.Instance);

        await sut.StartAsync(CancellationToken.None);

        await sut.StopAsync(CancellationToken.None);
    }

    /// <summary>
    /// Verifies that the host worker starts every scheduled loop resolved from the service scope.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithRegisteredLoops_StartsAllLoops()
    {
        TestScheduledWorkerLoop first = new TestScheduledWorkerLoop();
        TestScheduledWorkerLoop second = new TestScheduledWorkerLoop();

        ServiceCollection services = [];
        services.AddSingleton<IScheduledWorkerLoop>(first);
        services.AddSingleton<IScheduledWorkerLoop>(second);

        await using ServiceProvider serviceProvider = services.BuildServiceProvider();

        Worker sut = new Worker(serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                                NullLogger<Worker>.Instance);

        await sut.StartAsync(CancellationToken.None);
        await Task.WhenAll(first.Started.Task, second.Started.Task);

        Assert.Equal(1, first.RunCount);
        Assert.Equal(1, second.RunCount);

        await sut.StopAsync(CancellationToken.None);
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
