using Application.References.Services;
using Application.Shared.Enums;
using Application.Shared.Interfaces;


namespace Application.References.Handlers;

/// <summary>
/// A synchronization handler for Skill data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="skillSyncService">The service used to perform the skill synchronization logic.</param>
public class SkillSyncHandler(ISkillSyncService skillSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.Skills;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return skillSyncService.SyncSkillAsync(ct);
    }
}
