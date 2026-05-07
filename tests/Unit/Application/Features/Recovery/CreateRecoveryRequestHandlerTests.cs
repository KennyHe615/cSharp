using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.Recovery;
using Application.Enums;
using Application.Features.Recovery;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using tests.TestSupport.Time;

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
        AnalyticsRecoveryRequestResolveResult resolveResult = new AnalyticsRecoveryRequestResolveResult
                                                              {
                                                                  Id = 101L,
                                                                  PublicId = publicId,
                                                                  RequestAction =
                                                                          AnalyticsRecoveryRequestResolveAction
                                                                                 .Created
                                                              };

        Mock<IAnalyticsRecoveryRequestRepository> repository =
                new Mock<IAnalyticsRecoveryRequestRepository>(MockBehavior.Strict);

        repository.Setup(x => x.CreateOrGetActiveAsync(expectedCategory.ToString(),
                                                       It.IsAny<string?>(),
                                                       null,
                                                       It.IsAny<CancellationToken>()))
                  .ReturnsAsync(resolveResult);

        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository.Object);

        UtcInterval interval = UtcIntervalTestFactory.Create();

        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                    category,
                                                                                    interval,
                                                                                    null);

        CreateRecoveryRequestResponse response = await sut.Handle(command, CancellationToken.None);

        repository.Verify(x => x.CreateOrGetActiveAsync(expectedCategory.ToString(),
                                                        interval.ToString(),
                                                        null,
                                                        It.IsAny<CancellationToken>()),
                          Times.Once);

        repository.VerifyNoOtherCalls();

        Assert.True(response.Success);
        Assert.Equal("Recovery request accepted.", response.Message);
        Assert.Equal(publicId, response.Data.RequestId);
        Assert.Equal(nameof(AnalyticsRecoveryRequestResolveAction.Created), response.Data.RequestAction);
        Assert.Equal("CRC", response.Data.Lob);
        Assert.Equal(category.ToString(), response.Data.Category);
        Assert.Equal(interval, response.Data.Interval);
        Assert.Null(response.Data.GenesysJobId);
    }

    [Fact]
    public async Task Handle_WithGenesysJobId_PassesGenesysJobIdToRepository()
    {
        Guid publicId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        AnalyticsRecoveryRequestResolveResult resolveResult = new AnalyticsRecoveryRequestResolveResult
                                                              {
                                                                  Id = 202L,
                                                                  PublicId = publicId,
                                                                  RequestAction =
                                                                          AnalyticsRecoveryRequestResolveAction
                                                                                 .ReusedActive
                                                              };

        Mock<IAnalyticsRecoveryRequestRepository> repository =
                new Mock<IAnalyticsRecoveryRequestRepository>(MockBehavior.Strict);

        repository.Setup(x => x.CreateOrGetActiveAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
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

        repository.Verify(x => x.CreateOrGetActiveAsync(nameof(SyncAnalyticsCategory.ConversationsDetails),
                                                        null,
                                                        "JOB-123",
                                                        It.IsAny<CancellationToken>()),
                          Times.Once);

        repository.VerifyNoOtherCalls();

        Assert.True(response.Success);
        Assert.Equal("Recovery request accepted.", response.Message);
        Assert.Equal(publicId, response.Data.RequestId);
        Assert.Equal(nameof(AnalyticsRecoveryRequestResolveAction.ReusedActive), response.Data.RequestAction);
        Assert.Equal("JOB-123", response.Data.GenesysJobId);
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException()
    {
        Mock<IAnalyticsRecoveryRequestRepository> repository =
                new Mock<IAnalyticsRecoveryRequestRepository>(MockBehavior.Strict);

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

    [Fact]
    public async Task Handle_WithGenesysJobIdForNonConversationsDetails_ThrowsInvalidOperationException()
    {
        Mock<IAnalyticsRecoveryRequestRepository> repository =
                new Mock<IAnalyticsRecoveryRequestRepository>(MockBehavior.Strict);

        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository.Object);

        CreateRecoveryRequestCommand command =
                new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                 RecoveryCategory.UsersDetails,
                                                 null,
                                                 "JOB-123");

        InvalidOperationException ex =
                await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Equal("GenesysJobId is only supported for ConversationsDetails recovery.", ex.Message);
        repository.VerifyNoOtherCalls();
    }
}
