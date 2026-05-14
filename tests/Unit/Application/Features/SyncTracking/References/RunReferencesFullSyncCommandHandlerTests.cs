using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.References;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.References;

public sealed class RunReferencesFullSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldResolveFullRequest_ExecuteRunner_AndReturnRequestId()
    {
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunReferencesFullSyncCommand request = new RunReferencesFullSyncCommand(SyncReferenceCategory.WrapUpCode);
        CancellationToken ct = new CancellationTokenSource().Token;

        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 12345L,
                                                     PublicId =
                                                             Guid
                                                                    .Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
                                                     RequestAction =
                                                             SyncRequestResolveAction
                                                                    .Created
                                                 };

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                   SyncMode.Full,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(resolveResult);

        syncRequestRunner.Setup(x => x.ExecuteAsync(resolveResult.Id, ct))
                         .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        RunReferencesFullSyncCommandHandler sut =
                new RunReferencesFullSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        long actualRequestId = await sut.Handle(request, ct);

        Assert.Equal(resolveResult.Id, actualRequestId);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                    SyncMode.Full,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(resolveResult.Id, ct), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_WhenRunnerThrows_ShouldPropagateException()
    {
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunReferencesFullSyncCommand request = new RunReferencesFullSyncCommand(SyncReferenceCategory.Group);
        CancellationToken ct = new CancellationTokenSource().Token;

        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 77L,
                                                     PublicId =
                                                             Guid
                                                                    .Parse("11111111-2222-3333-4444-555555555555"),
                                                     RequestAction =
                                                             SyncRequestResolveAction
                                                                    .ReusedActive
                                                 };

        InvalidOperationException expected = new InvalidOperationException("runner failed");

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                   SyncMode.Full,
                                                                   null,
                                                                   null,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(resolveResult);

        syncRequestRunner.Setup(x => x.ExecuteAsync(resolveResult.Id, ct))
                         .ThrowsAsync(expected);

        RunReferencesFullSyncCommandHandler sut =
                new RunReferencesFullSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(request, ct));

        Assert.Same(expected, actual);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                    SyncMode.Full,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(resolveResult.Id, ct), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }
}
