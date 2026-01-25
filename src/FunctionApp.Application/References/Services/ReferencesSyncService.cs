using AutoMapper;

using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Domain.Entities.References;
using FunctionApp.Domain.Repositories;

using Microsoft.Extensions.Logging;


namespace FunctionApp.Application.References.Services;

public class ReferencesSyncService(IReferencesClient referencesClient,
                                   IUnitOfWork unitOfWork,
                                   IMapper mapper,
                                   ILogger<ReferencesSyncService> logger) : IReferencesSyncService
{
    public async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting synchronization of all reference entities...");

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
            logger.LogError(ex, "Critical failure during reference entities synchronization");

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
                logger.LogError("No {EntityName} found in Genesys to synchronize", entityName);

                return (entityName, false);
            }

            List<TEntity>? entities = mapper.Map<List<TEntity>>(dtos);

            await unitOfWork.UpsertRangeAsync(entities, cancellationToken);

            // Sequential SaveChanges for this entity type
            int savedCount = await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully synchronized {EntityName} (Changes: {Count})", entityName, savedCount);

            return (entityName, true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Synchronization failed for {EntityName}", entityName);

            return (entityName, false);
        }
    }

    private void ReportFinalStatus(List<(string Name, bool Success)> results)
    {
        List<string> failed = results.Where(r => !r.Success).Select(r => r.Name).ToList();

        if (failed.Count == 0)
        {
            logger.LogInformation("All reference entities synchronized successfully");

            return;
        }

        logger.LogWarning("Reference synchronization completed with partial success. Failed: [{Failed}]",
                          string.Join(", ", failed));
    }

    #endregion
}
