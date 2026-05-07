using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using Microsoft.Extensions.Logging;

using Moq;

using Ntt.Analytics.Workers.UsersDetails;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Unit tests for <see cref="UsersDetailsIncrementalWorker"/>.
/// </summary>
public sealed class UsersDetailsIncrementalWorkerTests
{
    /// <summary>
    /// Verifies that no command is dispatched when the repository cannot reserve a new interval.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenNoReservationExists_DoesNotPopulateCredentialsOrDispatch()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);

        Mock<IIncrementalSyncWindowRepository> repository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                       SyncAnalyticsCategory.UsersDetails,
                                                       It.IsAny<DateTimeOffset>(),
                                                       CancellationToken.None))
                  .ReturnsAsync(new IncrementalSyncWindowReservation(false,
                                                                     null,
                                                                     null,
                                                                     null));

        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        dateTimeProvider.SetupGet(x => x.EstNowOffset)
                        .Returns(new DateTimeOffset(2026,
                                                    5,
                                                    4,
                                                    10,
                                                    17,
                                                    12,
                                                    TimeSpan.FromHours(-4)));

        Mock<ILogger<UsersDetailsIncrementalWorker>> logger =
                new Mock<ILogger<UsersDetailsIncrementalWorker>>(MockBehavior.Loose);

        UsersDetailsIncrementalWorker sut = new UsersDetailsIncrementalWorker(mediator.Object,
                                                                              accessor.Object,
                                                                              credentialProvider.Object,
                                                                              repository.Object,
                                                                              dateTimeProvider.Object,
                                                                              logger.Object);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Null(accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(It.IsAny<ILobContextAccessor>(), It.IsAny<CancellationToken>()),
                                  Times.Never);

        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), It.IsAny<CancellationToken>()),
                        Times.Never);
    }

    /// <summary>
    /// Verifies that one reserved interval populates credentials and dispatches one incremental command.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenReservationExists_PopulatesCredentialsAndDispatchesIncrementalCommand()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        RunAnalyticsIncrementalSyncCommand? dispatchedCommand = null;

        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), CancellationToken.None))
                .Callback<IRequest<long>, CancellationToken>((request, _) =>
                                                             {
                                                                 dispatchedCommand =
                                                                         (RunAnalyticsIncrementalSyncCommand)request;
                                                             })
                .ReturnsAsync(123L);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        Mock<IIncrementalSyncWindowRepository> repository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                       SyncAnalyticsCategory.UsersDetails,
                                                       It.IsAny<DateTimeOffset>(),
                                                       CancellationToken.None))
                  .ReturnsAsync(new IncrementalSyncWindowReservation(true,
                                                                     "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         13,
                                                                         30,
                                                                         0,
                                                                         TimeSpan.Zero),
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         14,
                                                                         0,
                                                                         0,
                                                                         TimeSpan.Zero)));

        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        dateTimeProvider.SetupGet(x => x.EstNowOffset)
                        .Returns(new DateTimeOffset(2026,
                                                    5,
                                                    4,
                                                    10,
                                                    17,
                                                    12,
                                                    TimeSpan.FromHours(-4)));

        Mock<ILogger<UsersDetailsIncrementalWorker>> logger =
                new Mock<ILogger<UsersDetailsIncrementalWorker>>(MockBehavior.Loose);

        UsersDetailsIncrementalWorker sut = new UsersDetailsIncrementalWorker(mediator.Object,
                                                                              accessor.Object,
                                                                              credentialProvider.Object,
                                                                              repository.Object,
                                                                              dateTimeProvider.Object,
                                                                              logger.Object);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);
        Assert.NotNull(dispatchedCommand);
        Assert.Equal(SyncAnalyticsCategory.UsersDetails, dispatchedCommand!.Category);
        Assert.Equal("2026-05-04T13:30Z/2026-05-04T14:00Z", dispatchedCommand.Interval);
        Assert.Null(dispatchedCommand.PageNumber);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), CancellationToken.None),
                        Times.Once);
    }

    /// <summary>
    /// Verifies that dispatch failures are not swallowed by the incremental worker.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenDispatchFails_RethrowsOriginalException()
    {
        InvalidOperationException expected = new InvalidOperationException("incremental dispatch failed");

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), CancellationToken.None))
                .ThrowsAsync(expected);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        Mock<IIncrementalSyncWindowRepository> repository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                       SyncAnalyticsCategory.UsersDetails,
                                                       It.IsAny<DateTimeOffset>(),
                                                       CancellationToken.None))
                  .ReturnsAsync(new IncrementalSyncWindowReservation(true,
                                                                     "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         13,
                                                                         30,
                                                                         0,
                                                                         TimeSpan.Zero),
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         14,
                                                                         0,
                                                                         0,
                                                                         TimeSpan.Zero)));

        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        dateTimeProvider.SetupGet(x => x.EstNowOffset)
                        .Returns(new DateTimeOffset(2026,
                                                    5,
                                                    4,
                                                    10,
                                                    17,
                                                    12,
                                                    TimeSpan.FromHours(-4)));

        Mock<ILogger<UsersDetailsIncrementalWorker>> logger =
                new Mock<ILogger<UsersDetailsIncrementalWorker>>(MockBehavior.Loose);

        UsersDetailsIncrementalWorker sut = new UsersDetailsIncrementalWorker(mediator.Object,
                                                                              accessor.Object,
                                                                              credentialProvider.Object,
                                                                              repository.Object,
                                                                              dateTimeProvider.Object,
                                                                              logger.Object);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.RunOnceAsync(CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), CancellationToken.None),
                        Times.Once);
    }

    /// <summary>
    /// Verifies that host cancellation is rethrown by the incremental worker.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenHostCancellationOccurs_RethrowsOperationCanceledException()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();
        CancellationToken ct = cts.Token;

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), ct))
                .ThrowsAsync(new OperationCanceledException("host canceled", ct));

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, ct))
                          .Returns(Task.CompletedTask);

        Mock<IIncrementalSyncWindowRepository> repository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        repository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                       SyncAnalyticsCategory.UsersDetails,
                                                       It.IsAny<DateTimeOffset>(),
                                                       ct))
                  .ReturnsAsync(new IncrementalSyncWindowReservation(true,
                                                                     "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         13,
                                                                         30,
                                                                         0,
                                                                         TimeSpan.Zero),
                                                                     new DateTimeOffset(2026,
                                                                         5,
                                                                         4,
                                                                         14,
                                                                         0,
                                                                         0,
                                                                         TimeSpan.Zero)));

        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        dateTimeProvider.SetupGet(x => x.EstNowOffset)
                        .Returns(new DateTimeOffset(2026,
                                                    5,
                                                    4,
                                                    10,
                                                    17,
                                                    12,
                                                    TimeSpan.FromHours(-4)));

        Mock<ILogger<UsersDetailsIncrementalWorker>> logger =
                new Mock<ILogger<UsersDetailsIncrementalWorker>>(MockBehavior.Loose);

        UsersDetailsIncrementalWorker sut = new UsersDetailsIncrementalWorker(mediator.Object,
                                                                              accessor.Object,
                                                                              credentialProvider.Object,
                                                                              repository.Object,
                                                                              dateTimeProvider.Object,
                                                                              logger.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.RunOnceAsync(ct));

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);
        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, ct), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsIncrementalSyncCommand>(), ct), Times.Once);
    }
}
