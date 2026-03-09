using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
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
        SyncRequestEntity request = await GetRequestOrThrowAsync(requestId, ct)
           .ConfigureAwait(false);

        DateTimeOffset now = dateTimeProvider.EstNowOffset;

        await using IDbContextTransaction tx = await dbContext.Database.BeginTransactionAsync(ct)
                                                              .ConfigureAwait(false);

        SyncRunEntity? activeRun = await GetActiveRunAsync(requestId, ct)
           .ConfigureAwait(false);

        int nextAttempt = 1;
        if (activeRun is not null)
        {
            nextAttempt = activeRun.AttemptNo + 1;
            await MarkRunAsSupersededAsync(activeRun, now, ct)
               .ConfigureAwait(false);
        }

        SyncRunEntity newRun = await CreatePendingRunAsync(requestId, nextAttempt, ct)
           .ConfigureAwait(false);

        await PromoteRunToRunningAsync(newRun, now, ct)
           .ConfigureAwait(false);

        if (activeRun is not null)
        {
            activeRun.SupersededByRunId = newRun.Id;
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);
        }

        await SetCurrentRunAsync(request, newRun.Id, ct)
           .ConfigureAwait(false);

        await tx.CommitAsync(ct)
                .ConfigureAwait(false);

        return newRun.Id;
    }

    /// <inheritdoc />
    public async Task<SyncRunDto?> GetByIdAsync(long runId, CancellationToken ct)
    {
        return await dbContext.Set<SyncRunEntity>()
                              .AsNoTracking()
                              .Where(x => x.Id == runId)
                              .Select(x => new SyncRunDto
                                           {
                                               Id = x.Id,
                                               RequestId = x.RequestId,
                                               Status = x.Status,
                                               SupersededByRunId = x.SupersededByRunId,
                                               AttemptNo = x.AttemptNo,
                                               RunStartedAt = x.RunStartedAt,
                                               RunCompletedAt = x.RunCompletedAt,
                                               FailureReason = x.FailureReason
                                           })
                              .FirstOrDefaultAsync(ct)
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
                                    NormalizeReason(reason),
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
                                    NormalizeReason(reason),
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
    /// Applies a terminal status transition for an active run and persists the change.
    /// </summary>
    /// <param name="runId">Run id.</param>
    /// <param name="finalStatus">Final status to set.</param>
    /// <param name="supersededByRunId">Optional superseding run id.</param>
    /// <param name="failureReason">Optional failure/cancel reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the specified run id does not exist.
    /// </exception>
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

        if (supersededByRunId.HasValue)
        {
            run.SupersededByRunId = supersededByRunId.Value;
        }

        run.FailureReason = failureReason;

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    private async Task SetCurrentRunAsync(SyncRequestEntity request, long runId, CancellationToken ct)
    {
        request.CurrentRunId = runId;

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

    private static string NormalizeReason(string? reason)
    {
        if (string.IsNullOrWhiteSpace(reason)) return "Canceled by host/user.";

        string trimmed = reason.Trim();

        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }

    #endregion
}
