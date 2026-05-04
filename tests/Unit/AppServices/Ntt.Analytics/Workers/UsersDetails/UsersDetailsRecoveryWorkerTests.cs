using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using Microsoft.Extensions.Logging;

using Moq;

using Ntt.Analytics.Workers.UsersDetails;

using SharedKernel.Lobs;

using Xunit;


namespace tests.Unit.AppServices.Ntt.Analytics.Workers.UsersDetails;

/// <summary>
/// Unit tests for <see cref="UsersDetailsRecoveryWorker"/>.
/// </summary>
public sealed class UsersDetailsRecoveryWorkerTests
{
    /// <summary>
    /// Verifies that no credentials or recovery commands are dispatched when no eligible requests exist.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenNoEligibleRequestsExist_DoesNotPopulateCredentialsOrDispatch()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                 CancellationToken.None))
                  .ReturnsAsync(Array.Empty<SyncRequestDto>());

        Mock<ILogger<UsersDetailsRecoveryWorker>> logger =
                new Mock<ILogger<UsersDetailsRecoveryWorker>>(MockBehavior.Loose);

        UsersDetailsRecoveryWorker sut = new UsersDetailsRecoveryWorker(mediator.Object,
                                                                        accessor.Object,
                                                                        credentialProvider.Object,
                                                                        repository.Object,
                                                                        logger.Object);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Null(accessor.Object.LobName);

        credentialProvider.Verify(x => x.PopulateAsync(It.IsAny<ILobContextAccessor>(), It.IsAny<CancellationToken>()),
                                  Times.Never);

        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), It.IsAny<CancellationToken>()),
                        Times.Never);
    }

    /// <summary>
    /// Verifies that eligible recovery requests populate credentials and dispatch one recovery command per request,
    /// always forcing <c>GenesysJobId</c> to <c>null</c> for UsersDetails.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenEligibleRequestsExist_PopulatesCredentialsAndDispatchesRecoveryCommands()
    {
        List<RunAnalyticsRecoverySyncCommand> dispatchedCommands = [];

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), CancellationToken.None))
                .Callback<IRequest<long>, CancellationToken>((request, _) =>
                                                             {
                                                                 dispatchedCommands
                                                                        .Add((RunAnalyticsRecoverySyncCommand)request);
                                                             })
                .ReturnsAsync(123L);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        SyncRequestDto pending = new SyncRequestDto
                                 {
                                     Id = 1,
                                     PublicId = Guid.NewGuid(),
                                     Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                     Mode = SyncMode.Recovery,
                                     Status = SyncRequestStatus.Pending,
                                     ReopenCount = 0,
                                     Interval = "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                     PageNumber = 1,
                                     GenesysJobId = "SHOULD-NOT-FORWARD",
                                     ScopeKey = "scope-1"
                                 };

        SyncRequestDto failedRetryable = new SyncRequestDto
                                         {
                                             Id = 2,
                                             PublicId = Guid.NewGuid(),
                                             Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                             Mode = SyncMode.Recovery,
                                             Status = SyncRequestStatus.Failed,
                                             ReopenCount = 2,
                                             Interval = "2026-05-04T14:00Z/2026-05-04T14:30Z",
                                             PageNumber = 2,
                                             GenesysJobId = "ALSO-NOT-FORWARDED",
                                             ScopeKey = "scope-2"
                                         };

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                 CancellationToken.None))
                  .ReturnsAsync([pending, failedRetryable]);

        Mock<ILogger<UsersDetailsRecoveryWorker>> logger =
                new Mock<ILogger<UsersDetailsRecoveryWorker>>(MockBehavior.Loose);

        UsersDetailsRecoveryWorker sut = new UsersDetailsRecoveryWorker(mediator.Object,
                                                                        accessor.Object,
                                                                        credentialProvider.Object,
                                                                        repository.Object,
                                                                        logger.Object);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);
        Assert.Equal(2, dispatchedCommands.Count);

        Assert.Equal([SyncAnalyticsCategory.UsersDetails, SyncAnalyticsCategory.UsersDetails],
                     dispatchedCommands.Select(x => x.Category)
                                       .ToArray());
        Assert.Equal([null, null],
                     dispatchedCommands.Select(x => x.GenesysJobId)
                                       .ToArray());

        Assert.Equal(pending.Interval, dispatchedCommands[0].Interval);
        Assert.Equal(pending.PageNumber, dispatchedCommands[0].PageNumber);

        Assert.Equal(failedRetryable.Interval, dispatchedCommands[1].Interval);
        Assert.Equal(failedRetryable.PageNumber, dispatchedCommands[1].PageNumber);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), CancellationToken.None),
                        Times.Exactly(2));
    }

    /// <summary>
    /// Verifies that one failed recovery request does not stop the remaining batch.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenOneRecoveryRequestFails_ContinuesWithRemainingRequests()
    {
        List<RunAnalyticsRecoverySyncCommand> dispatchedCommands = [];

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), CancellationToken.None))
                .Returns<IRequest<long>, CancellationToken>((request, _) =>
                                                            {
                                                                RunAnalyticsRecoverySyncCommand command =
                                                                        (RunAnalyticsRecoverySyncCommand)request;
                                                                dispatchedCommands.Add(command);

                                                                if (command.PageNumber == 2)
                                                                {
                                                                    throw new
                                                                            InvalidOperationException("recovery request failed");
                                                                }

                                                                return Task.FromResult(123L);
                                                            });

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, CancellationToken.None))
                          .Returns(Task.CompletedTask);

        SyncRequestDto first = new SyncRequestDto
                               {
                                   Id = 1,
                                   PublicId = Guid.NewGuid(),
                                   Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                   Mode = SyncMode.Recovery,
                                   Status = SyncRequestStatus.Pending,
                                   Interval = "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                   PageNumber = 1,
                                   ScopeKey = "scope-1"
                               };

        SyncRequestDto failing = new SyncRequestDto
                                 {
                                     Id = 2,
                                     PublicId = Guid.NewGuid(),
                                     Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                     Mode = SyncMode.Recovery,
                                     Status = SyncRequestStatus.Failed,
                                     Interval = "2026-05-04T14:00Z/2026-05-04T14:30Z",
                                     PageNumber = 2,
                                     ScopeKey = "scope-2"
                                 };

        SyncRequestDto third = new SyncRequestDto
                               {
                                   Id = 3,
                                   PublicId = Guid.NewGuid(),
                                   Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                   Mode = SyncMode.Recovery,
                                   Status = SyncRequestStatus.Canceled,
                                   Interval = "2026-05-04T14:30Z/2026-05-04T15:00Z",
                                   PageNumber = 3,
                                   ScopeKey = "scope-3"
                               };

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                 CancellationToken.None))
                  .ReturnsAsync([first, failing, third]);

        Mock<ILogger<UsersDetailsRecoveryWorker>> logger =
                new Mock<ILogger<UsersDetailsRecoveryWorker>>(MockBehavior.Loose);

        UsersDetailsRecoveryWorker sut = new UsersDetailsRecoveryWorker(mediator.Object,
                                                                        accessor.Object,
                                                                        credentialProvider.Object,
                                                                        repository.Object,
                                                                        logger.Object);

        await sut.RunOnceAsync(CancellationToken.None);

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);
        Assert.Equal(3, dispatchedCommands.Count);
        Assert.Equal([1, 2, 3],
                     dispatchedCommands.Select(x => x.PageNumber)
                                       .ToArray());

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, CancellationToken.None), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), CancellationToken.None),
                        Times.Exactly(3));
    }

    /// <summary>
    /// Verifies that host cancellation is rethrown immediately and does not continue the remaining batch.
    /// </summary>
    [Fact]
    public async Task RunOnceAsync_WhenHostCancellationOccurs_RethrowsAndStopsRemainingBatch()
    {
        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();
        CancellationToken ct = cts.Token;

        List<RunAnalyticsRecoverySyncCommand> dispatchedCommands = [];

        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), ct))
                .Returns<IRequest<long>, CancellationToken>((request, _) =>
                                                            {
                                                                dispatchedCommands.Add((RunAnalyticsRecoverySyncCommand)
                                                                    request);

                                                                throw new OperationCanceledException("host canceled",
                                                                    ct);
                                                            });

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(accessor.Object, ct))
                          .Returns(Task.CompletedTask);

        SyncRequestDto first = new SyncRequestDto
                               {
                                   Id = 1,
                                   PublicId = Guid.NewGuid(),
                                   Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                   Mode = SyncMode.Recovery,
                                   Status = SyncRequestStatus.Pending,
                                   Interval = "2026-05-04T13:30Z/2026-05-04T14:00Z",
                                   PageNumber = 1,
                                   ScopeKey = "scope-1"
                               };

        SyncRequestDto second = new SyncRequestDto
                                {
                                    Id = 2,
                                    PublicId = Guid.NewGuid(),
                                    Category = nameof(SyncAnalyticsCategory.UsersDetails),
                                    Mode = SyncMode.Recovery,
                                    Status = SyncRequestStatus.Failed,
                                    Interval = "2026-05-04T14:00Z/2026-05-04T14:30Z",
                                    PageNumber = 2,
                                    ScopeKey = "scope-2"
                                };

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.GetEligibleRecoveryRequestsAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
                  .ReturnsAsync([first, second]);

        Mock<ILogger<UsersDetailsRecoveryWorker>> logger =
                new Mock<ILogger<UsersDetailsRecoveryWorker>>(MockBehavior.Loose);

        UsersDetailsRecoveryWorker sut = new UsersDetailsRecoveryWorker(mediator.Object,
                                                                        accessor.Object,
                                                                        credentialProvider.Object,
                                                                        repository.Object,
                                                                        logger.Object);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.RunOnceAsync(ct));

        Assert.Equal(LobName.Ntt.Value, accessor.Object.LobName);
        Assert.Single(dispatchedCommands);
        Assert.Equal(1, dispatchedCommands[0].PageNumber);

        credentialProvider.Verify(x => x.PopulateAsync(accessor.Object, ct), Times.Once);
        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), ct), Times.Once);
    }
}
