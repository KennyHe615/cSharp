using Application.Dtos.References;
using Application.References.Clients;
using Application.References.Services;
using Application.Shared.Context;
using Application.Shared.Extensions;

using Infrastructure.Persistence.Repositories.References;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.Services.References.Skills;

/// <summary>
/// Implementation of <see cref="ISkillSyncService"/> that orchestrates the synchronization of skills
/// from the Genesys API to the local database.
/// </summary>
/// <param name="referencesClient">The client used to fetch data from Genesys.</param>
/// <param name="skillRepository">The repository for persistence operations.</param>
/// <param name="lobContext">The context providing LOB-specific details.</param>
/// <param name="logger">The logger instance.</param>
public sealed class SkillSyncService(IReferencesClient referencesClient,
                                     ISkillRepository skillRepository,
                                     ILobContext lobContext,
                                     ILogger<SkillSyncService> logger) : ISkillSyncService
{
    private string LobName => lobContext.LobName;

    private const string CategoryName = "SKILL";

    /// <inheritdoc />
    public async Task SyncSkillAsync(CancellationToken ct)
    {
        using IDisposable scope = logger.BeginOperationScope($"{CategoryName} Sync", LobName);

        try
        {
            logger.LogDebug("[LOB: {Lob} Reference \"{Category}\"] Starting synchronization", LobName, CategoryName);

            List<SkillResponse> skills = await referencesClient.GetSkillsAsync(ct).ConfigureAwait(false);

            int count = skills.Count;
            if (count == 0)
            {
                logger.LogError("[LOB: {Lob} Reference \"{Category}\"] No data found in Genesys to synchronize",
                                LobName,
                                CategoryName);

                return;
            }

            await skillRepository.UpsertSkillAsync(skills, ct).ConfigureAwait(false);

            logger.LogInformation("[LOB: {Lob} Reference \"{Category}\"] Successfully synchronized (Changes: {Count})",
                                  LobName,
                                  CategoryName,
                                  count);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "[LOB: {Lob} Reference \"{Category}\"] Failure during synchronization. Exception: {Exception}",
                            LobName,
                            CategoryName,
                            ex.ToJson());

            throw;
        }
    }
}
