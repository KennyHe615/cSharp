using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Recovery;
using Application.DTOs.Recovery;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.Recovery;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;


namespace Infrastructure.Persistence.Repositories.Recovery;

/// <summary>
/// Repository implementation for scheduled recovery intake materialization work.
/// </summary>
public sealed class RecoveryIntakeWorkRepository(AppDbContext dbContext) : IRecoveryIntakeWorkRepository
{
    /// <inheritdoc />
    public async Task<AnalyticsRecoveryRequestDto?> TryStartNextPendingAsync(string? category, CancellationToken ct)
    {
        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync(ct)
                                                                       .ConfigureAwait(false);

        IQueryable<AnalyticsRecoveryRequestEntity> query = dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                    .Where(x => x.Status
                                                                               == AnalyticsRecoveryRequestStatus
                                                                                      .Pending);

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(x => x.Category == category);
        }

        AnalyticsRecoveryRequestEntity? entity = await query.OrderBy(x => x.AppCreatedAtEastern)
                                                            .ThenBy(x => x.Id)
                                                            .FirstOrDefaultAsync(ct)
                                                            .ConfigureAwait(false);

        if (entity is null)
        {
            await transaction.RollbackAsync(ct)
                             .ConfigureAwait(false);

            return null;
        }

        entity.Status = AnalyticsRecoveryRequestStatus.Running;
        entity.FailureReason = null;

        await dbContext.SaveChangesAsync(ct)
                       .ConfigureAwait(false);

        await transaction.CommitAsync(ct)
                         .ConfigureAwait(false);

        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkCompletedAsync(long id, CancellationToken ct)
    {
        AnalyticsRecoveryRequestEntity? entity = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .FirstOrDefaultAsync(x => x.Id == id
                                                                         && x.Status
                                                                         == AnalyticsRecoveryRequestStatus
                                                                                .Running,
                                                                     ct)
                                                                .ConfigureAwait(false);

        if (entity is null) return false;

        entity.Status = AnalyticsRecoveryRequestStatus.Completed;
        entity.FailureReason = null;

        await dbContext.SaveChangesAsync(ct)
                       .ConfigureAwait(false);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> TryMarkFailedAsync(long id, string failureReason, CancellationToken ct)
    {
        AnalyticsRecoveryRequestEntity? entity = await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                                                                .FirstOrDefaultAsync(x => x.Id == id
                                                                         && x.Status
                                                                         == AnalyticsRecoveryRequestStatus
                                                                                .Running,
                                                                     ct)
                                                                .ConfigureAwait(false);

        if (entity is null) return false;

        entity.Status = AnalyticsRecoveryRequestStatus.Failed;
        entity.FailureReason = failureReason;

        await dbContext.SaveChangesAsync(ct)
                       .ConfigureAwait(false);

        return true;
    }

    #region ========== *** Private Section *** ==========

    private static AnalyticsRecoveryRequestDto ToDto(AnalyticsRecoveryRequestEntity entity)
    {
        return new AnalyticsRecoveryRequestDto
               {
                   Id = entity.Id,
                   PublicId = entity.PublicId,
                   Category = entity.Category,
                   Status = entity.Status,
                   Interval = entity.Interval,
                   GenesysJobId = entity.GenesysJobId,
                   FailureReason = entity.FailureReason
               };
    }

    #endregion
}
