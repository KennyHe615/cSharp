using Application.Abstractions.Orchestration;
using Application.Abstractions.Orchestration.Analytics;
using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking.Shared;

using Microsoft.Extensions.Logging;

using SharedKernel.Extensions;
using SharedKernel.Time;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Shared coordinator for analytics categories that execute by interval and page number.
/// </summary>
public sealed class AnalyticsPageSyncCoordinator(ISyncRunItemRepository syncRunItemRepository,
                                                 ISyncRequestRepository syncRequestRepository,
                                                 IDateTimeProvider dateTimeProvider,
                                                 ILogger<AnalyticsPageSyncCoordinator> logger)
        : IAnalyticsPageSyncCoordinator
{
    private static readonly TimeSpan ClaimLeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan ClaimPollDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan HeartbeatInterval = TimeSpan.FromMinutes(1);

    /// <inheritdoc />
    public async Task<SyncExecutionResult> ExecuteAsync(AnalyticsPageSyncRequest request, CancellationToken ct)
    {
        ValidateRequest(request);

        string step = SyncRunItemSteps.AnalyticsPageFetch(request.Category.ToString());

        IReadOnlyCollection<int> pageNumbers = await request.ResolvePagesAsync(ct)
                                                            .ConfigureAwait(false);

        await syncRunItemRepository.SeedPendingPagesAsync(request.RunId,
                                                          step,
                                                          pageNumbers,
                                                          ct)
                                   .ConfigureAwait(false);

        await ProcessClaimedPagesAsync(request, step, ct)
               .ConfigureAwait(false);

        IReadOnlyCollection<SyncRunItemDto> failedPages =
                await syncRunItemRepository.GetFailedPagesAsync(request.RunId, step, ct)
                                           .ConfigureAwait(false);

        if (failedPages.Count == 0)
        {
            return new SyncExecutionResult(CompletedWithRecoveryItems: false);
        }

        if (request.Mode == SyncMode.Recovery)
        {
            string failureReason =
                    $"{request.Category} recovery failed for {failedPages.Count} page(s) in interval '{request.Interval}'.";

            return new SyncExecutionResult(false, true, failureReason);
        }

        foreach (SyncRunItemDto failedPage in failedPages)
        {
            if (!failedPage.PageNumber.HasValue) continue;

            await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                SyncMode.Recovery,
                                                                request.Interval,
                                                                failedPage.PageNumber.Value,
                                                                null,
                                                                ct)
                                       .ConfigureAwait(false);
        }

        return new SyncExecutionResult(CompletedWithRecoveryItems: true);
    }

    #region ========== *** Private Section *** ==========

    private async Task ProcessClaimedPagesAsync(AnalyticsPageSyncRequest request, string step, CancellationToken ct)
    {
        string claimedBy = $"{Environment.MachineName}:{Guid.NewGuid():N}";

        while (true)
        {
            DateTimeOffset claimedAtEastern = dateTimeProvider.EstNowOffset;
            Guid leaseToken = Guid.NewGuid();

            SyncRunItemDto? page = await syncRunItemRepository.ClaimNextPageAsync(request.RunId,
                                                                   step,
                                                                   claimedBy,
                                                                   leaseToken,
                                                                   claimedAtEastern,
                                                                   claimedAtEastern.Add(ClaimLeaseDuration),
                                                                   ct)
                                                              .ConfigureAwait(false);

            if (page is null)
            {
                bool hasUnfinishedPages = await syncRunItemRepository.HasUnfinishedPagesAsync(request.RunId, step, ct)
                                                                     .ConfigureAwait(false);

                if (!hasUnfinishedPages) return;

                await Task.Delay(ClaimPollDelay, ct)
                          .ConfigureAwait(false);

                continue;
            }

            await ProcessClaimedPageAsync(request,
                                          page,
                                          claimedBy,
                                          leaseToken,
                                          ct)
                   .ConfigureAwait(false);
        }
    }

    private async Task ProcessClaimedPageAsync(AnalyticsPageSyncRequest request,
                                               SyncRunItemDto page,
                                               string claimedBy,
                                               Guid leaseToken,
                                               CancellationToken ct)
    {
        if (!page.PageNumber.HasValue)
        {
            throw new InvalidOperationException($"Analytics run item '{page.Id}' does not contain a page number.");
        }

        using CancellationTokenSource heartbeatCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        Task heartbeatTask = RunHeartbeatLoopAsync(page.Id,
                                                   claimedBy,
                                                   leaseToken,
                                                   heartbeatCts);

        try
        {
            await request.ProcessPageAsync(page.PageNumber.Value, heartbeatCts.Token)
                         .ConfigureAwait(false);

            await StopHeartbeatAsync(heartbeatCts, heartbeatTask, true)
                   .ConfigureAwait(false);

            bool completed = await syncRunItemRepository.TryMarkCompletedAsync(page.Id,
                                                                               claimedBy,
                                                                               leaseToken,
                                                                               ct)
                                                        .ConfigureAwait(false);

            if (!completed)
            {
                throw new
                        InvalidOperationException($"Analytics page run item '{page.Id}' could not be marked completed because its lease was lost.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            await StopHeartbeatAsync(heartbeatCts, heartbeatTask, false)
                   .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await StopHeartbeatAsync(heartbeatCts, heartbeatTask, false)
                   .ConfigureAwait(false);

            bool failed = await syncRunItemRepository.TryMarkFailedAsync(page.Id,
                                                                         claimedBy,
                                                                         leaseToken,
                                                                         ex.ToFailureReason(),
                                                                         ct)
                                                     .ConfigureAwait(false);

            if (!failed)
            {
                throw new
                        InvalidOperationException($"Analytics page run item '{page.Id}' failed, but its failed state could not be recorded because its lease was lost.",
                                                  ex);
            }

            logger.LogWarning(ex,
                              "Analytics page failed. Category = {Category}, RunItemId = {RunItemId}, Interval = {Interval}, PageNumber = {PageNumber}.",
                              request.Category,
                              page.Id,
                              request.Interval,
                              page.PageNumber.Value);
        }
    }

    private async Task RunHeartbeatLoopAsync(long runItemId,
                                             string claimedBy,
                                             Guid leaseToken,
                                             CancellationTokenSource heartbeatCts)
    {
        CancellationToken ct = heartbeatCts.Token;

        try
        {
            while (true)
            {
                await Task.Delay(HeartbeatInterval, ct)
                          .ConfigureAwait(false);

                DateTimeOffset heartbeatAtEastern = dateTimeProvider.EstNowOffset;
                bool heartbeatSucceeded = await syncRunItemRepository.TryHeartbeatAsync(runItemId,
                                                                          claimedBy,
                                                                          leaseToken,
                                                                          heartbeatAtEastern,
                                                                          heartbeatAtEastern
                                                                                 .Add(ClaimLeaseDuration),
                                                                          ct)
                                                                     .ConfigureAwait(false);

                if (heartbeatSucceeded) continue;

                await heartbeatCts.CancelAsync()
                                  .ConfigureAwait(false);

                throw new
                        InvalidOperationException($"Analytics page run item '{runItemId}' heartbeat failed because its lease was lost.");
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }
    }

    private static async Task StopHeartbeatAsync(CancellationTokenSource heartbeatCts,
                                                 Task heartbeatTask,
                                                 bool throwOnHeartbeatFailure)
    {
        await heartbeatCts.CancelAsync()
                          .ConfigureAwait(false);

        try
        {
            await heartbeatTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (heartbeatCts.IsCancellationRequested)
        {
        }
        catch when (!throwOnHeartbeatFailure)
        {
        }
    }

    private static void ValidateRequest(AnalyticsPageSyncRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.RunId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), request.RunId, "RunId must be greater than zero.");
        }

        if (request.Mode != SyncMode.Incremental && request.Mode != SyncMode.Recovery)
        {
            throw new NotSupportedException("Analytics page sync accepts Incremental or Recovery mode only.");
        }

        if (string.IsNullOrWhiteSpace(request.Interval))
        {
            throw new ArgumentException("Analytics page sync requires an interval.", nameof(request));
        }

        if (request.RequestedPageNumber is < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request),
                                                  request.RequestedPageNumber.Value,
                                                  "Requested page number must be greater than or equal to 1.");
        }

        ArgumentNullException.ThrowIfNull(request.ResolvePagesAsync);
        ArgumentNullException.ThrowIfNull(request.ProcessPageAsync);
    }

    #endregion
}
