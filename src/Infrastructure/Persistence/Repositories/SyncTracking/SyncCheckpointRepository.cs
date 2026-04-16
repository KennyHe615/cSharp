using System.Linq.Expressions;

using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for checkpoint upsert/query operations.
/// </summary>
public sealed class SyncCheckpointRepository(AppDbContext dbContext,
                                             IUnitOfWork uow) : ISyncCheckpointRepository
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> or <paramref name="cursor"/> is empty/whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a unique-conflict fallback cannot reload the winning checkpoint row.
    /// </exception>
    public async Task UpsertAsync(long runId,
                                  string step,
                                  string cursor,
                                  SyncRunStatus status,
                                  string? failureReason,
                                  CancellationToken ct)
    {
        (string normalizedStep, string normalizedCursor, string? normalizedFailureReason) =
            NormalizeUpsertInputsOrThrow(step, cursor, failureReason);

        SyncCheckpointEntity? existing = await FindCheckpointAsync(runId,
                                                                   normalizedStep,
                                                                   normalizedCursor,
                                                                   ct)
                                            .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Status = status;
            existing.FailureReason = normalizedFailureReason;// use normalized value on update too

            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return;
        }

        await InsertOrUpdateOnRaceAsync(runId,
                                        normalizedStep,
                                        normalizedCursor,
                                        status,
                                        normalizedFailureReason,
                                        ct)
           .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty/whitespace.
    /// </exception>
    public async Task<SyncCheckpointDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct)
    {
        string normalizedStep = NormalizeStep(step);

        return await dbContext.Set<SyncCheckpointEntity>()
                              .AsNoTracking()
                              .Where(x => x.RunId     == runId
                                          && x.Step   == normalizedStep
                                          && x.Status == SyncRunStatus.Completed)
                              .OrderByDescending(x => x.AppUpdatedAt)
                              .Select(MapToDto())
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SyncCheckpointDto>> GetFailedAsync(long runId, CancellationToken ct)
    {
        return await dbContext.Set<SyncCheckpointEntity>()
                              .AsNoTracking()
                              .Where(x => x.RunId == runId && x.Status == SyncRunStatus.Failed)
                              .OrderByDescending(x => x.AppUpdatedAt)
                              .Select(MapToDto())
                              .ToListAsync(ct)
                              .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private static (string Step, string Cursor, string? FailureReason) NormalizeUpsertInputsOrThrow(string step,
        string cursor,
        string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            throw new ArgumentException("Cursor is required.", nameof(cursor));
        }

        string normalizedStep = NormalizeStep(step);
        string normalizedCursor = cursor.Trim();
        string? normalizedFailureReason = NormalizeFailureReason(failureReason);

        return (normalizedStep, normalizedCursor, normalizedFailureReason);
    }

    private static string NormalizeStep(string step)
    {
        return string.IsNullOrWhiteSpace(step)
                   ? throw new ArgumentException("Step is required.", nameof(step))
                   : step.Trim();
    }

    private static string? NormalizeFailureReason(string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason)) return null;

        string trimmed = failureReason.Trim();

        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }

    private async Task<SyncCheckpointEntity?> FindCheckpointAsync(long runId,
                                                                  string step,
                                                                  string cursor,
                                                                  CancellationToken ct)
    {
        return await dbContext.Set<SyncCheckpointEntity>()
                              .FirstOrDefaultAsync(x => x.RunId == runId && x.Step == step && x.Cursor == cursor, ct)
                              .ConfigureAwait(false);
    }

    private async Task InsertOrUpdateOnRaceAsync(long runId,
                                                 string step,
                                                 string cursor,
                                                 SyncRunStatus status,
                                                 string? failureReason,
                                                 CancellationToken ct)
    {
        SyncCheckpointEntity entity = new SyncCheckpointEntity
                                      {
                                          RunId = runId,
                                          Step = step,
                                          Cursor = cursor,
                                          Status = status,
                                          FailureReason = failureReason
                                      };

        await uow.UpsertAsync(entity, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);
        }
        catch (DbUpdateException ex) when (UniqueViolationDetector.IsCheckpointUniqueViolation(ex))
        {
            dbContext.Entry(entity)
                     .State = EntityState.Detached;

            SyncCheckpointEntity? winner = await FindCheckpointAsync(runId,
                                                                     step,
                                                                     cursor,
                                                                     ct)
                                              .ConfigureAwait(false);

            if (winner == null)
            {
                throw new
                    InvalidOperationException($"Sync checkpoint (runId={runId}, step='{step}', cursor='{cursor}') was not found after unique conflict.");
            }

            winner.Status = status;
            winner.FailureReason = failureReason;

            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);
        }
    }

    private static Expression<Func<SyncCheckpointEntity, SyncCheckpointDto>> MapToDto()
    {
        return x => new SyncCheckpointDto
                    {
                        Id = x.Id,
                        RunId = x.RunId,
                        Step = x.Step,
                        Cursor = x.Cursor,
                        Status = x.Status,
                        FailureReason = x.FailureReason
                    };
    }

    #endregion
}
