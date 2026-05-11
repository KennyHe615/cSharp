using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Features.Recovery;
using Application.Mediator;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Ntt.Analytics.Scheduling;
using Ntt.Analytics.Workers.Recovery;

using tests.TestSupport.Logging;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics.Workers.Recovery;

/// <summary>
/// Unit tests for <see cref="RecoveryIntakeMaterializationScheduledWorkerLoop"/>.
/// </summary>
public sealed class RecoveryIntakeMaterializationScheduledWorkerLoopTests
{
    /// <summary>
    /// Verifies that the materialization loop exits without resolving scoped work when disabled.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenDisabled_DoesNotResolveWorker()
    {
        IServiceScopeFactory scopeFactory = new ServiceCollection().BuildServiceProvider()
                                                                   .GetRequiredService<IServiceScopeFactory>();

        RecoveryIntakeMaterializationScheduledWorkerLoop sut =
                new RecoveryIntakeMaterializationScheduledWorkerLoop(scopeFactory,
                                                                     Options.Create(new CronOrIntervalOptions
                                                                         {
                                                                             RecoveryIntakeMaterializationEnabled =
                                                                                     false
                                                                         }),
                                                                     new ScheduledWorkerLoopRunner(new TestLogger<
                                                                             ScheduledWorkerLoopRunner>()));

        await sut.RunAsync(CancellationToken.None);
    }

    /// <summary>
    /// Verifies that the materialization loop resolves and runs one worker cycle when enabled.
    /// </summary>
    [Fact]
    public async Task RunAsync_WhenEnabled_RunsMaterializationWorker()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        Action cancel = cts.Cancel;

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);

        accessor.SetupProperty(x => x.LobName);

        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        mediator.Setup(x => x.Send(It.Is<MaterializeRecoveryIntakeCommand>(command => command.Category == null),
                                   It.IsAny<CancellationToken>()))
                .Returns<IRequest<bool>, CancellationToken>((_, _) =>
                                                            {
                                                                cancel();

                                                                return Task.FromResult(false);
                                                            });

        await using ServiceProvider provider = new ServiceCollection().AddSingleton(mediator.Object)
                                                                      .AddSingleton(accessor.Object)
                                                                      .AddSingleton(credentialProvider.Object)
                                                                      .AddSingleton<
                                                                               ILogger<
                                                                                       RecoveryIntakeMaterializationWorker>>(new
                                                                               TestLogger<
                                                                                       RecoveryIntakeMaterializationWorker>())
                                                                      .AddScoped<RecoveryIntakeMaterializationWorker>()
                                                                      .BuildServiceProvider();

        RecoveryIntakeMaterializationScheduledWorkerLoop sut =
                new RecoveryIntakeMaterializationScheduledWorkerLoop(provider
                                                                            .GetRequiredService<IServiceScopeFactory>(),
                                                                     Options.Create(new CronOrIntervalOptions
                                                                         {
                                                                             RecoveryIntakeMaterializationEnabled =
                                                                                     true,
                                                                             RecoveryIntakeMaterializationIntervalMinutes =
                                                                                     10
                                                                         }),
                                                                     new ScheduledWorkerLoopRunner(new TestLogger<
                                                                             ScheduledWorkerLoopRunner>()));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => sut.RunAsync(cts.Token));

        mediator.VerifyAll();
    }
}
