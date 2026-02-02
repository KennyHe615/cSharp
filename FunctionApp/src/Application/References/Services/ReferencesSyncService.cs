using Application.Dtos.References;
using Application.References.Clients;
using Application.Shared.Context;
using Application.Shared.Enums;
using Application.Shared.Extensions;

using Microsoft.Extensions.Logging;

using Shared.Providers;


namespace Application.References.Services;

public partial class ReferencesSyncService(IReferencesClient referencesClient,
                                           IReferencesWriter referencesWriter,
                                           ILobContext lobContext,
                                           IDateTimeProvider dateTimeProvider,
                                           ILogger<ReferencesSyncService> logger) : IReferencesSyncService
{
    private string? LobName => lobContext.LobName;

    public async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        using IDisposable scope = logger.BeginOperationScope("ReferencesSync", LobName);

        try
        {
            // 1. Start all fetch tasks in parallel to improve performance (Parallel I/O)
            Task<List<PresenceDefinitionResponse>> presenceDefinitionsTask =
                referencesClient.GetPresenceDefinitionsAsync(cancellationToken);
            Task<List<GroupResponse>> groupsTask = referencesClient.GetGroupsAsync(cancellationToken);

            // Wait for all API calls to complete
            await Task.WhenAll(presenceDefinitionsTask, groupsTask);

            // 2. Process, Map, and Save changes sequentially to ensure DbContext thread safety
            List<(string Name, bool Success)> syncResults =
            [
                await ProcessAndSaveAsync(nameof(SyncCategory.PresenceDefinitions),
                                          await presenceDefinitionsTask,
                                          referencesWriter.UpsertPresenceDefinitionsAsync,
                                          cancellationToken),
                await ProcessAndSaveAsync(nameof(SyncCategory.Groups),
                                          await groupsTask,
                                          referencesWriter.UpsertGroupsAsync,
                                          cancellationToken)
            ];

            // 3. Report final summary
            ReportFinalStatus(syncResults);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LOB: {Lob}] Critical failure during reference entities synchronization", LobName);

            throw;
        }
    }

    #region ========== *** Private Methods *** ==========

    private async Task<(string Name, bool Success)> ProcessAndSaveAsync<TDto>(
        string entityName,
        List<TDto> dtos,
        Func<IReadOnlyList<TDto>, CancellationToken, Task> upsertAsync,
        CancellationToken cancellationToken)
    {
        try
        {
            if (dtos.Count == 0)
            {
                logger.LogError("[LOB: {Lob} Reference \"{EntityName}\"] No data found in Genesys to synchronize",
                                LobName,
                                entityName);

                return (entityName, false);
            }

            await upsertAsync(dtos, cancellationToken);

            logger.LogInformation(
                "[LOB: {Lob} Reference \"{EntityName}\"] Successfully synchronized (Changes: {Count})",
                LobName,
                entityName,
                dtos.Count);

            return (entityName, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LOB: {Lob} Reference \"{EntityName}\"] Synchronization failed", LobName, entityName);

            return (entityName, false);
        }
    }

    private void ReportFinalStatus(List<(string Name, bool Success)> results)
    {
        List<string> failed = results.Where(r => !r.Success).Select(r => r.Name).ToList();

        if (failed.Count == 0)
        {
            logger.LogCritical("🔚[LOB: {Lob}]🔚Synchronization of [References] SUCCESSFULLY", LobName);

            return;
        }

        logger.LogCritical("[LOB: {Lob}] Synchronization of [References]❌PARTIALLY SUCCESSFUL. FAILED: [{Failed}]",
                           LobName,
                           string.Join(", ", failed));
    }

    #endregion
}
