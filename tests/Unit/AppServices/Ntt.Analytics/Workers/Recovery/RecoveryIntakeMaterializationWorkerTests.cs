using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Enums;
using Application.Features.Recovery;
using Application.Mediator;

using Moq;

using Ntt.Analytics.Workers.Recovery;

using SharedKernel.Lobs;

using tests.TestSupport.Logging;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics.Workers.Recovery;

public sealed class RecoveryIntakeMaterializationWorkerTests
{
    /// <summary>
    /// Verifies that one materialization cycle populates NTT context and dispatches work.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_DispatchesMaterializationCommand()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);

        accessor.SetupProperty(x => x.LobName);

        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        mediator.Setup(x => x.Send(It.Is<MaterializeRecoveryIntakeCommand>(command =>
                                                                                   command.Category
                                                                                   == SyncAnalyticsCategory
                                                                                          .UsersDetails),
                                   It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

        RecoveryIntakeMaterializationWorker sut =
                new RecoveryIntakeMaterializationWorker(mediator.Object,
                                                        accessor.Object,
                                                        credentialProvider.Object,
                                                        new TestLogger<RecoveryIntakeMaterializationWorker>());

        await sut.RunOnceAsync(SyncAnalyticsCategory.UsersDetails, CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, It.IsAny<CancellationToken>()), Times.Once);
        mediator.VerifyAll();
    }

    /// <summary>
    /// Verifies that an empty materialization cycle still prepares NTT context and exits cleanly.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenNoWork_DoesNotThrow()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);

        accessor.SetupProperty(x => x.LobName);

        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        mediator.Setup(x => x.Send(It.Is<MaterializeRecoveryIntakeCommand>(command => command.Category == null),
                                   It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

        RecoveryIntakeMaterializationWorker sut =
                new RecoveryIntakeMaterializationWorker(mediator.Object,
                                                        accessor.Object,
                                                        credentialProvider.Object,
                                                        new TestLogger<RecoveryIntakeMaterializationWorker>());

        await sut.RunOnceAsync(null, CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, It.IsAny<CancellationToken>()), Times.Once);
        mediator.VerifyAll();
    }
}
