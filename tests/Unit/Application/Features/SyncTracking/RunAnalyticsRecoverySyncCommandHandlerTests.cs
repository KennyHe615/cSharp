using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsRecoverySyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_AnalyticsCategoryWithInterval_ResolvesRecoveryScope_ExecutesAndReturnsRequestId()
    {
        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 101L,
                                                     PublicId = Guid.Parse(
                                                             "aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                                                     RequestAction =
                                                             SyncRequestResolveAction
                                                                    .Created
                                                 };

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                   2,
                                                                   null,
                                                                   CancellationToken.None))
                             .ReturnsAsync(resolveResult);

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(101L, CancellationToken.None))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    2,
                                                    null);

        long result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(101L, result);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                    SyncMode.Recovery,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    2,
                                                                    null,
                                                                    CancellationToken.None),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(101L, CancellationToken.None), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ConversationsDetailsWithGenesysJobId_ResolvesRecoveryScope_ExecutesAndReturnsRequestId()
    {
        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 151L,
                                                     PublicId = Guid.Parse(
                                                             "bbbbbbbb-cccc-dddd-eeee-ffffffffffff"),
                                                     RequestAction =
                                                             SyncRequestResolveAction
                                                                    .ReusedActive
                                                 };

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                   SyncMode.Recovery,
                                                                   null,
                                                                   null,
                                                                   "JOB-123",
                                                                   CancellationToken.None))
                             .ReturnsAsync(resolveResult);

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(151L, CancellationToken.None))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        long result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(151L, result);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                    SyncMode.Recovery,
                                                                    null,
                                                                    null,
                                                                    "JOB-123",
                                                                    CancellationToken.None),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(151L, CancellationToken.None), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task
            Handle_GenesysJobIdForNonConversationsDetails_ThrowsInvalidOperationException_WithoutRepositoryOrRunnerCall()
    {
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                    null,
                                                    null,
                                                    "JOB-123");

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal("GenesysJobId is only supported for ConversationsDetails recovery.", ex.Message);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException_WithoutRepositoryOrRunnerCall()
    {
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand((SyncAnalyticsCategory)999,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Recovery mode is not supported for category", ex.Message);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_RunExecutionThrows_RethrowsSameException()
    {
        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 201L,
                                                     PublicId = Guid.Parse(
                                                             "11111111-2222-3333-4444-555555555555"),
                                                     RequestAction =
                                                             SyncRequestResolveAction
                                                                    .ReusedActive
                                                 };

        InvalidOperationException original = new InvalidOperationException("run failed");

        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                   SyncMode.Recovery,
                                                                   null,
                                                                   null,
                                                                   "JOB-999",
                                                                   CancellationToken.None))
                             .ReturnsAsync(resolveResult);

        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);
        syncRequestRunner.Setup(x => x.ExecuteAsync(201L, CancellationToken.None))
                         .ThrowsAsync(original);

        RunAnalyticsRecoverySyncCommandHandler sut =
                new RunAnalyticsRecoverySyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-999");

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                                    SyncMode.Recovery,
                                                                    null,
                                                                    null,
                                                                    "JOB-999",
                                                                    CancellationToken.None),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(201L, CancellationToken.None), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }
}
