using Application.References;
using Application.Shared.Context;
using Application.Shared.Enums;

using Infrastructure.ExternalServices;
using Infrastructure.Persistence;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.Services.References;

public class ReferencesSyncService(IReferencesClient referencesClient,
                                   IReferencesRepository referencesRepository,
                                   ILobContext lobContext,
                                   ILogger<ReferencesSyncService> logger) : IReferencesSyncService
{
    private string LobName => lobContext.LobName;

    /// <summary>
    /// Synchronizes Skills data from Genesys Cloud to the database for the current LOB.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// This method fetches all skills from the Genesys API, validates that data is present,
    /// and delegates persistence to the repository. If no skills are found, the operation is logged
    /// as an error and exits early without modifying the database.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the Genesys API request fails.</exception>
    /// <exception cref="PersistenceException">Thrown when database persistence fails.</exception>
    public async Task SyncSkillsAsync(CancellationToken ct)
    {
        await SyncReferenceDataAsync(nameof(SyncCategory.Skill),
                                     () => referencesClient.GetSkillsAsync(ct),
                                     skills => referencesRepository.UpsertSkillsAsync(skills, ct))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronizes Presence Definitions data from Genesys Cloud to the database for the current LOB.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// This method fetches all presence definitions from the Genesys API, validates that data is present,
    /// and delegates persistence to the repository. If no presence definitions are found, the operation is logged
    /// as an error and exits early without modifying the database.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the Genesys API request fails.</exception>
    /// <exception cref="PersistenceException">Thrown when database persistence fails.</exception>
    public async Task SyncPresenceDefinitionsAsync(CancellationToken ct)
    {
        await SyncReferenceDataAsync(nameof(SyncCategory.PresenceDefinition),
                                     () => referencesClient.GetPresenceDefinitionsAsync(ct),
                                     presenceDefinitions =>
                                         referencesRepository.UpsertPresenceDefinitionsAsync(presenceDefinitions, ct))
            .ConfigureAwait(false);
    }

    /// <summary>
    /// Synchronizes Groups data from Genesys Cloud to the database for the current LOB.
    /// </summary>
    /// <param name="ct">A token to monitor for cancellation requests during the asynchronous operation.</param>
    /// <remarks>
    /// This method fetches all groups from the Genesys API, validates that data is present,
    /// and delegates persistence to the repository. If no groups are found, the operation is logged
    /// as an error and exits early without modifying the database.
    /// </remarks>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled via the <paramref name="ct"/>.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the Genesys API request fails.</exception>
    /// <exception cref="PersistenceException">Thrown when database persistence fails.</exception>
    public async Task SyncGroupsAsync(CancellationToken ct)
    {
        await SyncReferenceDataAsync(nameof(SyncCategory.Group),
                                     () => referencesClient.GetGroupsAsync(ct),
                                     groups => referencesRepository.UpsertGroupsAsync(groups, ct))
            .ConfigureAwait(false);
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Generic synchronization workflow for reference data: fetch, validate, persist, and log.
    /// </summary>
    /// <typeparam name="TData">The type of reference data being synchronized (e.g., SkillResponse, PresenceDefinitionResponse).</typeparam>
    /// <param name="categoryName">The human-readable category name for logging purposes.</param>
    /// <param name="fetchDataAsync">A delegate that fetches data from the Genesys API.</param>
    /// <param name="persistDataAsync">A delegate that persists the fetched data to the database.</param>
    /// <exception cref="OperationCanceledException">Thrown when the operation is cancelled.</exception>
    /// <exception cref="ExternalServiceHttpException">Thrown when the API fetch fails.</exception>
    /// <exception cref="PersistenceException">Thrown when database persistence fails.</exception>
    private async Task SyncReferenceDataAsync<TData>(string categoryName,
                                                     Func<Task<List<TData>>> fetchDataAsync,
                                                     Func<IReadOnlyCollection<TData>, Task> persistDataAsync)
    {
        logger.LogDebug("[LOB: {Lob} Reference \"{Category}\"] Starting synchronization", LobName, categoryName);

        try
        {
            List<TData> data = await fetchDataAsync().ConfigureAwait(false);

            int count = data.Count;
            if (count == 0)
            {
                logger.LogError("[LOB: {Lob} Reference \"{Category}\"] No data found in Genesys to synchronize",
                                LobName,
                                categoryName);

                return;
            }

            await persistDataAsync(data).ConfigureAwait(false);

            logger.LogInformation("[LOB: {Lob} Reference \"{Category}\"] Successfully synchronized (Count: {Count})",
                                  LobName,
                                  categoryName,
                                  count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "[LOB: {Lob} Reference \"{Category}\"] Failure during synchronization. Exception: {Exception}",
                            LobName,
                            categoryName,
                            ex.ToJson());

            throw;
        }
    }

    #endregion
}
