using AutoMapper;

using FunctionApp.Application.References.Clients;
using FunctionApp.Application.References.DTOs;
using FunctionApp.Domain.Entities.References;
using FunctionApp.Domain.Repositories;

using Microsoft.Extensions.Logging;


namespace FunctionApp.Application.References.Services;

public class ReferencesSyncService(ISkillClient skillClient,
                                   IUnitOfWork unitOfWork,
                                   IMapper mapper,
                                   ILogger<ReferencesSyncService> logger) : IReferencesSyncService
{
    public async Task SyncAllAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting synchronization of all reference entities...");

        try
        {
            await SyncSkillsAsync(cancellationToken);

            // Add other reference entities here (Languages, Queues, etc.)

            await unitOfWork.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Successfully synchronized all reference entities");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while synchronizing reference entities");

            throw;
        }
    }

    #region ========== *** Private Methods *** ==========

    private async Task SyncSkillsAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Fetching skills from Genesys...");
        List<SkillDto> skillDtos = await skillClient.GetSkillsAsync(cancellationToken);

        if (skillDtos.Count == 0)
        {
            logger.LogError("No skills found in Genesys");

            return;
        }

        logger.LogInformation("Mapping {Count} skills to domain entities...", skillDtos.Count);
        List<Skill> skills = mapper.Map<List<Skill>>(skillDtos);

        logger.LogInformation("Upserting skills into the database...");
        await unitOfWork.UpsertRangeAsync(skills, cancellationToken);
    }

    #endregion
}
