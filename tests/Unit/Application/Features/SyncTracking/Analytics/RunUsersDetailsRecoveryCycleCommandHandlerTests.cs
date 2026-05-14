using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;
using Application.Mediator;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.Analytics;

public sealed class RunUsersDetailsRecoveryCycleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoRecoveryRequest_ReturnsNull()
    {
        CancellationToken ct = CancellationToken.None;

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);

        syncRequestRepository
               .Setup(x => x.TryStartNextRecoveryRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
               .ReturnsAsync((SyncRequestDto?)null);

        RunUsersDetailsRecoveryCycleCommandHandler sut =
                new RunUsersDetailsRecoveryCycleCommandHandler(syncRequestRepository.Object, mediator.Object);

        long? result = await sut.Handle(new RunUsersDetailsRecoveryCycleCommand(), ct);

        Assert.Null(result);

        mediator.Verify(x => x.Send(It.IsAny<RunAnalyticsRecoverySyncCommand>(), It.IsAny<CancellationToken>()),
                        Times.Never);
        syncRequestRepository.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenRecoveryRequestsExist_DispatchesUntilQueueEmpty()
    {
        CancellationToken ct = CancellationToken.None;
        const string firstInterval = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        const string secondInterval = "2026-04-14T00:30Z/2026-04-14T01:00Z";

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);

        syncRequestRepository
               .SetupSequence(x => x.TryStartNextRecoveryRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
               .ReturnsAsync(BuildRequest(101L,
                                          firstInterval,
                                          1,
                                          null))
               .ReturnsAsync(BuildRequest(102L,
                                          secondInterval,
                                          2,
                                          null))
               .ReturnsAsync((SyncRequestDto?)null);

        mediator.Setup(x => x.Send(It.Is<RunAnalyticsRecoverySyncCommand>(command => command.RequestId == 101L
                                                                              && command.Category
                                                                              == SyncAnalyticsCategory.UsersDetails
                                                                              && command.Interval     == firstInterval
                                                                              && command.PageNumber   == 1
                                                                              && command.GenesysJobId == null),
                                   ct))
                .ReturnsAsync(101L);

        mediator.Setup(x => x.Send(It.Is<RunAnalyticsRecoverySyncCommand>(command => command.RequestId == 102L
                                                                              && command.Category
                                                                              == SyncAnalyticsCategory.UsersDetails
                                                                              && command.Interval     == secondInterval
                                                                              && command.PageNumber   == 2
                                                                              && command.GenesysJobId == null),
                                   ct))
                .ReturnsAsync(102L);

        RunUsersDetailsRecoveryCycleCommandHandler sut =
                new RunUsersDetailsRecoveryCycleCommandHandler(syncRequestRepository.Object, mediator.Object);

        long? result = await sut.Handle(new RunUsersDetailsRecoveryCycleCommand(), ct);

        Assert.Equal(102L, result);

        syncRequestRepository.VerifyAll();
        mediator.VerifyAll();
    }

    [Fact]
    public async Task Handle_SuppressesGenesysJobIdForUsersDetailsRecovery()
    {
        CancellationToken ct = CancellationToken.None;
        const string interval = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        const string genesysJobId = "JOB-123";

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);

        syncRequestRepository
               .SetupSequence(x => x.TryStartNextRecoveryRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
               .ReturnsAsync(BuildRequest(201L,
                                          interval,
                                          3,
                                          genesysJobId))
               .ReturnsAsync((SyncRequestDto?)null);

        mediator.Setup(x => x.Send(It.Is<RunAnalyticsRecoverySyncCommand>(command => command.RequestId == 201L
                                                                              && command.Category
                                                                              == SyncAnalyticsCategory.UsersDetails
                                                                              && command.Interval     == interval
                                                                              && command.PageNumber   == 3
                                                                              && command.GenesysJobId == null),
                                   ct))
                .ReturnsAsync(201L);

        RunUsersDetailsRecoveryCycleCommandHandler sut =
                new RunUsersDetailsRecoveryCycleCommandHandler(syncRequestRepository.Object, mediator.Object);

        long? result = await sut.Handle(new RunUsersDetailsRecoveryCycleCommand(), ct);

        Assert.Equal(201L, result);

        syncRequestRepository.VerifyAll();
        mediator.VerifyAll();
    }

    #region ========== *** Private Section *** ==========

    private static SyncRequestDto BuildRequest(long id, string interval, int? pageNumber, string? genesysJobId)
    {
        return new SyncRequestDto
               {
                   Id = id,
                   Category = nameof(SyncAnalyticsCategory.UsersDetails),
                   Mode = SyncMode.Recovery,
                   Status = SyncRequestStatus.Running,
                   Interval = interval,
                   PageNumber = pageNumber,
                   GenesysJobId = genesysJobId,
                   ScopeKey =
                           $"{nameof(SyncAnalyticsCategory.UsersDetails)}|Recovery|{interval}|{pageNumber}|{genesysJobId ?? "-"}"
               };
    }

    #endregion
}
