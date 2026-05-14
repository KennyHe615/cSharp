using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Mediator;

using SharedKernel.Time;


namespace Application.Features.SyncTracking.Analytics;

/// <summary>
/// Handles one UsersDetails incremental cycle by joining active executable work first,
/// otherwise reserving the next contiguous incremental window.
/// </summary>
public sealed class RunUsersDetailsIncrementalCycleCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                                  IIncrementalSyncWindowRepository
                                                                          incrementalSyncWindowRepository,
                                                                  IDateTimeProvider dateTimeProvider,
                                                                  ISyncRequestRunner syncRequestRunner,
                                                                  IIntervalPlanner intervalPlanner)
        : IRequestHandler<RunUsersDetailsIncrementalCycleCommand, long?>
{
    /// <summary>
    /// Executes one UsersDetails incremental cycle and returns the last executed sync request id when work was found.
    /// </summary>
    /// <param name="request">Cycle command containing the LOB to reserve against.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The last executed sync request id when work ran; otherwise <c>null</c>.</returns>
    public async Task<long?> Handle(RunUsersDetailsIncrementalCycleCommand request, CancellationToken ct = default)
    {
        long? lastRequestId = null;
        DateTimeOffset cycleCutoffEastern = dateTimeProvider.EstNowOffset;

        while (true)
        {
            long? drainedRequestId = await DrainJoinableWorkAsync(ct)
                                            .ConfigureAwait(false);

            if (drainedRequestId.HasValue)
            {
                lastRequestId = drainedRequestId.Value;

                continue;
            }

            IncrementalSyncWindowReservation reservation =
                    await incrementalSyncWindowRepository.ReserveNextWindowAsync(request.Lob,
                                                              SyncAnalyticsCategory.UsersDetails,
                                                              cycleCutoffEastern,
                                                              ct)
                                                         .ConfigureAwait(false);

            if (!reservation.Reserved || string.IsNullOrWhiteSpace(reservation.IntervalUtc)) return lastRequestId;

            lastRequestId = await ExecuteOriginalIntervalAsync(reservation.IntervalUtc, ct)
                                   .ConfigureAwait(false);
        }
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Drains currently joinable UsersDetails incremental requests until no pending or running executable work remains.
    /// </summary>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The last executed request id when work was found; otherwise <c>null</c>.</returns>
    private async Task<long?> DrainJoinableWorkAsync(CancellationToken ct)
    {
        long? lastRequestId = null;

        while (true)
        {
            SyncRequestDto? joinableRequest =
                    await syncRequestRepository
                         .GetNextJoinableIncrementalRequestAsync(nameof(SyncAnalyticsCategory.UsersDetails), ct)
                         .ConfigureAwait(false);

            if (joinableRequest is null) return lastRequestId;

            await syncRequestRunner.ExecuteJoinableAsync(joinableRequest.Id, ct)
                                   .ConfigureAwait(false);

            lastRequestId = joinableRequest.Id;
        }
    }

    /// <summary>
    /// Plans the original reserved interval into executable slices, creates those sync requests,
    /// and executes each created slice.
    /// </summary>
    /// <param name="intervalText">Original reserved UTC interval.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns>The last executable sync request id.</returns>
    private async Task<long> ExecuteOriginalIntervalAsync(string intervalText, CancellationToken ct)
    {
        UtcInterval interval = UtcInterval.Parse(intervalText);

        IReadOnlyList<PlannedIntervalDto> plannedIntervals =
                await intervalPlanner.PlanAsync(SyncAnalyticsCategory.UsersDetails, interval, ct)
                                     .ConfigureAwait(false);

        List<(SyncRequestResolveResult ResolveResult, string Interval)> plannedScopes = [];

        foreach (PlannedIntervalDto plannedInterval in plannedIntervals)
        {
            string plannedIntervalText = plannedInterval.Interval.ToString();

            SyncRequestResolveResult resolveResult =
                    await syncRequestRepository.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                        SyncMode.Incremental,
                                                                        plannedIntervalText,
                                                                        null,
                                                                        null,
                                                                        ct)
                                               .ConfigureAwait(false);

            plannedScopes.Add((resolveResult, plannedIntervalText));
        }

        long lastRequestId = 0;

        foreach ((SyncRequestResolveResult resolveResult, string plannedIntervalText) in plannedScopes)
        {
            lastRequestId = await ExecutePlannedScopeAsync(resolveResult, plannedIntervalText, ct)
                                   .ConfigureAwait(false);
        }

        return lastRequestId;
    }

    /// <summary>
    /// Executes one provider-safe incremental request and creates a recovery request if execution fails.
    /// </summary>
    /// <param name="resolveResult">Resolved executable request.</param>
    /// <param name="interval">Provider-safe interval slice.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The executed sync request id.</returns>
    private async Task<long> ExecutePlannedScopeAsync(SyncRequestResolveResult resolveResult,
                                                      string interval,
                                                      CancellationToken ct)
    {
        try
        {
            await syncRequestRunner.ExecuteJoinableAsync(resolveResult.Id, ct)
                                   .ConfigureAwait(false);

            return resolveResult.Id;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            try
            {
                _ = await syncRequestRepository.CreateOrGetByScopeAsync(nameof(SyncAnalyticsCategory.UsersDetails),
                                                                        SyncMode.Recovery,
                                                                        interval,
                                                                        null,
                                                                        null,
                                                                        ct)
                                               .ConfigureAwait(false);
            }
            catch
            {
                // Best-effort only. Do not hide original incremental failure.
            }

            throw;
        }
    }

    #endregion
}
