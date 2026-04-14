using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Features.SyncTracking.References;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.References;

public sealed class RunReferencesFullSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_ShouldCreateOrGetRequest_ExecuteRunner_AndReturnRequestId()
    {
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRunner> syncRequestRunner = new Mock<ISyncRequestRunner>(MockBehavior.Strict);

        RunReferencesFullSyncCommand request = new RunReferencesFullSyncCommand(SyncReferenceCategory.WrapUpCode);
        CancellationToken ct = new CancellationTokenSource().Token;
        const long expectedRequestId = 12345L;

        syncRequestRepository
           .Setup(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                 SyncMode.Incremental,
                                                 null,
                                                 null,
                                                 null,
                                                 ct))
           .ReturnsAsync(expectedRequestId);

        syncRequestRunner.Setup(x => x.ExecuteAsync(expectedRequestId, ct))
                         .Returns(Task.CompletedTask);

        RunReferencesFullSyncCommandHandler sut =
            new RunReferencesFullSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        long actualRequestId = await sut.Handle(request, ct);

        Assert.Equal(expectedRequestId, actualRequestId);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                    SyncMode.Incremental,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct),
                                     Times.Once);

        syncRequestRunner.Verify(x => x.ExecuteAsync(expectedRequestId, ct), Times.Once);

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
        const long requestId = 77L;

        InvalidOperationException expected = new InvalidOperationException("runner failed");

        syncRequestRepository
           .Setup(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                 SyncMode.Incremental,
                                                 null,
                                                 null,
                                                 null,
                                                 ct))
           .ReturnsAsync(requestId);

        syncRequestRunner.Setup(x => x.ExecuteAsync(requestId, ct))
                         .ThrowsAsync(expected);

        RunReferencesFullSyncCommandHandler sut =
            new RunReferencesFullSyncCommandHandler(syncRequestRepository.Object, syncRequestRunner.Object);

        InvalidOperationException actual =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(request, ct));

        Assert.Same(expected, actual);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                    SyncMode.Incremental,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct),
                                     Times.Once);
        syncRequestRunner.Verify(x => x.ExecuteAsync(requestId, ct), Times.Once);

        syncRequestRepository.VerifyNoOtherCalls();
        syncRequestRunner.VerifyNoOtherCalls();
    }
}
