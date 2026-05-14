using System.Linq.Expressions;

using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.DTOs.SyncTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.SyncTracking;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Repository implementation for sync run-item tracking and page-level distributed claim operations.
/// </summary>
public sealed class SyncRunItemRepository(AppDbContext dbContext,
                                          IUnitOfWork uow,
                                          ILogger<SyncRunItemRepository> logger) : ISyncRunItemRepository
{
    #region ========== *** Properties Section *** ==========

    private const int MaxClaimRetryCount = 20;
    private const int MaxSeedRetryCount = 3;

    #endregion

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
        string normalizedStep = step.NormalizeStep();
        string normalizedCursor = cursor.NormalizeCursor();
        string? normalizedFailureReason = failureReason.NormalizeFailureReason();

        SyncRunItemEntity? existing = await FindGenericRunItemAsync(runId,
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

        await InsertOrUpdateGenericOnRaceAsync(runId,
                                               normalizedStep,
                                               normalizedCursor,
                                               status,
                                               normalizedFailureReason,
                                               ct)
               .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty or whitespace, or when any page number is less than 1.
    /// </exception>
    public async Task SeedPendingPagesAsync(long runId,
                                            string step,
                                            IReadOnlyCollection<int> pageNumbers,
                                            CancellationToken ct)
    {
        string normalizedStep = step.NormalizeStep();
        IReadOnlyList<int> normalizedPageNumbers = pageNumbers.NormalizeDistinctPageNumbers();

        if (normalizedPageNumbers.Count == 0) return;

        for (int attempt = 1; attempt <= MaxSeedRetryCount; attempt++)
        {
            try
            {
                await InsertMissingPendingPagesAsync(runId,
                                                     normalizedStep,
                                                     normalizedPageNumbers,
                                                     ct)
                       .ConfigureAwait(false);

                return;
            }
            catch (Exception ex) when (UniqueViolationDetector.IsRunItemUniqueViolation(ex)
                                       && attempt < MaxSeedRetryCount)
            {
                dbContext.ChangeTracker.Clear();

                logger.LogWarning(ex,
                                  "Unique conflict while seeding sync run page items. Retrying. RunId = {RunId}, Step = {Step}, PageCount = {PageCount}, Attempt = {Attempt}, MaxAttempts = {MaxAttempts}.",
                                  runId,
                                  normalizedStep,
                                  normalizedPageNumbers.Count,
                                  attempt,
                                  MaxSeedRetryCount);
            }
            catch (Exception ex) when (UniqueViolationDetector.IsRunItemUniqueViolation(ex))
            {
                dbContext.ChangeTracker.Clear();

                logger.LogError(ex,
                                "Unique conflict while seeding sync run page items after all retry attempts. RunId = {RunId}, Step = {Step}, PageCount = {PageCount}, MaxAttempts = {MaxAttempts}.",
                                runId,
                                normalizedStep,
                                normalizedPageNumbers.Count,
                                MaxSeedRetryCount);

                throw;
            }
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> or <paramref name="claimedBy"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="leaseToken"/> is empty, or when
    /// <paramref name="claimExpiresAtEastern"/> is not greater than <paramref name="claimedAtEastern"/>.
    /// </exception>
    public async Task<SyncRunItemDto?> ClaimNextPageAsync(long runId,
                                                          string step,
                                                          string claimedBy,
                                                          Guid leaseToken,
                                                          DateTimeOffset claimedAtEastern,
                                                          DateTimeOffset claimExpiresAtEastern,
                                                          CancellationToken ct)
    {
        string normalizedStep = step.NormalizeStep();
        string normalizedClaimedBy = claimedBy.NormalizeClaimedBy();
        Guid normalizedLeaseToken = leaseToken.NormalizeLeaseToken();

        claimedAtEastern.ValidateClaimWindow(claimExpiresAtEastern);

        for (int attempt = 0; attempt < MaxClaimRetryCount; attempt++)
        {
            SyncRunItemDto? candidate = await BuildEligiblePageClaimQuery(runId, normalizedStep, claimedAtEastern)
                                             .OrderBy(x => x.PageNumber)
                                             .ThenBy(x => x.Id)
                                             .Select(MapToDto())
                                             .FirstOrDefaultAsync(ct)
                                             .ConfigureAwait(false);

            if (candidate is null) return null;

            int affectedRows = await dbContext.Set<SyncRunItemEntity>()
                                              .Where(x => x.Id       == candidate.Id
                                                          && x.RunId == runId
                                                          && x.Step  == normalizedStep
                                                          && x.PageNumber.HasValue
                                                          && (x.Status == SyncRunStatus.Pending
                                                              || x.Status == SyncRunStatus.Running
                                                              && x.ClaimExpiresAtEastern.HasValue
                                                              && x.ClaimExpiresAtEastern.Value <= claimedAtEastern))
                                              .ExecuteUpdateAsync(setters => setters
                                                                            .SetProperty(x => x.Status,
                                                                                 SyncRunStatus.Running)
                                                                            .SetProperty(x => x.FailureReason,
                                                                                 (string?)null)
                                                                            .SetProperty(x => x.ClaimedBy,
                                                                                 normalizedClaimedBy)
                                                                            .SetProperty(x => x.LeaseToken,
                                                                                 normalizedLeaseToken)
                                                                            .SetProperty(x => x.ClaimedAtEastern,
                                                                                 claimedAtEastern)
                                                                            .SetProperty(x => x.ClaimExpiresAtEastern,
                                                                                 claimExpiresAtEastern)
                                                                            .SetProperty(x => x.LastHeartbeatAtEastern,
                                                                                 claimedAtEastern)
                                                                            .SetProperty(x => x.AttemptCount,
                                                                                 x => x.AttemptCount + 1),
                                                                  ct)
                                              .ConfigureAwait(false);

            if (affectedRows == 0) continue;

            return await dbContext.Set<SyncRunItemEntity>()
                                  .AsNoTracking()
                                  .Where(x => x.Id == candidate.Id)
                                  .Select(MapToDto())
                                  .FirstAsync(ct)
                                  .ConfigureAwait(false);
        }

        return null;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimedBy"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="leaseToken"/> is empty, or when
    /// <paramref name="claimExpiresAtEastern"/> is not greater than <paramref name="heartbeatAtEastern"/>.
    /// </exception>
    public async Task<bool> TryHeartbeatAsync(long runItemId,
                                              string claimedBy,
                                              Guid leaseToken,
                                              DateTimeOffset heartbeatAtEastern,
                                              DateTimeOffset claimExpiresAtEastern,
                                              CancellationToken ct)
    {
        string normalizedClaimedBy = claimedBy.NormalizeClaimedBy();
        Guid normalizedLeaseToken = leaseToken.NormalizeLeaseToken();

        heartbeatAtEastern.ValidateHeartbeatWindow(claimExpiresAtEastern);

        int affectedRows = await BuildOwnedRunningPageQuery(runItemId, normalizedClaimedBy, normalizedLeaseToken)
                                .ExecuteUpdateAsync(setters => setters
                                                              .SetProperty(x => x.LastHeartbeatAtEastern,
                                                                           heartbeatAtEastern)
                                                              .SetProperty(x => x.ClaimExpiresAtEastern,
                                                                           claimExpiresAtEastern),
                                                    ct)
                                .ConfigureAwait(false);

        return affectedRows == 1;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimedBy"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="leaseToken"/> is empty.
    /// </exception>
    public async Task<bool> TryMarkCompletedAsync(long runItemId,
                                                  string claimedBy,
                                                  Guid leaseToken,
                                                  CancellationToken ct)
    {
        string normalizedClaimedBy = claimedBy.NormalizeClaimedBy();
        Guid normalizedLeaseToken = leaseToken.NormalizeLeaseToken();

        int affectedRows = await BuildOwnedRunningPageQuery(runItemId, normalizedClaimedBy, normalizedLeaseToken)
                                .ExecuteUpdateAsync(setters => setters
                                                              .SetProperty(x => x.Status, SyncRunStatus.Completed)
                                                              .SetProperty(x => x.FailureReason, (string?)null)
                                                              .SetProperty(x => x.ClaimedBy, (string?)null)
                                                              .SetProperty(x => x.LeaseToken, (Guid?)null)
                                                              .SetProperty(x => x.ClaimedAtEastern,
                                                                           (DateTimeOffset?)null)
                                                              .SetProperty(x => x.ClaimExpiresAtEastern,
                                                                           (DateTimeOffset?)null)
                                                              .SetProperty(x => x.LastHeartbeatAtEastern,
                                                                           (DateTimeOffset?)null),
                                                    ct)
                                .ConfigureAwait(false);

        return affectedRows == 1;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimedBy"/> or <paramref name="failureReason"/> is empty or whitespace.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="leaseToken"/> is empty.
    /// </exception>
    public async Task<bool> TryMarkFailedAsync(long runItemId,
                                               string claimedBy,
                                               Guid leaseToken,
                                               string failureReason,
                                               CancellationToken ct)
    {
        string normalizedClaimedBy = claimedBy.NormalizeClaimedBy();
        Guid normalizedLeaseToken = leaseToken.NormalizeLeaseToken();
        string normalizedFailureReason = failureReason.NormalizeRequiredFailureReason();

        int affectedRows = await BuildOwnedRunningPageQuery(runItemId, normalizedClaimedBy, normalizedLeaseToken)
                                .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.Status, SyncRunStatus.Failed)
                                                                      .SetProperty(x => x.FailureReason,
                                                                           normalizedFailureReason)
                                                                      .SetProperty(x => x.ClaimedBy, (string?)null)
                                                                      .SetProperty(x => x.LeaseToken, (Guid?)null)
                                                                      .SetProperty(x => x.ClaimedAtEastern,
                                                                           (DateTimeOffset?)null)
                                                                      .SetProperty(x => x.ClaimExpiresAtEastern,
                                                                           (DateTimeOffset?)null)
                                                                      .SetProperty(x => x.LastHeartbeatAtEastern,
                                                                           (DateTimeOffset?)null),
                                                    ct)
                                .ConfigureAwait(false);

        return affectedRows == 1;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty or whitespace.
    /// </exception>
    public async Task<SyncRunItemDto?> GetLatestCompletedAsync(long runId, string step, CancellationToken ct)
    {
        string normalizedStep = step.NormalizeStep();

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
                              .Where(x => x.RunId        == runId && x.Status == SyncRunStatus.Failed)
                              .OrderBy(x => x.PageNumber == null ? 1 : 0)
                              .ThenBy(x => x.PageNumber)
                              .ThenByDescending(x => x.AppUpdatedAtEastern)
                              .Select(MapToDto())
                              .ToListAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Gets failed page items for the specified run and page-work step, ordered by page number.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Failed page run-item collection.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty or whitespace.
    /// </exception>
    public async Task<IReadOnlyCollection<SyncRunItemDto>> GetFailedPagesAsync(long runId,
                                                                               string step,
                                                                               CancellationToken ct)
    {
        string normalizedStep = step.NormalizeStep();

        return await dbContext.Set<SyncRunItemEntity>()
                              .AsNoTracking()
                              .Where(x => x.RunId   == runId
                                          && x.Step == normalizedStep
                                          && x.PageNumber.HasValue
                                          && x.Status == SyncRunStatus.Failed)
                              .OrderBy(x => x.PageNumber)
                              .ThenBy(x => x.Id)
                              .Select(MapToDto())
                              .ToListAsync(ct)
                              .ConfigureAwait(false);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty or whitespace.
    /// </exception>
    public async Task<bool> HasUnfinishedPagesAsync(long runId, string step, CancellationToken ct)
    {
        string normalizedStep = step.NormalizeStep();

        return await dbContext.Set<SyncRunItemEntity>()
                              .AsNoTracking()
                              .AnyAsync(x => x.RunId   == runId
                                             && x.Step == normalizedStep
                                             && x.PageNumber.HasValue
                                             && (x.Status    == SyncRunStatus.Pending
                                                 || x.Status == SyncRunStatus.Running),
                                        ct)
                              .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Builds the query for a running page item still owned by the supplied worker lease.
    /// </summary>
    /// <param name="runItemId">Run-item identifier.</param>
    /// <param name="claimedBy">Normalized worker identifier.</param>
    /// <param name="leaseToken">Validated lease ownership token.</param>
    /// <returns>The owned running page-item query.</returns>
    private IQueryable<SyncRunItemEntity> BuildOwnedRunningPageQuery(long runItemId, string claimedBy, Guid leaseToken)
    {
        return dbContext.Set<SyncRunItemEntity>()
                        .Where(x => x.Id == runItemId
                                    && x.PageNumber.HasValue
                                    && x.Status     == SyncRunStatus.Running
                                    && x.ClaimedBy  == claimedBy
                                    && x.LeaseToken == leaseToken);
    }

    /// <summary>
    /// Builds the query used to locate the next claim-eligible page item.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical page-work step name.</param>
    /// <param name="claimedAtEastern">Eastern application timestamp used to evaluate expired leases.</param>
    /// <returns>The claim-eligible page query.</returns>
    private IQueryable<SyncRunItemEntity> BuildEligiblePageClaimQuery(long runId,
                                                                      string step,
                                                                      DateTimeOffset claimedAtEastern)
    {
        return dbContext.Set<SyncRunItemEntity>()
                        .AsNoTracking()
                        .Where(x => x.RunId   == runId
                                    && x.Step == step
                                    && x.PageNumber.HasValue
                                    && (x.Status == SyncRunStatus.Pending
                                        || x.Status == SyncRunStatus.Running
                                        && x.ClaimExpiresAtEastern.HasValue
                                        && x.ClaimExpiresAtEastern.Value <= claimedAtEastern));
    }

    /// <summary>
    /// Finds one generic run item by its natural key: <c>(runId, step, cursor)</c>.
    /// Only non-page items are considered by this lookup.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="cursor">Generic selector token.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The matching generic run item when found; otherwise null.</returns>
    private async Task<SyncRunItemEntity?> FindGenericRunItemAsync(long runId,
                                                                   string step,
                                                                   string cursor,
                                                                   CancellationToken ct)
    {
        return await dbContext.Set<SyncRunItemEntity>()
                              .FirstOrDefaultAsync(x => x.RunId         == runId
                                                        && x.Step       == step
                                                        && x.PageNumber == null
                                                        && x.Cursor     == cursor,
                                                   ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Inserts a new generic run item and resolves duplicate-key races by reloading and updating the winning row.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Logical stage or item name.</param>
    /// <param name="cursor">Generic selector token.</param>
    /// <param name="status">Target run-item status.</param>
    /// <param name="failureReason">Optional failure reason.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the row cannot be reloaded after a unique conflict.
    /// </exception>
    private async Task InsertOrUpdateGenericOnRaceAsync(long runId,
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
                                       PageNumber = null,
                                       Status = status,
                                       FailureReason = failureReason,
                                       ClaimedBy = null,
                                       LeaseToken = null,
                                       ClaimedAtEastern = null,
                                       ClaimExpiresAtEastern = null,
                                       AttemptCount = 0,
                                       LastHeartbeatAtEastern = null
                                   };

        await uow.UpsertAsync(entity, ct: ct)
                 .ConfigureAwait(false);

        try
        {
            await uow.SaveChangesAsync(ct)
                     .ConfigureAwait(false);
        }
        catch (Exception ex) when (UniqueViolationDetector.IsRunItemUniqueViolation(ex))
        {
            dbContext.Entry(entity)
                     .State = EntityState.Detached;

            SyncRunItemEntity? winner = await FindGenericRunItemAsync(runId,
                                                                      step,
                                                                      cursor,
                                                                      ct)
                                               .ConfigureAwait(false);

            if (winner is null)
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

    /// <summary>
    /// Inserts pending page items that do not already exist for the supplied run and step.
    /// </summary>
    /// <param name="runId">Physical run id.</param>
    /// <param name="step">Normalized logical page-work step name.</param>
    /// <param name="pageNumbers">Normalized one-based page numbers to seed.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task InsertMissingPendingPagesAsync(long runId,
                                                      string step,
                                                      IReadOnlyCollection<int> pageNumbers,
                                                      CancellationToken ct)
    {
        List<int> existingPageNumbers = await dbContext.Set<SyncRunItemEntity>()
                                                       .AsNoTracking()
                                                       .Where(x => x.RunId   == runId
                                                                   && x.Step == step
                                                                   && x.PageNumber.HasValue
                                                                   && pageNumbers.Contains(x.PageNumber.Value))
                                                       .Select(x => x.PageNumber!.Value)
                                                       .ToListAsync(ct)
                                                       .ConfigureAwait(false);

        HashSet<int> existingPageNumberSet = existingPageNumbers.ToHashSet();

        List<SyncRunItemEntity> pendingPageItems =
                pageNumbers.Where(pageNumber => !existingPageNumberSet.Contains(pageNumber))
                           .Select(pageNumber => new SyncRunItemEntity
                                                 {
                                                     RunId = runId,
                                                     Step = step,
                                                     Cursor = null,
                                                     PageNumber = pageNumber,
                                                     Status = SyncRunStatus.Pending,
                                                     FailureReason = null,
                                                     ClaimedBy = null,
                                                     LeaseToken = null,
                                                     ClaimedAtEastern = null,
                                                     ClaimExpiresAtEastern = null,
                                                     AttemptCount = 0,
                                                     LastHeartbeatAtEastern = null
                                                 })
                           .ToList();

        if (pendingPageItems.Count == 0) return;

        await dbContext.Set<SyncRunItemEntity>()
                       .AddRangeAsync(pendingPageItems, ct)
                       .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    /// <summary>
    /// Maps a run-item entity to its application DTO projection.
    /// </summary>
    /// <returns>The entity-to-DTO projection expression.</returns>
    private static Expression<Func<SyncRunItemEntity, SyncRunItemDto>> MapToDto()
    {
        return x => new SyncRunItemDto
                    {
                        Id = x.Id,
                        RunId = x.RunId,
                        Step = x.Step,
                        Cursor = x.Cursor,
                        PageNumber = x.PageNumber,
                        Status = x.Status,
                        FailureReason = x.FailureReason,
                        ClaimedBy = x.ClaimedBy,
                        LeaseToken = x.LeaseToken,
                        ClaimedAtEastern = x.ClaimedAtEastern,
                        ClaimExpiresAtEastern = x.ClaimExpiresAtEastern,
                        AttemptCount = x.AttemptCount,
                        LastHeartbeatAtEastern = x.LastHeartbeatAtEastern
                    };
    }

    #endregion
}
