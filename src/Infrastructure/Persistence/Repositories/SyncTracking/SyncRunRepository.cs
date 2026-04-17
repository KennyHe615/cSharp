using Application.Abstractions.Persistence;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using SharedKernel.Time;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for sync run lifecycle persistence.
/// </summary>
public sealed class SyncRunRepository(AppDbContext dbContext,
                                      IUnitOfWork uow,
                                      IDateTimeProvider dateTimeProvider) : ISyncRunRepository
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="requestId"/> does not exist.
    /// </exception>
    public async Task<long> StartNewRunAsync(long requestId, CancellationToken ct)
    {
        IExecutionStrategy strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
                                           {
                                               SyncRequestEntity request = await GetRequestOrThrowAsync(requestId, ct)
                                                                              .ConfigureAwait(false);

                                               DateTimeOffset now = dateTimeProvider.EstNowOffset;

                                               await using IDbContextTransaction tx =
                                                   await dbContext.Database.BeginTransactionAsync(ct)
                                                                  .ConfigureAwait(false);

                                               // 1) Find currently active run (if any) for the same request scope.
                                               SyncRunEntity? activeRun = await GetActiveRunAsync(requestId, ct)
                                                                             .ConfigureAwait(false);

                                               int nextAttempt = 1;
                                               if (activeRun is not null)
                                               {
                                                   // 2) Supersede previous active run before creating a replacement.
                                                   nextAttempt = activeRun.AttemptNo + 1;
                                                   await MarkRunAsSupersededAsync(activeRun, now, ct)
                                                      .ConfigureAwait(false);
                                               }

                                               // 3) Create a new pending run and immediately promote it to running.
                                               SyncRunEntity newRun =
                                                   await CreatePendingRunAsync(requestId, nextAttempt, ct)
                                                      .ConfigureAwait(false);

                                               await PromoteRunToRunningAsync(newRun, now, ct)
                                                  .ConfigureAwait(false);

                                               if (activeRun is not null)
                                               {
                                                   // 4) Link superseded run to the newly created run for traceability.
                                                   activeRun.SupersededByRunId = newRun.Id;
                                                   await uow.SaveChangesAsync(ct)
                                                            .ConfigureAwait(false);
                                               }

                                               // 5) Move request pointer to the new current run and mark request as RUNNING.
                                               await SetCurrentRunAsync(request, newRun.Id, ct)
                                                  .ConfigureAwait(false);

                                               await tx.CommitAsync(ct)
                                                       .ConfigureAwait(false);

                                               return newRun.Id;
                                           })
                             .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct)
    {
        SyncRunEntity? run = await dbContext.Set<SyncRunEntity>()
                                            .AsNoTracking()
                                            .FirstOrDefaultAsync(x => x.Id == runId, ct)
                                            .ConfigureAwait(false);

        if (run is null) return false;

        long? currentRunId = await dbContext.Set<SyncRequestEntity>()
                                            .Where(x => x.Id == run.RequestId)
                                            .Select(x => x.CurrentRunId)
                                            .FirstOrDefaultAsync(ct)
                                            .ConfigureAwait(false);

        bool isActive = IsActive(run.Status);

        return currentRunId == runId && isActive;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="runId"/> does not exist.
    /// </exception>
    public async Task MarkCompletedAsync(long runId, CancellationToken ct)
    {
        await ApplyFinalStatusAsync(runId,
                                    SyncRunStatus.Completed,
                                    null,
                                    null,
                                    ct)
           .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="runId"/> does not exist.
    /// </exception>
    public async Task MarkFailedAsync(long runId, string reason, CancellationToken ct)
    {
        await ApplyFinalStatusAsync(runId,
                                    SyncRunStatus.Failed,
                                    null,
                                    NormalizeRunFailureSummary(SyncRunStatus.Failed, reason),
                                    ct)
           .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="runId"/> does not exist.
    /// </exception>
    public async Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct)
    {
        await ApplyFinalStatusAsync(runId,
                                    SyncRunStatus.Superseded,
                                    supersededByRunId,
                                    null,
                                    ct)
           .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified <paramref name="runId"/> does not exist.
    /// </exception>
    public async Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct)
    {
        await ApplyFinalStatusAsync(runId,
                                    SyncRunStatus.Canceled,
                                    null,
                                    NormalizeRunFailureSummary(SyncRunStatus.Canceled, reason),
                                    ct)
           .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Gets a sync request entity by id or throws if not found.
    /// </summary>
    /// <param name="requestId">Sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="SyncRequestEntity"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified request id does not exist.
    /// </exception>
    private async Task<SyncRequestEntity> GetRequestOrThrowAsync(long requestId, CancellationToken ct)
    {
        SyncRequestEntity? request = await dbContext.Set<SyncRequestEntity>()
                                                    .FirstOrDefaultAsync(x => x.Id == requestId, ct)
                                                    .ConfigureAwait(false);

        return request ?? throw new InvalidOperationException($"Sync request '{requestId}' was not found.");
    }

    /// <summary>
    /// Gets a sync run entity by id or throws if not found.
    /// </summary>
    /// <param name="runId">Sync run id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching run entity.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified run id does not exist.
    /// </exception>
    private async Task<SyncRunEntity> GetRunOrThrowAsync(long runId, CancellationToken ct)
    {
        SyncRunEntity? run = await dbContext.Set<SyncRunEntity>()
                                            .FirstOrDefaultAsync(x => x.Id == runId, ct)
                                            .ConfigureAwait(false);

        return run ?? throw new InvalidOperationException($"Sync run '{runId}' was not found.");
    }

    /// <summary>
    /// Applies a terminal run transition and mirrors terminal state to its parent request.
    /// Request status is intentionally derived from current run terminal state for dedupe/reopen flows.
    /// </summary>
    private async Task ApplyFinalStatusAsync(long runId,
                                             SyncRunStatus finalStatus,
                                             long? supersededByRunId,
                                             string? failureReason,
                                             CancellationToken ct)
    {
        SyncRunEntity run = await GetRunOrThrowAsync(runId, ct)
                               .ConfigureAwait(false);

        if (!IsActive(run.Status)) return;

        run.Status = finalStatus;
        run.RunCompletedAt = dateTimeProvider.EstNowOffset;

        if (supersededByRunId.HasValue) run.SupersededByRunId = supersededByRunId.Value;

        run.FailureReason = failureReason;

        SyncRequestEntity request = await GetRequestOrThrowAsync(run.RequestId, ct)
                                       .ConfigureAwait(false);

        SyncRequestStatus? requestFinalStatus = MapRequestFinalStatus(finalStatus);
        if (requestFinalStatus.HasValue)
        {
            request.Status = requestFinalStatus.Value;
        }

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    private async Task SetCurrentRunAsync(SyncRequestEntity request, long runId, CancellationToken ct)
    {
        request.CurrentRunId = runId;
        request.Status = SyncRequestStatus.Running;

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    private async Task<SyncRunEntity?> GetActiveRunAsync(long requestId, CancellationToken ct)
    {
        return await dbContext.Set<SyncRunEntity>()
                              .FirstOrDefaultAsync(x => x.RequestId == requestId
                                                        && (x.Status    == SyncRunStatus.Pending
                                                            || x.Status == SyncRunStatus.Running),
                                                   ct)
                              .ConfigureAwait(false);
    }

    private async Task<SyncRunEntity> CreatePendingRunAsync(long requestId, int attemptNo, CancellationToken ct)
    {
        SyncRunEntity newRun = new SyncRunEntity
                               {
                                   RequestId = requestId,
                                   Status = SyncRunStatus.Pending,
                                   AttemptNo = attemptNo,
                                   RunStartedAt = null,
                                   RunCompletedAt = null
                               };

        await uow.UpsertAsync(newRun, ct: ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);

        return newRun;
    }

    private async Task PromoteRunToRunningAsync(SyncRunEntity run, DateTimeOffset startedAt, CancellationToken ct)
    {
        run.Status = SyncRunStatus.Running;
        run.RunStartedAt = startedAt;

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    private async Task MarkRunAsSupersededAsync(SyncRunEntity run, DateTimeOffset now, CancellationToken ct)
    {
        run.Status = SyncRunStatus.Superseded;
        run.RunCompletedAt = now;

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    private static bool IsActive(SyncRunStatus status)
    {
        return status is SyncRunStatus.Pending or SyncRunStatus.Running;
    }

    /// <summary>
    /// Maps run terminal status to request terminal status.
    /// Superseded does not map because request lifecycle is represented by the latest current run.
    /// </summary>
    private static SyncRequestStatus? MapRequestFinalStatus(SyncRunStatus runFinalStatus)
    {
        return runFinalStatus switch
               {
                   SyncRunStatus.Completed => SyncRequestStatus.Completed,
                   SyncRunStatus.Failed => SyncRequestStatus.Failed,
                   SyncRunStatus.Canceled => SyncRequestStatus.Canceled,
                   _ => null
               };
    }

    private static string NormalizeRunFailureSummary(SyncRunStatus finalStatus, string? rawReason)
    {
        string reason = (rawReason ?? string.Empty).Trim();

        if (finalStatus == SyncRunStatus.Canceled)
        {
            if (string.IsNullOrWhiteSpace(reason)) return "Run was canceled.";

            return reason.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                       ? "Run was canceled due to timeout."
                       : "Run was canceled by caller or host.";
        }

        if (reason.Contains("not wired yet", StringComparison.OrdinalIgnoreCase)
            || reason.Contains("not supported", StringComparison.OrdinalIgnoreCase))
        {
            return "Requested sync category is not supported in the current release.";
        }

        if (reason.Contains("temporarily disabled", StringComparison.OrdinalIgnoreCase))
        {
            return "Requested sync pipeline is temporarily disabled.";
        }

        return reason.Contains("timeout", StringComparison.OrdinalIgnoreCase)
                   ? "Run failed due to timeout."
                   : "Run failed. See checkpoint failure_reason for step-level details.";
    }

    #endregion
}
