using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Sync;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for sync request persistence and scope resolution.
/// </summary>
public sealed class SyncRequestRepository(AppDbContext dbContext,
                                          IUnitOfWork uow) : ISyncRequestRepository
{
    /// <inheritdoc />
    public async Task<SyncRequestResolveResult> CreateOrGetByScopeAsync(string category,
                                                                        SyncMode mode,
                                                                        string? interval,
                                                                        int? pageNumber,
                                                                        string? genesysJobId,
                                                                        CancellationToken ct)
    {
        string scopeKey = SyncScopeKeyFormatter.Format(category,
                                                       mode.ToString(),
                                                       interval,
                                                       pageNumber,
                                                       genesysJobId);

        return mode switch
               {
                   SyncMode.Incremental => await ResolveIncrementalAsync(category,
                                                                         mode,
                                                                         interval,
                                                                         pageNumber,
                                                                         genesysJobId,
                                                                         scopeKey,
                                                                         ct)
                                                          .ConfigureAwait(false),

                   SyncMode.Recovery => await ResolveRecoveryAsync(category,
                                                                   mode,
                                                                   interval,
                                                                   pageNumber,
                                                                   genesysJobId,
                                                                   scopeKey,
                                                                   ct)
                                                       .ConfigureAwait(false),

                   _ => throw new InvalidOperationException($"Unsupported sync mode '{mode}'.")
               };
    }

    /// <inheritdoc />
    public async Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        return await dbContext.Set<SyncRequestEntity>()
                              .AsNoTracking()
                              .Where(x => x.Id == id)
                              .Select(x => new SyncRequestDto
                                           {
                                               Id = x.Id,
                                               PublicId = x.PublicId,
                                               Category = x.Category,
                                               Mode = x.Mode,
                                               Status = x.Status,
                                               ReopenCount = x.ReopenCount,
                                               Interval = x.Interval,
                                               PageNumber = x.PageNumber,
                                               GenesysJobId = x.GenesysJobId,
                                               ScopeKey = x.ScopeKey,
                                               CurrentRunId = x.CurrentRunId
                                           })
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Incremental semantics: one logical request row per scope.
    /// Existing scope is reused; otherwise a new row is created.
    /// </summary>
    private async Task<SyncRequestResolveResult> ResolveIncrementalAsync(string category,
                                                                         SyncMode mode,
                                                                         string? interval,
                                                                         int? pageNumber,
                                                                         string? genesysJobId,
                                                                         string scopeKey,
                                                                         CancellationToken ct)
    {
        SyncRequestEntity? existing = await FindByModeAndScopeKeyAsync(mode,
                                                                       scopeKey,
                                                                       true,
                                                                       ct)
                                                     .ConfigureAwait(false);

        if (existing is not null)
        {
            return ToResolveResult(existing, SyncRequestResolveAction.ReusedActive);
        }

        SyncRequestEntity entity = BuildNewEntity(category,
                                                  mode,
                                                  interval,
                                                  pageNumber,
                                                  genesysJobId);

        await uow.UpsertAsync(entity, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return ToResolveResult(entity, SyncRequestResolveAction.Created);
        }
        catch (Exception ex) when (UniqueViolationDetector.IsScopeKeyUniqueViolation(ex))
        {
            dbContext.ChangeTracker.Clear();

            // Scale-out race: another instance inserted same incremental scope first.
            SyncRequestEntity winner = await GetByModeAndScopeKeyOrThrowAsync(mode, scopeKey, ct)
                                                      .ConfigureAwait(false);

            return ToResolveResult(winner, SyncRequestResolveAction.ReusedActive);
        }
    }

    /// <summary>
    /// Recovery semantics (latest-row rule):
    /// - latest active => reuse active
    /// - latest failed/canceled => reopen same row
    /// - latest completed (or missing) => create new row
    /// </summary>
    private async Task<SyncRequestResolveResult> ResolveRecoveryAsync(string category,
                                                                      SyncMode mode,
                                                                      string? interval,
                                                                      int? pageNumber,
                                                                      string? genesysJobId,
                                                                      string scopeKey,
                                                                      CancellationToken ct)
    {
        SyncRequestEntity? latest = await FindLatestRecoveryByScopeKeyAsync(scopeKey, false, ct)
                                                   .ConfigureAwait(false);

        if (latest is not null)
        {
            switch (latest.Status)
            {
                case SyncRequestStatus.Pending or SyncRequestStatus.Running:
                    return ToResolveResult(latest, SyncRequestResolveAction.ReusedActive);

                case SyncRequestStatus.Failed or SyncRequestStatus.Canceled:
                    latest.Status = SyncRequestStatus.Pending;
                    latest.CurrentRunId = null;
                    latest.ReopenCount += 1;

                    try
                    {
                        await uow.SaveChangesAsync(ct)
                                 .ConfigureAwait(false);

                        return ToResolveResult(latest, SyncRequestResolveAction.ReusedFailed);
                    }
                    catch (Exception ex) when (UniqueViolationDetector.IsScopeKeyUniqueViolation(ex))
                    {
                        return await GetActiveRecoveryRaceWinnerAsync(scopeKey, ct)
                                              .ConfigureAwait(false);
                    }
            }

            // Important: latest COMPLETED is treated as explicit rerun intent and creates a new row.
        }

        SyncRequestEntity entity = BuildNewEntity(category,
                                                  mode,
                                                  interval,
                                                  pageNumber,
                                                  genesysJobId);

        await uow.UpsertAsync(entity, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);

            return ToResolveResult(entity, SyncRequestResolveAction.Created);
        }
        catch (Exception ex) when (UniqueViolationDetector.IsScopeKeyUniqueViolation(ex))
        {
            return await GetActiveRecoveryRaceWinnerAsync(scopeKey, ct)
                                  .ConfigureAwait(false);
        }
    }

    private async Task<SyncRequestEntity?> FindByModeAndScopeKeyAsync(SyncMode mode,
                                                                      string scopeKey,
                                                                      bool asNoTracking,
                                                                      CancellationToken ct)
    {
        IQueryable<SyncRequestEntity> query = dbContext.Set<SyncRequestEntity>();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Mode == mode && x.ScopeKey == scopeKey, ct)
                          .ConfigureAwait(false);
    }

    private async Task<SyncRequestEntity> GetByModeAndScopeKeyOrThrowAsync(SyncMode mode,
                                                                           string scopeKey,
                                                                           CancellationToken ct)
    {
        return await dbContext.Set<SyncRequestEntity>()
                              .AsNoTracking()
                              .FirstAsync(x => x.Mode == mode && x.ScopeKey == scopeKey, ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Used only after recovery unique-index race on active scope creation.
    /// </summary>
    private async Task<SyncRequestEntity> GetActiveRecoveryByScopeKeyOrThrowAsync(string scopeKey, CancellationToken ct)
    {
        return await dbContext.Set<SyncRequestEntity>()
                              .AsNoTracking()
                              .Where(x => x.Mode        == SyncMode.Recovery
                                          && x.ScopeKey == scopeKey
                                          && (x.Status    == SyncRequestStatus.Pending
                                              || x.Status == SyncRequestStatus.Running))
                              .OrderByDescending(x => x.AppUpdatedAt)
                              .ThenByDescending(x => x.Id)
                              .FirstAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Returns latest recovery request row for one scope.
    /// Decision logic is based on this latest row's status.
    /// </summary>
    private async Task<SyncRequestEntity?> FindLatestRecoveryByScopeKeyAsync(string scopeKey,
                                                                             bool asNoTracking,
                                                                             CancellationToken ct)
    {
        IQueryable<SyncRequestEntity> query = dbContext.Set<SyncRequestEntity>();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.Where(x => x.Mode == SyncMode.Recovery && x.ScopeKey == scopeKey)
                          .OrderByDescending(x => x.AppUpdatedAt)
                          .ThenByDescending(x => x.Id)
                          .FirstOrDefaultAsync(ct)
                          .ConfigureAwait(false);
    }

    private static SyncRequestEntity BuildNewEntity(string category,
                                                    SyncMode mode,
                                                    string? interval,
                                                    int? pageNumber,
                                                    string? genesysJobId)
    {
        SyncRequestEntity entity = new SyncRequestEntity
                                   {
                                       Category = category,
                                       Mode = mode,
                                       Status = SyncRequestStatus.Pending,
                                       ReopenCount = 0,
                                       Interval = interval,
                                       PageNumber = pageNumber,
                                       GenesysJobId = genesysJobId
                                   };

        entity.RebuildScopeKey();

        return entity;
    }

    private static SyncRequestResolveResult ToResolveResult(SyncRequestEntity entity, SyncRequestResolveAction action)
    {
        return new SyncRequestResolveResult
               {
                   Id = entity.Id,
                   PublicId = entity.PublicId,
                   RequestAction = action
               };
    }

    private async Task<SyncRequestResolveResult> GetActiveRecoveryRaceWinnerAsync(string scopeKey, CancellationToken ct)
    {
        dbContext.ChangeTracker.Clear();

        // Scale-out race: another instance created active recovery scope first.
        SyncRequestEntity winner = await GetActiveRecoveryByScopeKeyOrThrowAsync(scopeKey, ct)
                                                  .ConfigureAwait(false);

        return ToResolveResult(winner, SyncRequestResolveAction.ReusedActive);
    }

    #endregion
}
