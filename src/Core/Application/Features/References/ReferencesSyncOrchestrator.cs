using Application.Abstractions.External;
using Application.Abstractions.Normalization;
using Application.Abstractions.Orchestration.References;
using Application.Abstractions.Persistence;
using Application.Abstractions.Persistence.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking;


namespace Application.Features.References;

/// <summary>
/// References domain executor for category-level full-sync operations.
/// Fetches provider payloads, normalizes contracts, persists reference tables, and records sync run items.
/// </summary>
public sealed class ReferencesSyncOrchestrator(IReferenceApiClient referenceApiClient,
                                               IReferencesNormalizer referencesNormalizer,
                                               IReferencesRepository referencesRepository,
                                               ISyncRunItemRepository syncRunItemRepository)
        : IReferencesSyncOrchestrator
{
    /// <inheritdoc />
    public Task ExecuteAsync(long runId, SyncReferenceCategory category, CancellationToken ct = default)
    {
        return category switch
               {
                   SyncReferenceCategory.Group => ExecuteCategoryAsync(runId,
                                                                       SyncReferenceCategory.Group,
                                                                       referenceApiClient.GetGroupsAsync,
                                                                       referencesNormalizer.NormalizeGroups,
                                                                       referencesRepository.UpsertGroupsAsync,
                                                                       ct),

                   SyncReferenceCategory.Skill => ExecuteCategoryAsync(runId,
                                                                       SyncReferenceCategory.Skill,
                                                                       referenceApiClient.GetSkillsAsync,
                                                                       referencesNormalizer.NormalizeSkills,
                                                                       referencesRepository.UpsertSkillsAsync,
                                                                       ct),

                   SyncReferenceCategory.PresenceDefinition => ExecuteCategoryAsync(runId,
                       SyncReferenceCategory.PresenceDefinition,
                       referenceApiClient.GetPresenceDefinitionsAsync,
                       referencesNormalizer.NormalizePresenceDefinitions,
                       referencesRepository.UpsertPresenceDefinitionsAsync,
                       ct),

                   SyncReferenceCategory.WrapUpCode => ExecuteCategoryAsync(runId,
                                                                            SyncReferenceCategory.WrapUpCode,
                                                                            referenceApiClient.GetWrapUpCodesAsync,
                                                                            referencesNormalizer.NormalizeWrapUpCodes,
                                                                            referencesRepository.UpsertWrapUpCodesAsync,
                                                                            ct),

                   // TODO: Provider endpoints for these categories are not wired yet in this flow.
                   SyncReferenceCategory.User =>
                           throw new NotSupportedException("References full-sync for User is not wired yet."),
                   SyncReferenceCategory.Queue =>
                           throw new NotSupportedException("References full-sync for Queue is not wired yet."),
                   SyncReferenceCategory.Flow =>
                           throw new NotSupportedException("References full-sync for Flow is not wired yet."),

                   _ => throw new NotSupportedException($"Unsupported references category '{category}'.")
               };
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Executes a full category pipeline: fetch, normalize, upsert, and summary run-item recording.
    /// </summary>
    private async Task ExecuteCategoryAsync<TRaw, TDto>(long runId,
                                                        SyncReferenceCategory category,
                                                        Func<CancellationToken, Task<IReadOnlyCollection<TRaw>>> fetch,
                                                        Func<IReadOnlyCollection<TRaw>, IReadOnlyCollection<TDto>>
                                                                normalize,
                                                        Func<IReadOnlyCollection<TDto>, CancellationToken, Task> upsert,
                                                        CancellationToken ct)
    {
        IReadOnlyCollection<TRaw> raw = await RunPageFetchStageAsync(runId,
                                                                     category,
                                                                     fetch,
                                                                     ct)
                                               .ConfigureAwait(false);

        IReadOnlyCollection<TDto> normalized = normalize(raw);

        await upsert(normalized, ct)
               .ConfigureAwait(false);

        await MarkSummaryCompletedAsync(runId,
                                        category,
                                        normalized.Count,
                                        ct)
               .ConfigureAwait(false);
    }

    /// <summary>
    /// Runs the fetch stage for one references category and records fetch run items.
    /// </summary>
    private async Task<IReadOnlyCollection<TContract>> RunPageFetchStageAsync<TContract>(
            long runId,
            SyncReferenceCategory category,
            Func<CancellationToken, Task<IReadOnlyCollection<TContract>>> fetch,
            CancellationToken ct)
    {
        string step = SyncRunItemSteps.ReferencesPageFetch(category.ToString());

        await syncRunItemRepository.UpsertAsync(runId,
                                                step,
                                                "fetch-start",
                                                SyncRunStatus.Running,
                                                null,
                                                ct)
                                   .ConfigureAwait(false);

        try
        {
            IReadOnlyCollection<TContract> payload = await fetch(ct)
                                                            .ConfigureAwait(false);

            await syncRunItemRepository.UpsertAsync(runId,
                                                    step,
                                                    $"fetched:{payload.Count}",
                                                    SyncRunStatus.Completed,
                                                    null,
                                                    ct)
                                       .ConfigureAwait(false);

            return payload;
        }
        catch (OperationCanceledException ex)
        {
            await syncRunItemRepository.UpsertAsync(runId,
                                                    step,
                                                    "fetch-canceled",
                                                    SyncRunStatus.Canceled,
                                                    ex.Message,
                                                    CancellationToken.None)
                                       .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await syncRunItemRepository.UpsertAsync(runId,
                                                    step,
                                                    "fetch-failed",
                                                    SyncRunStatus.Failed,
                                                    ex.Message,
                                                    CancellationToken.None)
                                       .ConfigureAwait(false);

            throw;
        }
    }

    /// <summary>
    /// Marks the category summary run item as completed with total upserted count.
    /// </summary>
    private Task MarkSummaryCompletedAsync(long runId, SyncReferenceCategory category, int total, CancellationToken ct)
    {
        string step = SyncRunItemSteps.ReferencesSummary(category.ToString());

        return syncRunItemRepository.UpsertAsync(runId,
                                                 step,
                                                 $"upserted:{total}",
                                                 SyncRunStatus.Completed,
                                                 null,
                                                 ct);
    }

    #endregion
}
