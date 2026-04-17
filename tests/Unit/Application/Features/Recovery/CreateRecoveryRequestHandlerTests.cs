using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Recovery;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class CreateRecoveryRequestHandlerTests
{
    [Theory]
    [InlineData(RecoveryCategory.UsersDetails, SyncAnalyticsCategory.UsersDetails)]
    [InlineData(RecoveryCategory.ConversationsDetails, SyncAnalyticsCategory.ConversationsDetails)]
    [InlineData(RecoveryCategory.ConversationsAggregates, SyncAnalyticsCategory.ConversationsAggregates)]
    public async Task Handle_MapsCategoryAndResolvesRequest(RecoveryCategory category,
                                                            SyncAnalyticsCategory expectedCategory)
    {
        Guid publicId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 101L,
                                                     PublicId = publicId,
                                                     RequestAction =
                                                         SyncRequestResolveAction.Created
                                                 };

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.CreateOrGetByScopeAsync(expectedCategory.ToString(),
                                                        SyncMode.Recovery,
                                                        It.IsAny<string?>(),
                                                        null,
                                                        null,
                                                        It.IsAny<CancellationToken>()))
                  .ReturnsAsync(resolveResult);

        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository.Object);

        UtcInterval interval = BuildInterval();

        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                category,
                                                                                interval,
                                                                                null);

        (bool success, string message, object detail) = await sut.Handle(command, CancellationToken.None);

        repository.Verify(x => x.CreateOrGetByScopeAsync(expectedCategory.ToString(),
                                                         SyncMode.Recovery,
                                                         interval.ToString(),
                                                         null,
                                                         null,
                                                         It.IsAny<CancellationToken>()),
                          Times.Once);

        repository.VerifyNoOtherCalls();

        Assert.True(success);
        Assert.Equal("Recovery request resolved successfully.", message);

        Assert.NotNull(detail);

        Type detailType = detail.GetType();
        Assert.Equal(publicId, (Guid)detailType.GetProperty("RequestId")!.GetValue(detail)!);
        Assert.Equal(nameof(SyncRequestResolveAction.Created),
                     (string)detailType.GetProperty("RequestAction")!.GetValue(detail)!);
        Assert.Equal("CRC", (string)detailType.GetProperty("Lob")!.GetValue(detail)!);
        Assert.Equal(category.ToString(), (string)detailType.GetProperty("Category")!.GetValue(detail)!);
        Assert.Equal(interval, (UtcInterval?)detailType.GetProperty("Interval")!.GetValue(detail)!);
        Assert.Null((string?)detailType.GetProperty("GenesysJobId")!.GetValue(detail));
    }

    [Fact]
    public async Task Handle_WithGenesysJobId_PassesGenesysJobIdToRepository()
    {
        Guid publicId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        SyncRequestResolveResult resolveResult = new SyncRequestResolveResult
                                                 {
                                                     Id = 202L,
                                                     PublicId = publicId,
                                                     RequestAction =
                                                         SyncRequestResolveAction
                                                            .ReusedActive
                                                 };

        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        repository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                        SyncMode.Recovery,
                                                        null,
                                                        null,
                                                        "JOB-123",
                                                        It.IsAny<CancellationToken>()))
                  .ReturnsAsync(resolveResult);

        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository.Object);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("LCL"),
                                             RecoveryCategory.ConversationsDetails,
                                             null,
                                             "JOB-123");

        CreateRecoveryRequestResponse response = await sut.Handle(command, CancellationToken.None);

        repository.Verify(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                         SyncMode.Recovery,
                                                         null,
                                                         null,
                                                         "JOB-123",
                                                         It.IsAny<CancellationToken>()),
                          Times.Once);

        repository.VerifyNoOtherCalls();

        Type detailType = response.Data.GetType();
        Assert.Equal(publicId, (Guid)detailType.GetProperty("RequestId")!.GetValue(response.Data)!);
        Assert.Equal(nameof(SyncRequestResolveAction.ReusedActive),
                     (string)detailType.GetProperty("RequestAction")!.GetValue(response.Data)!);
        Assert.Equal("JOB-123", (string?)detailType.GetProperty("GenesysJobId")!.GetValue(response.Data));
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException()
    {
        Mock<ISyncRequestRepository> repository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository.Object);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             (RecoveryCategory)999,
                                             null,
                                             null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Unsupported recovery category", ex.Message);
        repository.VerifyNoOtherCalls();
    }

    #region ========== *** Private Section *** ==========

    private static UtcInterval BuildInterval()
    {
        return new UtcInterval(new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero),
                               new DateTimeOffset(2025,
                                                  1,
                                                  1,
                                                  1,
                                                  0,
                                                  0,
                                                  TimeSpan.Zero));
    }

    #endregion
}
