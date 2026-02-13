using Application.Common.Abstractions.Services;
using Application.Common.Enums;


namespace Application.References.Handlers;

/// <summary>
/// A synchronization handler for Skill data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="referencesSyncService">The service used to perform the skill synchronization logic.</param>
public class SkillSyncHandler(IReferencesSyncService referencesSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.Skill;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return referencesSyncService.SyncSkillsAsync(ct);
    }
}
