using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Analytics.UsersDetails;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Analytics.UsersDetails;

public sealed class RunUsersDetailsIncrementalCycleCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenJoinableWorkExists_DrainsBeforeReservingWindow()
    {
        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIncrementalSyncWindowRepository> incrementalSyncWindowRepository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRequestRepository
               .SetupSequence(x => x.GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            ct))
               .ReturnsAsync(BuildRequest(101L))
               .ReturnsAsync(BuildRequest(102L))
               .ReturnsAsync((SyncRequestDto?)null)
               .ReturnsAsync((SyncRequestDto?)null);

        syncRequestRunner.Setup(x => x.ExecuteJoinableAsync(101L, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        syncRequestRunner.Setup(x => x.ExecuteJoinableAsync(102L, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        incrementalSyncWindowRepository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                                            SyncAnalyticsCategory.UsersDetails,
                                                                            now,
                                                                            ct))
                                       .ReturnsAsync(new IncrementalSyncWindowReservation(false,
                                                         null,
                                                         null,
                                                         null));

        RunUsersDetailsIncrementalCycleCommandHandler sut = BuildSut(syncRequestRepository,
                                                                     incrementalSyncWindowRepository,
                                                                     dateTimeProvider,
                                                                     syncRequestRunner,
                                                                     intervalPlanner);

        long? result = await sut.Handle(new RunUsersDetailsIncrementalCycleCommand(LobName.Ntt), ct);

        Assert.Equal(102L, result);

        syncRequestRepository.VerifyAll();
        syncRequestRunner.VerifyAll();
        incrementalSyncWindowRepository.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenNoJoinableWorkAndNoReservation_ReturnsNull()
    {
        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIncrementalSyncWindowRepository> incrementalSyncWindowRepository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRequestRepository
               .Setup(x => x.GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
               .ReturnsAsync((SyncRequestDto?)null);

        incrementalSyncWindowRepository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                                            SyncAnalyticsCategory.UsersDetails,
                                                                            now,
                                                                            ct))
                                       .ReturnsAsync(new IncrementalSyncWindowReservation(false,
                                                         null,
                                                         null,
                                                         null));

        RunUsersDetailsIncrementalCycleCommandHandler sut = BuildSut(syncRequestRepository,
                                                                     incrementalSyncWindowRepository,
                                                                     dateTimeProvider,
                                                                     syncRequestRunner,
                                                                     intervalPlanner);

        long? result = await sut.Handle(new RunUsersDetailsIncrementalCycleCommand(LobName.Ntt), ct);

        Assert.Null(result);

        syncRequestRepository.VerifyAll();
        incrementalSyncWindowRepository.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenWindowReserved_PlansCreatesAndExecutesScopes()
    {
        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));
        const string reservedIntervalText = "2026-04-14T00:00Z/2026-04-14T01:00Z";
        const string firstPlannedIntervalText = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        const string secondPlannedIntervalText = "2026-04-14T00:30Z/2026-04-14T01:00Z";

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIncrementalSyncWindowRepository> incrementalSyncWindowRepository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRequestRepository
               .SetupSequence(x => x.GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                            ct))
               .ReturnsAsync((SyncRequestDto?)null)
               .ReturnsAsync((SyncRequestDto?)null);

        incrementalSyncWindowRepository
               .SetupSequence(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                            SyncAnalyticsCategory.UsersDetails,
                                                            now,
                                                            ct))
               .ReturnsAsync(new IncrementalSyncWindowReservation(true,
                                                                  reservedIntervalText,
                                                                  new DateTimeOffset(2026,
                                                                      4,
                                                                      14,
                                                                      0,
                                                                      0,
                                                                      0,
                                                                      TimeSpan.Zero),
                                                                  new DateTimeOffset(2026,
                                                                      4,
                                                                      14,
                                                                      1,
                                                                      0,
                                                                      0,
                                                                      TimeSpan.Zero)))
               .ReturnsAsync(new IncrementalSyncWindowReservation(false,
                                                                  null,
                                                                  null,
                                                                  null));

        intervalPlanner.Setup(x => x.PlanAsync(SyncAnalyticsCategory.UsersDetails,
                                               UtcInterval.Parse(reservedIntervalText),
                                               ct))
                       .ReturnsAsync([
                                         BuildPlannedInterval("2026-04-14T00:00Z", "2026-04-14T00:30Z"),
                                         BuildPlannedInterval("2026-04-14T00:30Z", "2026-04-14T01:00Z")
                                     ]);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   firstPlannedIntervalText,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(new SyncRequestResolveResult { Id = 201L, PublicId = Guid.NewGuid() });

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   secondPlannedIntervalText,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(new SyncRequestResolveResult { Id = 202L, PublicId = Guid.NewGuid() });

        syncRequestRunner.Setup(x => x.ExecuteJoinableAsync(201L, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        syncRequestRunner.Setup(x => x.ExecuteJoinableAsync(202L, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunUsersDetailsIncrementalCycleCommandHandler sut = BuildSut(syncRequestRepository,
                                                                     incrementalSyncWindowRepository,
                                                                     dateTimeProvider,
                                                                     syncRequestRunner,
                                                                     intervalPlanner);

        long? result = await sut.Handle(new RunUsersDetailsIncrementalCycleCommand(LobName.Ntt), ct);

        Assert.Equal(202L, result);

        syncRequestRepository.VerifyAll();
        incrementalSyncWindowRepository.VerifyAll();
        intervalPlanner.VerifyAll();
        syncRequestRunner.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenPlannedScopeExecutionFails_CreatesRecoveryRequestAndRethrows()
    {
        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));
        const string reservedIntervalText = "2026-04-14T00:00Z/2026-04-14T00:30Z";

        InvalidOperationException expected = new InvalidOperationException("execution failed");

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIncrementalSyncWindowRepository> incrementalSyncWindowRepository =
                new Mock<IIncrementalSyncWindowRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRequestRepository
               .Setup(x => x.GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct))
               .ReturnsAsync((SyncRequestDto?)null);

        incrementalSyncWindowRepository.Setup(x => x.ReserveNextWindowAsync(LobName.Ntt,
                                                                            SyncAnalyticsCategory.UsersDetails,
                                                                            now,
                                                                            ct))
                                       .ReturnsAsync(new IncrementalSyncWindowReservation(true,
                                                         reservedIntervalText,
                                                         new DateTimeOffset(2026,
                                                                            4,
                                                                            14,
                                                                            0,
                                                                            0,
                                                                            0,
                                                                            TimeSpan.Zero),
                                                         new DateTimeOffset(2026,
                                                                            4,
                                                                            14,
                                                                            0,
                                                                            30,
                                                                            0,
                                                                            TimeSpan.Zero)));

        intervalPlanner.Setup(x => x.PlanAsync(SyncAnalyticsCategory.UsersDetails,
                                               UtcInterval.Parse(reservedIntervalText),
                                               ct))
                       .ReturnsAsync([BuildPlannedInterval("2026-04-14T00:00Z", "2026-04-14T00:30Z")]);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Incremental,
                                                                   reservedIntervalText,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(new SyncRequestResolveResult { Id = 301L, PublicId = Guid.NewGuid() });

        syncRequestRunner.Setup(x => x.ExecuteJoinableAsync(301L, ct))
                         .ThrowsAsync(expected);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   reservedIntervalText,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(new SyncRequestResolveResult { Id = 401L, PublicId = Guid.NewGuid() });

        RunUsersDetailsIncrementalCycleCommandHandler sut = BuildSut(syncRequestRepository,
                                                                     incrementalSyncWindowRepository,
                                                                     dateTimeProvider,
                                                                     syncRequestRunner,
                                                                     intervalPlanner);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                                                                            sut.Handle(new
                                                                                        RunUsersDetailsIncrementalCycleCommand(LobName
                                                                                               .Ntt),
                                                                                ct));

        Assert.Same(expected, actual);

        syncRequestRepository.VerifyAll();
        incrementalSyncWindowRepository.VerifyAll();
        intervalPlanner.VerifyAll();
        syncRequestRunner.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    #region ========== *** Private Section *** ==========

    private static RunUsersDetailsIncrementalCycleCommandHandler BuildSut(
            Mock<ISyncRequestRepository> syncRequestRepository,
            Mock<IIncrementalSyncWindowRepository> incrementalSyncWindowRepository,
            Mock<IDateTimeProvider> dateTimeProvider,
            Mock<ISyncRequestRunner> syncRequestRunner,
            Mock<IIntervalPlanner> intervalPlanner)
    {
        return new RunUsersDetailsIncrementalCycleCommandHandler(syncRequestRepository.Object,
                                                                 incrementalSyncWindowRepository.Object,
                                                                 dateTimeProvider.Object,
                                                                 syncRequestRunner.Object,
                                                                 intervalPlanner.Object);
    }

    private static SyncRequestDto BuildRequest(long id)
    {
        return new SyncRequestDto
               {
                   Id = id,
                   Category = nameof(SyncAnalyticsCategory.UsersDetails),
                   Mode = SyncMode.Incremental,
                   Status = SyncRequestStatus.Pending,
                   ScopeKey = $"UsersDetails|Incremental|{id}|-|-"
               };
    }

    private static PlannedIntervalDto BuildPlannedInterval(string start, string end)
    {
        return new PlannedIntervalDto(UtcInterval.Parse($"{start}/{end}"), 1);
    }

    #endregion
}
