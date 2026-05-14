using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Features.Analytics.UsersDetails;
using Application.Mediator;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using Ntt.Analytics.Workers.UsersDetails;

using SharedKernel.Lobs;

using Xunit;


namespace tests.Unit.Hosts.AppServices.Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Unit tests for <see cref="UsersDetailsIncrementalWorker"/>.
/// </summary>
public sealed class UsersDetailsIncrementalWorkerTests
{
    /// <summary>
    /// Verifies that the worker populates NTT context and dispatches one incremental cycle command.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenCycleReturnsRequestId_PopulatesContextAndDispatchesCycleCommand()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command => command.Lob == LobName.Ntt),
                                   CancellationToken.None))
                .ReturnsAsync(123L);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        UsersDetailsIncrementalWorker sut =
                new UsersDetailsIncrementalWorker(mediator.Object,
                                                  accessor.Object,
                                                  credentialProvider.Object,
                                                  NullLogger<UsersDetailsIncrementalWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x =>
                                x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command =>
                                               command.Lob == LobName.Ntt),
                                       CancellationToken.None),
                        Times.Once);
    }

    /// <summary>
    /// Verifies that the worker completes successfully when the incremental cycle finds no work.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenCycleReturnsNoRequestId_CompletesWithoutError()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command => command.Lob == LobName.Ntt),
                                   CancellationToken.None))
                .ReturnsAsync((long?)null);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        UsersDetailsIncrementalWorker sut =
                new UsersDetailsIncrementalWorker(mediator.Object,
                                                  accessor.Object,
                                                  credentialProvider.Object,
                                                  NullLogger<UsersDetailsIncrementalWorker>.Instance);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x =>
                                x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command =>
                                               command.Lob == LobName.Ntt),
                                       CancellationToken.None),
                        Times.Once);
    }

    /// <summary>
    /// Verifies that mediator failures are not swallowed by the worker.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenCycleDispatchFails_RethrowsOriginalException()
    {
        InvalidOperationException expected = new InvalidOperationException("incremental cycle failed");

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command => command.Lob == LobName.Ntt),
                                   CancellationToken.None))
                .ThrowsAsync(expected);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        UsersDetailsIncrementalWorker sut =
                new UsersDetailsIncrementalWorker(mediator.Object,
                                                  accessor.Object,
                                                  credentialProvider.Object,
                                                  NullLogger<UsersDetailsIncrementalWorker>.Instance);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunOnceAsync(CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x =>
                                x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command =>
                                               command.Lob == LobName.Ntt),
                                       CancellationToken.None),
                        Times.Once);
    }

    /// <summary>
    /// Verifies that host cancellation from the mediator is propagated by the worker.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenHostCancellationOccurs_RethrowsOperationCanceledException()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();
        CancellationToken ct = cts.Token;

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command => command.Lob == LobName.Ntt),
                                   ct))
                .ThrowsAsync(new OperationCanceledException("host canceled", ct));

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, ct))
                          .Returns(Task.CompletedTask);

        UsersDetailsIncrementalWorker sut =
                new UsersDetailsIncrementalWorker(mediator.Object,
                                                  accessor.Object,
                                                  credentialProvider.Object,
                                                  NullLogger<UsersDetailsIncrementalWorker>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.RunOnceAsync(ct));

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, ct), Times.Once);
        mediator.Verify(x =>
                                x.Send(It.Is<RunUsersDetailsIncrementalCycleCommand>(command =>
                                               command.Lob == LobName.Ntt),
                                       ct),
                        Times.Once);
    }
}
