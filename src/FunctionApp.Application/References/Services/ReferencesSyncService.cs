using AutoMapper;

using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Application.Shared.Context;
using FunctionApp.Application.Shared.Providers;
using FunctionApp.Domain.Entities.References;
using FunctionApp.Domain.Repositories;

using Microsoft.Extensions.Logging;


namespace FunctionApp.Application.References.Services;

public class ReferencesSyncService(IReferencesClient referencesClient,
                                   IUnitOfWork unitOfWork,
                                   IMapper mapper,
                                   ILobContext lobContext,
                                   IDateTimeProvider dateTimeProvider,
                                   ILogger<ReferencesSyncService> logger) : IReferencesSyncService
{
    private string? LobName => lobContext.LobName;

    public async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            // 1. Start all fetch tasks in parallel to improve performance (Parallel I/O)
            Task<List<SkillResponseDto>> skillsTask = referencesClient.GetSkillsAsync(cancellationToken);
            Task<List<PresenceDefinitionResponseDto>> presenceDefinitionsTask =
                referencesClient.GetPresenceDefinitionsAsync(cancellationToken);
            Task<List<GroupResponseDto>> groupsTask = referencesClient.GetGroupsAsync(cancellationToken);

            // Wait for all API calls to complete
            await Task.WhenAll(skillsTask, presenceDefinitionsTask, groupsTask);

            // 2. Process, Map, and Save changes sequentially to ensure DbContext thread safety
            List<(string Name, bool Success)> syncResults =
            [
                await ProcessAndSaveAsync<SkillResponseDto, Skill>("Skills", await skillsTask, cancellationToken),
                await ProcessAndSaveAsync<PresenceDefinitionResponseDto, PresenceDefinition>(
                    "PresenceDefinitions",
                    await presenceDefinitionsTask,
                    cancellationToken),
                await ProcessAndSaveAsync<GroupResponseDto, Group>("Groups", await groupsTask, cancellationToken)
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

    private async Task<(string Name, bool Success)> ProcessAndSaveAsync<TDto, TEntity>(
        string entityName,
        List<TDto> dtos,
        CancellationToken cancellationToken) where TEntity : class
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

            List<TEntity>? entities = mapper.Map<List<TEntity>>(dtos);

            await unitOfWork.UpsertRangeAsync(entities, cancellationToken);

            // Sequential SaveChanges for this entity type
            int savedCount = await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "[LOB: {Lob} Reference \"{EntityName}\"] Successfully synchronized (Changes: {Count})",
                LobName,
                entityName,
                savedCount);

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
            logger.LogCritical("[LOB: {Lob}]💯Synchronization of [References] SUCCESSFULLY ON **{Time}**",
                               LobName,
                               dateTimeProvider.FormatLocalTimestamp());

            return;
        }

        logger.LogCritical(
            "[LOB: {Lob}] Synchronization of [References]❌PARTIALLY SUCCESSFUL ON **{Time}**. FAILED: [{Failed}]",
            LobName,
            dateTimeProvider.FormatLocalTimestamp(),
            string.Join(", ", failed));
    }

    #endregion
}
