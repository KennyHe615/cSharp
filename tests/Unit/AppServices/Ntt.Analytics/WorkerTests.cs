using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
        Mock<IServiceScopeFactory> scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        Mock<ILogger<Worker>> logger = new Mock<ILogger<Worker>>(MockBehavior.Strict);

        IOptions<CronOrIntervalOptions> options = Options.Create(new CronOrIntervalOptions
                                                                 {
                                                                     UsersDetailsIncrementalIntervalMinutes = 30,
                                                                     UsersDetailsRecoveryIntervalHours = 3
                                                                 });

        Assert.Throws<ArgumentNullException>(() => new Worker(null!, options, logger.Object));
        Assert.Throws<ArgumentNullException>(() => new Worker(scopeFactory.Object, null!, logger.Object));
        Assert.Throws<ArgumentNullException>(() => new Worker(scopeFactory.Object, options, null!));
    }

    /// <summary>
    /// Verifies that the worker accepts valid scheduling options.
    /// </summary>
    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        Mock<IServiceScopeFactory> scopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        Mock<ILogger<Worker>> logger = new Mock<ILogger<Worker>>(MockBehavior.Loose);

        IOptions<CronOrIntervalOptions> options = Options.Create(new CronOrIntervalOptions
                                                                 {
                                                                     UsersDetailsIncrementalIntervalMinutes = 30,
                                                                     UsersDetailsRecoveryIntervalHours = 3
                                                                 });

        Worker sut = new Worker(scopeFactory.Object, options, logger.Object);

        Assert.NotNull(sut);
    }
}
