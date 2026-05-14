using Application.Abstractions.External;
using Application.Abstractions.Normalization;
using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Contracts.ExternalApis.Genesys.References;
using Application.DTOs.References;
using Application.Enums;
using Application.Features.SyncTracking.References;
using Application.Features.SyncTracking.Shared;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.References;

public sealed class ReferencesSyncOrchestratorTests
{
    [Fact]
    public async Task ExecuteAsync_Group_HappyPath_ShouldFetchNormalizeUpsertAndWriteCheckpoints()
    {
        Mock<IReferenceApiClient> referenceApiClient = new Mock<IReferenceApiClient>(MockBehavior.Strict);
        Mock<IReferencesNormalizer> referencesNormalizer = new Mock<IReferencesNormalizer>(MockBehavior.Strict);
        Mock<IReferencesRepository> referencesRepository = new Mock<IReferencesRepository>(MockBehavior.Strict);
        Mock<ISyncRunItemRepository> syncCheckpointRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);

        const long runId = 1001;
        CancellationToken ct = new CancellationTokenSource().Token;

        IReadOnlyCollection<GroupRawContract> raw = [new GroupRawContract { Id = Guid.NewGuid(), Name = "G1" }];
        IReadOnlyCollection<GroupDto> normalized =
        [
            new GroupDto
            {
                Id = raw.First()
                        .Id,
                Name = "G1"
            }
        ];

        string fetchStep = SyncRunItemSteps.ReferencesPageFetch(nameof(SyncReferenceCategory.Group));
        string summaryStep = SyncRunItemSteps.ReferencesSummary(nameof(SyncReferenceCategory.Group));

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetch-start",
                                                          SyncRunStatus.Running,
                                                          null,
                                                          ct))
                                .Returns(Task.CompletedTask);

        referenceApiClient.Setup(x => x.GetGroupsAsync(ct))
                          .ReturnsAsync(raw);

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetched:1",
                                                          SyncRunStatus.Completed,
                                                          null,
                                                          ct))
                                .Returns(Task.CompletedTask);

        referencesNormalizer.Setup(x => x.NormalizeGroups(raw))
                            .Returns(normalized);

        referencesRepository.Setup(x => x.UpsertGroupsAsync(normalized, ct))
                            .Returns(Task.CompletedTask);

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          summaryStep,
                                                          "upserted:1",
                                                          SyncRunStatus.Completed,
                                                          null,
                                                          ct))
                                .Returns(Task.CompletedTask);

        ReferencesSyncOrchestrator sut = new ReferencesSyncOrchestrator(referenceApiClient.Object,
                                                                        referencesNormalizer.Object,
                                                                        referencesRepository.Object,
                                                                        syncCheckpointRepository.Object);

        await sut.ExecuteAsync(runId, SyncReferenceCategory.Group, ct);

        referenceApiClient.Verify(x => x.GetGroupsAsync(ct), Times.Once);
        referencesNormalizer.Verify(x => x.NormalizeGroups(raw), Times.Once);
        referencesRepository.Verify(x => x.UpsertGroupsAsync(normalized, ct), Times.Once);
        syncCheckpointRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_User_ShouldThrowNotSupported_AndNotCallDependencies()
    {
        Mock<IReferenceApiClient> referenceApiClient = new Mock<IReferenceApiClient>(MockBehavior.Strict);
        Mock<IReferencesNormalizer> referencesNormalizer = new Mock<IReferencesNormalizer>(MockBehavior.Strict);
        Mock<IReferencesRepository> referencesRepository = new Mock<IReferencesRepository>(MockBehavior.Strict);
        Mock<ISyncRunItemRepository> syncCheckpointRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);

        ReferencesSyncOrchestrator sut = new ReferencesSyncOrchestrator(referenceApiClient.Object,
                                                                        referencesNormalizer.Object,
                                                                        referencesRepository.Object,
                                                                        syncCheckpointRepository.Object);

        await Assert.ThrowsAsync<NotSupportedException>(() => sut.ExecuteAsync(1,
                                                                               SyncReferenceCategory.User,
                                                                               CancellationToken.None));

        referenceApiClient.VerifyNoOtherCalls();
        referencesNormalizer.VerifyNoOtherCalls();
        referencesRepository.VerifyNoOtherCalls();
        syncCheckpointRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_Group_WhenFetchCanceled_ShouldWriteCanceledCheckpoint_AndRethrow()
    {
        Mock<IReferenceApiClient> referenceApiClient = new Mock<IReferenceApiClient>(MockBehavior.Strict);
        Mock<IReferencesNormalizer> referencesNormalizer = new Mock<IReferencesNormalizer>(MockBehavior.Strict);
        Mock<IReferencesRepository> referencesRepository = new Mock<IReferencesRepository>(MockBehavior.Strict);
        Mock<ISyncRunItemRepository> syncCheckpointRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);

        const long runId = 2002;
        CancellationToken ct = new CancellationTokenSource().Token;

        string fetchStep = SyncRunItemSteps.ReferencesPageFetch(nameof(SyncReferenceCategory.Group));

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetch-start",
                                                          SyncRunStatus.Running,
                                                          null,
                                                          ct))
                                .Returns(Task.CompletedTask);

        OperationCanceledException expected = new OperationCanceledException("cancelled");
        referenceApiClient.Setup(x => x.GetGroupsAsync(ct))
                          .ThrowsAsync(expected);

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetch-canceled",
                                                          SyncRunStatus.Canceled,
                                                          expected.Message,
                                                          CancellationToken.None))
                                .Returns(Task.CompletedTask);

        ReferencesSyncOrchestrator sut = new ReferencesSyncOrchestrator(referenceApiClient.Object,
                                                                        referencesNormalizer.Object,
                                                                        referencesRepository.Object,
                                                                        syncCheckpointRepository.Object);

        OperationCanceledException actual =
                await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(runId,
                                                                         SyncReferenceCategory.Group,
                                                                         ct));

        Assert.Same(expected, actual);

        referencesNormalizer.VerifyNoOtherCalls();
        referencesRepository.VerifyNoOtherCalls();
        syncCheckpointRepository.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_Group_WhenFetchFails_ShouldWriteFailedCheckpoint_AndRethrow()
    {
        Mock<IReferenceApiClient> referenceApiClient = new Mock<IReferenceApiClient>(MockBehavior.Strict);
        Mock<IReferencesNormalizer> referencesNormalizer = new Mock<IReferencesNormalizer>(MockBehavior.Strict);
        Mock<IReferencesRepository> referencesRepository = new Mock<IReferencesRepository>(MockBehavior.Strict);
        Mock<ISyncRunItemRepository> syncCheckpointRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);

        const long runId = 3003;
        CancellationToken ct = new CancellationTokenSource().Token;

        string fetchStep = SyncRunItemSteps.ReferencesPageFetch(nameof(SyncReferenceCategory.Group));

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetch-start",
                                                          SyncRunStatus.Running,
                                                          null,
                                                          ct))
                                .Returns(Task.CompletedTask);

        InvalidOperationException expected = new InvalidOperationException("fetch failed");
        referenceApiClient.Setup(x => x.GetGroupsAsync(ct))
                          .ThrowsAsync(expected);

        syncCheckpointRepository.Setup(x => x.UpsertAsync(runId,
                                                          fetchStep,
                                                          "fetch-failed",
                                                          SyncRunStatus.Failed,
                                                          expected.Message,
                                                          CancellationToken.None))
                                .Returns(Task.CompletedTask);

        ReferencesSyncOrchestrator sut = new ReferencesSyncOrchestrator(referenceApiClient.Object,
                                                                        referencesNormalizer.Object,
                                                                        referencesRepository.Object,
                                                                        syncCheckpointRepository.Object);

        InvalidOperationException actual =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(runId,
                                                                        SyncReferenceCategory.Group,
                                                                        ct));

        Assert.Same(expected, actual);

        referencesNormalizer.VerifyNoOtherCalls();
        referencesRepository.VerifyNoOtherCalls();
        syncCheckpointRepository.VerifyAll();
    }
}
