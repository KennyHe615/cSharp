using Application.Abstractions.External;
using Application.Abstractions.Normalization;
using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Analytics;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;
using Application.Contracts.ExternalApis.Genesys.UsersDetails;
using Application.DTOs.UsersDetails;
using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Moq;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.Analytics;

public sealed class UsersDetailsSyncExecutorTests
{
    [Fact]
    public async Task ExecuteAsync_NormalizesIntervalAndDelegatesToPageCoordinator()
    {
        const long runId = 10L;
        const string inputInterval = "2026-04-14T00:00:30Z/2026-04-14T00:30:45Z";
        const string normalizedInterval = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        const int pageNumber = 2;
        CancellationToken ct = CancellationToken.None;

        Mock<IAnalyticsUsersDetailsClient> usersDetailsClient =
                new Mock<IAnalyticsUsersDetailsClient>(MockBehavior.Strict);
        Mock<IUsersDetailsNormalizer> usersDetailsNormalizer = new Mock<IUsersDetailsNormalizer>(MockBehavior.Strict);
        Mock<IUserDetailsRepository> userDetailsRepository = new Mock<IUserDetailsRepository>(MockBehavior.Strict);
        Mock<IAnalyticsPageSyncCoordinator> pageSyncCoordinator =
                new Mock<IAnalyticsPageSyncCoordinator>(MockBehavior.Strict);

        pageSyncCoordinator.Setup(x => x.ExecuteAsync(It.Is<AnalyticsPageSyncRequest>(request =>
                                                              request.RunId == runId
                                                              && request.Category
                                                              == SyncAnalyticsCategory.UsersDetails
                                                              && request.Mode     == SyncMode.Incremental
                                                              && request.Interval == normalizedInterval
                                                              && request.RequestedPageNumber
                                                              == pageNumber),
                                                      ct))
                           .ReturnsAsync(new SyncExecutionResult(CompletedWithRecoveryItems: false));

        UsersDetailsSyncExecutor sut = new UsersDetailsSyncExecutor(usersDetailsClient.Object,
                                                                    usersDetailsNormalizer.Object,
                                                                    userDetailsRepository.Object,
                                                                    pageSyncCoordinator.Object);

        SyncExecutionResult result = await sut.ExecuteAsync(runId,
                                                            SyncMode.Incremental,
                                                            inputInterval,
                                                            pageNumber,
                                                            null,
                                                            ct);

        Assert.False(result.CompletedWithRecoveryItems);

        pageSyncCoordinator.VerifyAll();
    }

    [Fact]
    public async Task ResolvePagesAsync_WhenNoRequestedPage_UsesHitCountToBuildPages()
    {
        const long runId = 20L;
        const string interval = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        CancellationToken ct = CancellationToken.None;

        Mock<IAnalyticsUsersDetailsClient> usersDetailsClient =
                new Mock<IAnalyticsUsersDetailsClient>(MockBehavior.Strict);
        Mock<IUsersDetailsNormalizer> usersDetailsNormalizer = new Mock<IUsersDetailsNormalizer>(MockBehavior.Strict);
        Mock<IUserDetailsRepository> userDetailsRepository = new Mock<IUserDetailsRepository>(MockBehavior.Strict);
        Mock<IAnalyticsPageSyncCoordinator> pageSyncCoordinator =
                new Mock<IAnalyticsPageSyncCoordinator>(MockBehavior.Strict);

        usersDetailsClient.Setup(x => x.GetHitCountAsync(new DateTimeOffset(2026,
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
                                                                            TimeSpan.Zero),
                                                         ct))
                          .ReturnsAsync(101);

        pageSyncCoordinator.Setup(x => x.ExecuteAsync(It.IsAny<AnalyticsPageSyncRequest>(), ct))
                           .Returns<AnalyticsPageSyncRequest, CancellationToken>(async (request, token) =>
                            {
                                IReadOnlyCollection<int> pages = await request.ResolvePagesAsync(token);

                                Assert.Equal([1, 2], pages);

                                return new SyncExecutionResult(CompletedWithRecoveryItems: false);
                            });

        UsersDetailsSyncExecutor sut = new UsersDetailsSyncExecutor(usersDetailsClient.Object,
                                                                    usersDetailsNormalizer.Object,
                                                                    userDetailsRepository.Object,
                                                                    pageSyncCoordinator.Object);

        await sut.ExecuteAsync(runId,
                               SyncMode.Incremental,
                               interval,
                               null,
                               null,
                               ct);

        usersDetailsClient.VerifyAll();
        pageSyncCoordinator.VerifyAll();
    }

    [Fact]
    public async Task ProcessPageAsync_FetchesNormalizesAndPersistsPage()
    {
        const long runId = 30L;
        const string interval = "2026-04-14T00:00Z/2026-04-14T00:30Z";
        const int pageNumber = 4;
        CancellationToken ct = CancellationToken.None;

        UsersDetailsRawContract raw = new UsersDetailsRawContract();
        IReadOnlyCollection<PrimaryPresenceDto> primaryPresence =
        [
            new PrimaryPresenceDto()
        ];
        IReadOnlyCollection<RoutingStatusDto> routingStatus =
        [
            new RoutingStatusDto()
        ];

        Mock<IAnalyticsUsersDetailsClient> usersDetailsClient =
                new Mock<IAnalyticsUsersDetailsClient>(MockBehavior.Strict);
        Mock<IUsersDetailsNormalizer> usersDetailsNormalizer = new Mock<IUsersDetailsNormalizer>(MockBehavior.Strict);
        Mock<IUserDetailsRepository> userDetailsRepository = new Mock<IUserDetailsRepository>(MockBehavior.Strict);
        Mock<IAnalyticsPageSyncCoordinator> pageSyncCoordinator =
                new Mock<IAnalyticsPageSyncCoordinator>(MockBehavior.Strict);

        usersDetailsClient.Setup(x => x.GetUsersDetailsAsync(interval,
                                                             pageNumber,
                                                             null,
                                                             ct))
                          .ReturnsAsync(raw);

        usersDetailsNormalizer.Setup(x => x.NormalizeUsersDetails(raw))
                              .Returns((primaryPresence, routingStatus));

        userDetailsRepository.Setup(x => x.UpsertUserDetailsAsync(primaryPresence, routingStatus, ct))
                             .Returns(Task.CompletedTask);

        pageSyncCoordinator.Setup(x => x.ExecuteAsync(It.IsAny<AnalyticsPageSyncRequest>(), ct))
                           .Returns<AnalyticsPageSyncRequest, CancellationToken>(async (request, token) =>
                            {
                                await request.ProcessPageAsync(pageNumber, token);

                                return new SyncExecutionResult(CompletedWithRecoveryItems: false);
                            });

        UsersDetailsSyncExecutor sut = new UsersDetailsSyncExecutor(usersDetailsClient.Object,
                                                                    usersDetailsNormalizer.Object,
                                                                    userDetailsRepository.Object,
                                                                    pageSyncCoordinator.Object);

        await sut.ExecuteAsync(runId,
                               SyncMode.Incremental,
                               interval,
                               pageNumber,
                               null,
                               ct);

        usersDetailsClient.VerifyAll();
        usersDetailsNormalizer.VerifyAll();
        userDetailsRepository.VerifyAll();
        pageSyncCoordinator.VerifyAll();
    }
}
