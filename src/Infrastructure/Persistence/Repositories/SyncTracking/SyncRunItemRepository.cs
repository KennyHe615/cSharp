using System.Linq.Expressions;

using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for sync run-item upsert and query operations.
/// </summary>
public sealed class SyncRunItemRepository(AppDbContext dbContext,
                                          IUnitOfWork uow) : ISyncRunItemRepository
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> or <paramref name="cursor"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a unique-conflict fallback cannot reload the winning run-item row.
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

        SyncRunItemEntity? existing = await FindRunItemAsync(runId,
                                                             normalizedStep,
                                                             normalizedCursor,
                                                             ct)
                                                     .ConfigureAwait(false);

        if (existing is not null)
        {
            existing.Status = status;
            existing.FailureReason = normalizedFailureReason;

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
    public async Task<SyncRunItemDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct)
    {
        string normalizedStep = NormalizeStep(step);

        return await dbContext.Set<SyncRunItemEntity>()
                              .AsNoTracking()
                              .Where(x => x.RunId   == runId
                                          && x.Step == normalizedStep
                                          && (x.Status    == SyncRunStatus.Completed
                                              || x.Status == SyncRunStatus.CompletedWithRecoveryItems))
                              .OrderByDescending(x => x.AppUpdatedAtEastern)
                              .Select(MapToDto())
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SyncRunItemDto>> GetFailedAsync(long runId, CancellationToken ct)
    {
        return await dbContext.Set<SyncRunItemEntity>()
                              .AsNoTracking()
                              .Where(x => x.RunId == runId && x.Status == SyncRunStatus.Failed)
                              .OrderByDescending(x => x.AppUpdatedAtEastern)
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

    /// <summary>
    /// Finds one run item by its natural key: (runId, step, cursor).
    /// </summary>
    private async Task<SyncRunItemEntity?> FindRunItemAsync(long runId,
                                                            string step,
                                                            string cursor,
                                                            CancellationToken ct)
    {
        return await dbContext.Set<SyncRunItemEntity>()
                              .FirstOrDefaultAsync(x => x.RunId == runId && x.Step == step && x.Cursor == cursor, ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts a new run item and resolves duplicate-key races by reloading and updating the winning row.
    /// </summary>
    private async Task InsertOrUpdateOnRaceAsync(long runId,
                                                 string step,
                                                 string cursor,
                                                 SyncRunStatus status,
                                                 string? failureReason,
                                                 CancellationToken ct)
    {
        SyncRunItemEntity entity = new SyncRunItemEntity
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
        catch (DbUpdateException ex) when (UniqueViolationDetector.IsRunItemUniqueViolation(ex))
        {
            dbContext.Entry(entity)
                     .State = EntityState.Detached;

            SyncRunItemEntity? winner = await FindRunItemAsync(runId,
                                                               step,
                                                               cursor,
                                                               ct)
                                                       .ConfigureAwait(false);

            if (winner == null)
            {
                throw new
                                InvalidOperationException($"Sync run item (runId={runId}, step='{step}', cursor='{cursor}') was not found after unique conflict.");
            }

            winner.Status = status;
            winner.FailureReason = failureReason;

            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);
        }
    }

    private static Expression<Func<SyncRunItemEntity, SyncRunItemDto>> MapToDto()
    {
        return x => new SyncRunItemDto
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
