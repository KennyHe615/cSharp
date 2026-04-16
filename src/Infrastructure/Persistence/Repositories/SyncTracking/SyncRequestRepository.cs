using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Sync;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for sync request persistence and scope deduplication.
/// </summary>
public sealed class SyncRequestRepository(AppDbContext dbContext,
                                          IUnitOfWork uow) : ISyncRequestRepository
{
    /// <inheritdoc />
    public async Task<long> CreateOrGetByScopeAsync(string category,
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

        SyncRequestEntity? existing = await FindByScopeKeyAsync(scopeKey, ct)
                                         .ConfigureAwait(false);

        if (existing is not null) return existing.Id;

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

            return entity.Id;
        }
        catch (DbUpdateException ex) when (UniqueViolationDetector.IsScopeKeyUniqueViolation(ex))
        {
            // Scale-out race: another instance inserted same scope key first.
            SyncRequestEntity winner = await GetByScopeKeyOrThrowAsync(scopeKey, ct)
                                          .ConfigureAwait(false);

            return winner.Id;
        }
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
                                               Category = x.Category,
                                               Mode = x.Mode,
                                               Interval = x.Interval,
                                               PageNumber = x.PageNumber,
                                               GenesysJobId = x.GenesysJobId,
                                               ScopeKey = x.ScopeKey,
                                               CurrentRunId = x.CurrentRunId
                                           })
                              .FirstOrDefaultAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct)
    {
        SyncRequestEntity request = await GetRequestOrThrowAsync(requestId, ct)
                                       .ConfigureAwait(false);

        await EnsureRunBelongsToRequestAsync(runId, requestId, ct)
           .ConfigureAwait(false);

        request.CurrentRunId = runId;

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Finds a sync request by scope key.
    /// </summary>
    /// <param name="scopeKey">Normalized scope key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="SyncRequestEntity"/> when found; otherwise <c>null</c>.</returns>
    private async Task<SyncRequestEntity?> FindByScopeKeyAsync(string scopeKey, CancellationToken ct)
    {
        return await dbContext.Set<SyncRequestEntity>()
                              .AsNoTracking()
                              .FirstOrDefaultAsync(x => x.ScopeKey == scopeKey, ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds a new sync request entity from scope selectors and rebuilds its persisted scope key.
    /// </summary>
    /// <param name="category">Sync category.</param>
    /// <param name="mode">Sync mode.</param>
    /// <param name="interval">Optional interval selector.</param>
    /// <param name="pageNumber">Optional page selector.</param>
    /// <param name="genesysJobId">Optional Genesys job selector.</param>
    /// <returns>Initialized <see cref="SyncRequestEntity"/> with scope key rebuilt.</returns>
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
                                       Interval = interval,
                                       PageNumber = pageNumber,
                                       GenesysJobId = genesysJobId
                                   };

        entity.RebuildScopeKey();

        return entity;
    }

    /// <summary>
    /// Gets a sync request by scope key.
    /// </summary>
    /// <param name="scopeKey">Normalized scope key.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching <see cref="SyncRequestEntity"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the request cannot be found for the provided scope key.
    /// </exception>
    private async Task<SyncRequestEntity> GetByScopeKeyOrThrowAsync(string scopeKey, CancellationToken ct)
    {
        return await dbContext.Set<SyncRequestEntity>()
                              .AsNoTracking()
                              .FirstAsync(x => x.ScopeKey == scopeKey, ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets a sync request by id.
    /// </summary>
    /// <param name="requestId">Sync request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching  <see cref="SyncRequestEntity"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the sync request does not exist.
    /// </exception>
    private async Task<SyncRequestEntity> GetRequestOrThrowAsync(long requestId, CancellationToken ct)
    {
        SyncRequestEntity? request = await dbContext.Set<SyncRequestEntity>()
                                                    .FirstOrDefaultAsync(x => x.Id == requestId, ct)
                                                    .ConfigureAwait(false);

        return request ?? throw new InvalidOperationException($"Sync request '{requestId}' was not found.");
    }

    /// <summary>
    /// Validates that the run exists and belongs to the provided request.
    /// </summary>
    /// <param name="runId">Run id to validate.</param>
    /// <param name="requestId">Expected owner request id.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the run does not exist or belongs to a different request.
    /// </exception>
    private async Task EnsureRunBelongsToRequestAsync(long runId, long requestId, CancellationToken ct)
    {
        long? runOwnerRequestId = await dbContext.Set<SyncRunEntity>()
                                                 .AsNoTracking()
                                                 .Where(x => x.Id == runId)
                                                 .Select(x => (long?)x.RequestId)
                                                 .FirstOrDefaultAsync(ct)
                                                 .ConfigureAwait(false);

        if (!runOwnerRequestId.HasValue)
        {
            throw new InvalidOperationException($"Sync run '{runId}' was not found.");
        }

        if (runOwnerRequestId.Value != requestId)
        {
            throw new InvalidOperationException($"Sync run '{runId}' does not belong to sync request '{requestId}'.");
        }
    }

    #endregion
}
