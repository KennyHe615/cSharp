using Application.Abstractions.External;
using Application.Abstractions.Normalization;
using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Features.SyncTracking.Shared;


namespace Application.Features.SyncTracking.References;

/// <summary>
/// References domain executor for category-level full-sync operations.
/// Fetches provider payloads, normalizes contracts, persists reference tables, and records sync checkpoints.
/// </summary>
public sealed class ReferencesSyncOrchestrator(IReferenceApiClient referenceApiClient,
                                               IReferencesNormalizer referencesNormalizer,
                                               IReferencesRepository referencesRepository,
                                               ISyncCheckpointRepository syncCheckpointRepository)
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
    /// Executes a full category pipeline: fetch -> normalize -> upsert -> summary checkpoint.
    /// </summary>
    /// <typeparam name="TRaw">Raw provider contract type.</typeparam>
    /// <typeparam name="TDto">Normalized DTO type.</typeparam>
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
    /// Runs the fetch stage for one references category and records fetch checkpoints.
    /// </summary>
    /// <typeparam name="TContract">Provider contract type returned by API client.</typeparam>
    /// <param name="runId">Physical run identifier.</param>
    /// <param name="category">References category being fetched.</param>
    /// <param name="fetch">Delegate that retrieves provider payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The fetched provider payload.</returns>
    /// <exception cref="OperationCanceledException">Thrown when host cancellation is requested.</exception>
    /// <exception cref="Exception">Propagates non-cancellation failures after checkpointing.</exception>
    private async Task<IReadOnlyCollection<TContract>> RunPageFetchStageAsync<TContract>(
        long runId,
        SyncReferenceCategory category,
        Func<CancellationToken, Task<IReadOnlyCollection<TContract>>> fetch,
        CancellationToken ct)
    {
        string step = SyncCheckpointSteps.ReferencesPageFetch(category.ToString());

        await syncCheckpointRepository.UpsertAsync(runId,
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

            await syncCheckpointRepository.UpsertAsync(runId,
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
            await syncCheckpointRepository.UpsertAsync(runId,
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
            await syncCheckpointRepository.UpsertAsync(runId,
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
    /// Marks the category summary checkpoint as completed with total upserted count.
    /// </summary>
    /// <param name="runId">Physical run identifier.</param>
    /// <param name="category">References category that completed upsert.</param>
    /// <param name="total">Total records upserted for the category.</param>
    /// <param name="ct">Cancellation token.</param>
    private Task MarkSummaryCompletedAsync(long runId, SyncReferenceCategory category, int total, CancellationToken ct)
    {
        string step = SyncCheckpointSteps.ReferencesSummary(category.ToString());

        return syncCheckpointRepository.UpsertAsync(runId,
                                                    step,
                                                    $"upserted:{total}",
                                                    SyncRunStatus.Completed,
                                                    null,
                                                    ct);
    }

    #endregion
}
