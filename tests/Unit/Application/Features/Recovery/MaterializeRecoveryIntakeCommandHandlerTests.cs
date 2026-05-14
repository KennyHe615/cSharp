using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Recovery;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.DTOs.Recovery;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Recovery;

using Moq;

using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class MaterializeRecoveryIntakeCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenNoPendingIntake_ReturnsFalse()
    {
        Mock<IRecoveryIntakeWorkRepository> intakeRepository =
                new Mock<IRecoveryIntakeWorkRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        intakeRepository.Setup(x => x.TryStartNextPendingAsync(null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync((AnalyticsRecoveryRequestDto?)null);

        MaterializeRecoveryIntakeCommandHandler sut =
                new MaterializeRecoveryIntakeCommandHandler(intakeRepository.Object,
                                                            syncRequestRepository.Object,
                                                            intervalPlanner.Object);

        bool result = await sut.Handle(new MaterializeRecoveryIntakeCommand(null), CancellationToken.None);

        Assert.False(result);
        intakeRepository.VerifyAll();
        syncRequestRepository.VerifyNoOtherCalls();
        intervalPlanner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenIntervalRequest_PlansSlicesAndCreatesExecutableRecoveryRequests()
    {
        UtcInterval sourceInterval = new UtcInterval(new DateTimeOffset(2026,
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
                                                                        TimeSpan.Zero));

        UtcInterval firstSlice = new UtcInterval(new DateTimeOffset(2026,
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
                                                                    10,
                                                                    0,
                                                                    TimeSpan.Zero));

        UtcInterval secondSlice = new UtcInterval(new DateTimeOffset(2026,
                                                                     4,
                                                                     14,
                                                                     0,
                                                                     10,
                                                                     0,
                                                                     TimeSpan.Zero),
                                                  new DateTimeOffset(2026,
                                                                     4,
                                                                     14,
                                                                     0,
                                                                     30,
                                                                     0,
                                                                     TimeSpan.Zero));

        AnalyticsRecoveryRequestDto intake = new AnalyticsRecoveryRequestDto
                                             {
                                                 Id = 101L,
                                                 PublicId =
                                                         Guid
                                                                .Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                                                 Category =
                                                         nameof(SyncAnalyticsCategory
                                                                       .UsersDetails),
                                                 Status = AnalyticsRecoveryRequestStatus
                                                        .Running,
                                                 Interval = sourceInterval.ToString()
                                             };

        Mock<IRecoveryIntakeWorkRepository> intakeRepository =
                new Mock<IRecoveryIntakeWorkRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        intakeRepository.Setup(x => x.TryStartNextPendingAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                               It.IsAny<CancellationToken>()))
                        .ReturnsAsync(intake);

        intervalPlanner.Setup(x => x.PlanAsync(SyncAnalyticsCategory.UsersDetails,
                                               sourceInterval,
                                               It.IsAny<CancellationToken>()))
                       .ReturnsAsync([
                                         new PlannedIntervalDto(firstSlice, 10),
                                         new PlannedIntervalDto(secondSlice, 20)
                                     ]);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   firstSlice.ToString(),
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(BuildResolveResult(201L));

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   secondSlice.ToString(),
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(BuildResolveResult(202L));

        intakeRepository.Setup(x => x.TryMarkCompletedAsync(101L, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        MaterializeRecoveryIntakeCommandHandler sut =
                new MaterializeRecoveryIntakeCommandHandler(intakeRepository.Object,
                                                            syncRequestRepository.Object,
                                                            intervalPlanner.Object);

        bool result =
                await sut.Handle(new MaterializeRecoveryIntakeCommand(SyncAnalyticsCategory.UsersDetails),
                                 CancellationToken.None);

        Assert.True(result);
        intakeRepository.VerifyAll();
        syncRequestRepository.VerifyAll();
        intervalPlanner.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenGenesysJobIdRequest_CreatesSingleExecutableRecoveryRequestWithoutPlanning()
    {
        AnalyticsRecoveryRequestDto intake = new AnalyticsRecoveryRequestDto
                                             {
                                                 Id = 102L,
                                                 PublicId =
                                                         Guid
                                                                .Parse("11111111-2222-3333-4444-555555555555"),
                                                 Category =
                                                         nameof(SyncAnalyticsCategory
                                                                       .ConversationsDetails),
                                                 Status = AnalyticsRecoveryRequestStatus
                                                        .Running,
                                                 GenesysJobId = "JOB-123"
                                             };

        Mock<IRecoveryIntakeWorkRepository> intakeRepository =
                new Mock<IRecoveryIntakeWorkRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        intakeRepository.Setup(x => x.TryStartNextPendingAsync(null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(intake);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                   SyncMode.Recovery,
                                                                   null,
                                                                   null,
                                                                   "JOB-123",
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(BuildResolveResult(301L));

        intakeRepository.Setup(x => x.TryMarkCompletedAsync(102L, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        MaterializeRecoveryIntakeCommandHandler sut =
                new MaterializeRecoveryIntakeCommandHandler(intakeRepository.Object,
                                                            syncRequestRepository.Object,
                                                            intervalPlanner.Object);

        bool result = await sut.Handle(new MaterializeRecoveryIntakeCommand(null), CancellationToken.None);

        Assert.True(result);
        intakeRepository.VerifyAll();
        syncRequestRepository.VerifyAll();
        intervalPlanner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenCompletionTransitionFails_MarksIntakeFailedAndReturnsTrue()
    {
        UtcInterval sourceInterval = new UtcInterval(new DateTimeOffset(2026,
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
                                                                        TimeSpan.Zero));

        AnalyticsRecoveryRequestDto intake = new AnalyticsRecoveryRequestDto
                                             {
                                                 Id = 104L,
                                                 PublicId =
                                                         Guid
                                                                .Parse("aaaaaaaa-1111-2222-3333-bbbbbbbbbbbb"),
                                                 Category =
                                                         nameof(SyncAnalyticsCategory
                                                                       .UsersDetails),
                                                 Status = AnalyticsRecoveryRequestStatus
                                                        .Running,
                                                 Interval = sourceInterval.ToString()
                                             };

        Mock<IRecoveryIntakeWorkRepository> intakeRepository =
                new Mock<IRecoveryIntakeWorkRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        intakeRepository.Setup(x => x.TryStartNextPendingAsync(null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(intake);

        intervalPlanner.Setup(x => x.PlanAsync(SyncAnalyticsCategory.UsersDetails,
                                               sourceInterval,
                                               It.IsAny<CancellationToken>()))
                       .ReturnsAsync([new PlannedIntervalDto(sourceInterval, 10)]);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   sourceInterval.ToString(),
                                                                   null,
                                                                   null,
                                                                   It.IsAny<CancellationToken>()))
                             .ReturnsAsync(BuildResolveResult(401L));

        intakeRepository.Setup(x => x.TryMarkCompletedAsync(104L, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(false);

        intakeRepository.Setup(x => x.TryMarkFailedAsync(104L,
                                                         It.Is<string>(reason =>
                                                                               reason
                                                                                      .Contains("could not be marked completed",
                                                                                           StringComparison
                                                                                                  .Ordinal)),
                                                         It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        MaterializeRecoveryIntakeCommandHandler sut =
                new MaterializeRecoveryIntakeCommandHandler(intakeRepository.Object,
                                                            syncRequestRepository.Object,
                                                            intervalPlanner.Object);

        bool result = await sut.Handle(new MaterializeRecoveryIntakeCommand(null), CancellationToken.None);

        Assert.True(result);
        intakeRepository.VerifyAll();
        syncRequestRepository.VerifyAll();
        intervalPlanner.VerifyAll();
    }

    [Fact]
    public async Task Handle_WhenMaterializationFails_MarksIntakeFailedAndReturnsTrue()
    {
        AnalyticsRecoveryRequestDto intake = new AnalyticsRecoveryRequestDto
                                             {
                                                 Id = 103L,
                                                 PublicId =
                                                         Guid
                                                                .Parse("99999999-8888-7777-6666-555555555555"),
                                                 Category =
                                                         nameof(SyncAnalyticsCategory
                                                                       .UsersDetails),
                                                 Status = AnalyticsRecoveryRequestStatus
                                                        .Running,
                                                 Interval = "invalid"
                                             };

        Mock<IRecoveryIntakeWorkRepository> intakeRepository =
                new Mock<IRecoveryIntakeWorkRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IIntervalPlanner> intervalPlanner = new Mock<IIntervalPlanner>(MockBehavior.Strict);

        intakeRepository.Setup(x => x.TryStartNextPendingAsync(null, It.IsAny<CancellationToken>()))
                        .ReturnsAsync(intake);

        intakeRepository.Setup(x => x.TryMarkFailedAsync(103L,
                                                         It.Is<string>(reason => reason.Length > 0),
                                                         It.IsAny<CancellationToken>()))
                        .ReturnsAsync(true);

        MaterializeRecoveryIntakeCommandHandler sut =
                new MaterializeRecoveryIntakeCommandHandler(intakeRepository.Object,
                                                            syncRequestRepository.Object,
                                                            intervalPlanner.Object);

        bool result = await sut.Handle(new MaterializeRecoveryIntakeCommand(null), CancellationToken.None);

        Assert.True(result);
        intakeRepository.VerifyAll();
        syncRequestRepository.VerifyNoOtherCalls();
        intervalPlanner.VerifyNoOtherCalls();
    }

    #region ========== *** Private Section *** ==========

    private static SyncRequestResolveResult BuildResolveResult(long id)
    {
        return new SyncRequestResolveResult
               {
                   Id = id,
                   PublicId = Guid.NewGuid(),
                   RequestAction = SyncRequestResolveAction.Created
               };
    }

    #endregion
}
