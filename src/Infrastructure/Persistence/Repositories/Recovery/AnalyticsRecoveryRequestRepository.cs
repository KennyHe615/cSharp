using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.Recovery;
using Application.DTOs.Recovery;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.Recovery;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Sync;


namespace Infrastructure.Persistence.Repositories.Recovery;

/// <summary>
/// Repository implementation for user-submitted analytics recovery intake requests.
/// </summary>
public sealed class AnalyticsRecoveryRequestRepository(AppDbContext dbContext,
                                                       IUnitOfWork uow) : IAnalyticsRecoveryRequestRepository
{
    /// <inheritdoc />
    public async Task<AnalyticsRecoveryRequestResolveResult> CreateOrGetActiveAsync(string category,
        string? interval,
        string? genesysJobId,
        CancellationToken ct)
    {
        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       AnalyticsRecoveryRequestEntity.ScopeMode,
                                                       interval,
                                                       null,
                                                       genesysJobId);

        AnalyticsRecoveryRequestEntity? existing = await FindActiveByScopeKeyAsync(scopeKey, ct)
                                                          .ConfigureAwait(false);

        if (existing is not null)
        {
            return ToResolveResult(existing, AnalyticsRecoveryRequestResolveAction.ReusedActive);
        }

        AnalyticsRecoveryRequestEntity entity = BuildNewEntity(category, interval, genesysJobId);

        await uow.UpsertAsync(entity, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return ToResolveResult(entity, AnalyticsRecoveryRequestResolveAction.Created);
        }
        catch (Exception ex) when (AnalyticsRecoveryRequestUniqueViolationDetector.IsActiveScopeUniqueViolation(ex))
        {
            dbContext.ChangeTracker.Clear();

            AnalyticsRecoveryRequestEntity winner = await GetActiveByScopeKeyOrThrowAsync(scopeKey, ct)
                                                           .ConfigureAwait(false);

            return ToResolveResult(winner, AnalyticsRecoveryRequestResolveAction.ReusedActive);
        }
    }

    /// <inheritdoc />
    public async Task<AnalyticsRecoveryRequestDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        return await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                              .AsNoTracking()
                              .Where(x => x.Id == id)
                              .Select(x => new AnalyticsRecoveryRequestDto
                                           {
                                               Id = x.Id,
                                               PublicId = x.PublicId,
                                               Category = x.Category,
                                               Status = x.Status,
                                               Interval = x.Interval,
                                               GenesysJobId = x.GenesysJobId,
                                               FailureReason = x.FailureReason
                                           })
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private async Task<AnalyticsRecoveryRequestEntity?> FindActiveByScopeKeyAsync(string scopeKey, CancellationToken ct)
    {
        return await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                              .AsNoTracking()
                              .Where(x => x.ScopeKey == scopeKey
                                          && (x.Status    == AnalyticsRecoveryRequestStatus.Pending
                                              || x.Status == AnalyticsRecoveryRequestStatus.Running))
                              .OrderByDescending(x => x.AppUpdatedAtEastern)
                              .ThenByDescending(x => x.Id)
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    private async Task<AnalyticsRecoveryRequestEntity> GetActiveByScopeKeyOrThrowAsync(string scopeKey,
        CancellationToken ct)
    {
        return await dbContext.Set<AnalyticsRecoveryRequestEntity>()
                              .AsNoTracking()
                              .Where(x => x.ScopeKey == scopeKey
                                          && (x.Status    == AnalyticsRecoveryRequestStatus.Pending
                                              || x.Status == AnalyticsRecoveryRequestStatus.Running))
                              .OrderByDescending(x => x.AppUpdatedAtEastern)
                              .ThenByDescending(x => x.Id)
                              .FirstAsync(ct)
                              .ConfigureAwait(false);
    }

    private static AnalyticsRecoveryRequestEntity BuildNewEntity(string category,
                                                                 string? interval,
                                                                 string? genesysJobId)
    {
        AnalyticsRecoveryRequestEntity entity = new AnalyticsRecoveryRequestEntity
                                                {
                                                    Category = category,
                                                    Status = AnalyticsRecoveryRequestStatus.Pending,
                                                    Interval = interval,
                                                    GenesysJobId = genesysJobId
                                                };

        entity.RebuildScopeKey();

        return entity;
    }

    private static AnalyticsRecoveryRequestResolveResult ToResolveResult(AnalyticsRecoveryRequestEntity entity,
                                                                         AnalyticsRecoveryRequestResolveAction action)
    {
        return new AnalyticsRecoveryRequestResolveResult
               {
                   Id = entity.Id,
                   PublicId = entity.PublicId,
                   RequestAction = action
               };
    }

    #endregion
}
