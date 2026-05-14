using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence.SyncTracking;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.Analytics.Shared;
using Application.Features.SyncTracking;

using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Analytics.Shared;

public sealed class AnalyticsPageSyncCoordinatorTests
{
    [Fact]
    public async Task ExecuteAsync_WhenAllPagesComplete_ReturnsSuccess()
    {
        const long runId = 10L;
        const long runItemId = 100L;
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        const int pageNumber = 2;

        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));
        string step = SyncRunItemSteps.AnalyticsPageFetch(nameof(SyncAnalyticsCategory.UsersDetails));

        string? capturedClaimedBy = null;
        Guid capturedLeaseToken = Guid.Empty;
        int claimCallCount = 0;
        List<int> processedPages = [];

        Mock<ISyncRunItemRepository> syncRunItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRunItemRepository.Setup(x => x.SeedPendingPagesAsync(runId,
                                                                 step,
                                                                 It.Is<IReadOnlyCollection<int>>(pages =>
                                                                         pages.SequenceEqual(new[]
                                                                             {
                                                                                 pageNumber
                                                                             })),
                                                                 ct))
                             .Returns(Task.CompletedTask);

        syncRunItemRepository.Setup(x => x.ClaimNextPageAsync(runId,
                                                              step,
                                                              It.IsAny<string>(),
                                                              It.IsAny<Guid>(),
                                                              now,
                                                              now.AddMinutes(5),
                                                              ct))
                             .Returns((long _,
                                       string _,
                                       string claimedBy,
                                       Guid leaseToken,
                                       DateTimeOffset _,
                                       DateTimeOffset _,
                                       CancellationToken _) =>
                                      {
                                          claimCallCount++;

                                          if (claimCallCount > 1)
                                          {
                                              return Task.FromResult<SyncRunItemDto?>(null);
                                          }

                                          capturedClaimedBy = claimedBy;
                                          capturedLeaseToken = leaseToken;

                                          return Task.FromResult<SyncRunItemDto?>(new SyncRunItemDto
                                              {
                                                  Id = runItemId,
                                                  RunId = runId,
                                                  Step = step,
                                                  PageNumber = pageNumber,
                                                  Status = SyncRunStatus.Running,
                                                  ClaimedBy = claimedBy,
                                                  LeaseToken = leaseToken,
                                                  ClaimedAtEastern = now,
                                                  ClaimExpiresAtEastern = now.AddMinutes(5),
                                                  LastHeartbeatAtEastern = now
                                              });
                                      });

        syncRunItemRepository.Setup(x => x.TryMarkCompletedAsync(runItemId,
                                                                 It.Is<string>(value => value == capturedClaimedBy),
                                                                 It.Is<Guid>(value => value   == capturedLeaseToken),
                                                                 ct))
                             .ReturnsAsync(true);

        syncRunItemRepository.Setup(x => x.HasUnfinishedPagesAsync(runId, step, ct))
                             .ReturnsAsync(false);

        syncRunItemRepository.Setup(x => x.GetFailedPagesAsync(runId, step, ct))
                             .ReturnsAsync(Array.Empty<SyncRunItemDto>());

        AnalyticsPageSyncCoordinator sut = new AnalyticsPageSyncCoordinator(syncRunItemRepository.Object,
                                                                            syncRequestRepository.Object,
                                                                            dateTimeProvider.Object,
                                                                            NullLogger<AnalyticsPageSyncCoordinator>
                                                                                   .Instance);

        AnalyticsPageSyncRequest request = BuildRequest(runId,
                                                        SyncMode.Incremental,
                                                        interval,
                                                        pageNumber,
                                                        [pageNumber],
                                                        page =>
                                                        {
                                                            processedPages.Add(page);

                                                            return Task.CompletedTask;
                                                        });

        SyncExecutionResult result = await sut.ExecuteAsync(request, ct);

        Assert.False(result.CompletedWithRecoveryItems);
        Assert.False(result.Failed);
        Assert.Null(result.FailureReason);
        Assert.Equal(new[] { pageNumber }, processedPages);

        syncRequestRepository.Verify(x => x.CreateOrGetByScopeAsync(It.IsAny<string>(),
                                                                    It.IsAny<SyncMode>(),
                                                                    It.IsAny<string?>(),
                                                                    It.IsAny<int?>(),
                                                                    It.IsAny<string?>(),
                                                                    It.IsAny<CancellationToken>()),
                                     Times.Never);

        syncRunItemRepository.VerifyAll();
        syncRequestRepository.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenIncrementalPageFails_CreatesRecoveryRequestAndReturnsRecoveryItems()
    {
        const long runId = 20L;
        const long runItemId = 200L;
        const string interval = "2026-04-14T00:00:00Z/2026-04-14T00:30:00Z";
        const int pageNumber = 3;

        CancellationToken ct = CancellationToken.None;
        DateTimeOffset now = new DateTimeOffset(2026,
                                                4,
                                                14,
                                                9,
                                                0,
                                                0,
                                                TimeSpan.FromHours(-4));
        string step = SyncRunItemSteps.AnalyticsPageFetch(nameof(SyncAnalyticsCategory.UsersDetails));

        string? capturedClaimedBy = null;
        Guid capturedLeaseToken = Guid.Empty;
        int claimCallCount = 0;

        Mock<ISyncRunItemRepository> syncRunItemRepository = new Mock<ISyncRunItemRepository>(MockBehavior.Strict);
        Mock<ISyncRequestRepository> syncRequestRepository = new Mock<ISyncRequestRepository>(MockBehavior.Strict);
        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);

        dateTimeProvider.Setup(x => x.EstNowOffset)
                        .Returns(now);

        syncRunItemRepository.Setup(x => x.SeedPendingPagesAsync(runId,
                                                                 step,
                                                                 It.Is<IReadOnlyCollection<int>>(pages =>
                                                                         pages.SequenceEqual(new[]
                                                                             {
                                                                                 pageNumber
                                                                             })),
                                                                 ct))
                             .Returns(Task.CompletedTask);

        syncRunItemRepository.Setup(x => x.ClaimNextPageAsync(runId,
                                                              step,
                                                              It.IsAny<string>(),
                                                              It.IsAny<Guid>(),
                                                              now,
                                                              now.AddMinutes(5),
                                                              ct))
                             .Returns((long _,
                                       string _,
                                       string claimedBy,
                                       Guid leaseToken,
                                       DateTimeOffset _,
                                       DateTimeOffset _,
                                       CancellationToken _) =>
                                      {
                                          claimCallCount++;

                                          if (claimCallCount > 1)
                                          {
                                              return Task.FromResult<SyncRunItemDto?>(null);
                                          }

                                          capturedClaimedBy = claimedBy;
                                          capturedLeaseToken = leaseToken;

                                          return Task.FromResult<SyncRunItemDto?>(new SyncRunItemDto
                                              {
                                                  Id = runItemId,
                                                  RunId = runId,
                                                  Step = step,
                                                  PageNumber = pageNumber,
                                                  Status = SyncRunStatus.Running,
                                                  ClaimedBy = claimedBy,
                                                  LeaseToken = leaseToken,
                                                  ClaimedAtEastern = now,
                                                  ClaimExpiresAtEastern = now.AddMinutes(5),
                                                  LastHeartbeatAtEastern = now
                                              });
                                      });

        syncRunItemRepository.Setup(x => x.TryMarkFailedAsync(runItemId,
                                                              It.Is<string>(value => value == capturedClaimedBy),
                                                              It.Is<Guid>(value => value   == capturedLeaseToken),
                                                              It.Is<string>(value => value.Contains("page failed",
                                                                                StringComparison.Ordinal)),
                                                              ct))
                             .ReturnsAsync(true);

        syncRunItemRepository.Setup(x => x.HasUnfinishedPagesAsync(runId, step, ct))
                             .ReturnsAsync(false);

        syncRunItemRepository.Setup(x => x.GetFailedPagesAsync(runId, step, ct))
                             .ReturnsAsync([
                                               new SyncRunItemDto
                                               {
                                                   Id = runItemId,
                                                   RunId = runId,
                                                   Step = step,
                                                   PageNumber = pageNumber,
                                                   Status = SyncRunStatus.Failed,
                                                   FailureReason = "page failed"
                                               }
                                           ]);

        syncRequestRepository.Setup(x => x.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                   SyncMode.Recovery,
                                                                   interval,
                                                                   pageNumber,
                                                                   null,
                                                                   ct))
                             .ReturnsAsync(new SyncRequestResolveResult
                                           {
                                               Id = 300L,
                                               PublicId = Guid.NewGuid(),
                                               RequestAction =
                                                       SyncRequestResolveAction.Created
                                           });

        AnalyticsPageSyncCoordinator sut = new AnalyticsPageSyncCoordinator(syncRunItemRepository.Object,
                                                                            syncRequestRepository.Object,
                                                                            dateTimeProvider.Object,
                                                                            NullLogger<AnalyticsPageSyncCoordinator>
                                                                                   .Instance);

        AnalyticsPageSyncRequest request = BuildRequest(runId,
                                                        SyncMode.Incremental,
                                                        interval,
                                                        pageNumber,
                                                        [pageNumber],
                                                        _ => throw new InvalidOperationException("page failed"));

        SyncExecutionResult result = await sut.ExecuteAsync(request, ct);

        Assert.True(result.CompletedWithRecoveryItems);
        Assert.False(result.Failed);
        Assert.Null(result.FailureReason);

        syncRunItemRepository.VerifyAll();
        syncRequestRepository.VerifyAll();
        dateTimeProvider.VerifyAll();
    }

    #region ========== *** Private Section *** ==========

    private static AnalyticsPageSyncRequest BuildRequest(long runId,
                                                         SyncMode mode,
                                                         string interval,
                                                         int? requestedPageNumber,
                                                         IReadOnlyCollection<int> pageNumbers,
                                                         Func<int, Task> processPageAsync)
    {
        return new AnalyticsPageSyncRequest(runId,
                                            SyncAnalyticsCategory.UsersDetails,
                                            mode,
                                            interval,
                                            requestedPageNumber,
                                            _ => Task.FromResult(pageNumbers),
                                            (pageNumber, _) => processPageAsync(pageNumber));
    }

    #endregion
}
