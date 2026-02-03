using Application.Shared.Enums;
using Application.Shared.Interfaces;


namespace Application.References.Handlers;

/// <summary>
/// A synchronization handler for Presence_Definition data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="referencesSyncService">The service used to perform the presence_definition synchronization logic.</param>
public class PresenceDefinitionSyncHandler(IReferencesSyncService referencesSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.PresenceDefinitions;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return referencesSyncService.SyncPresenceDefinitionAsync(ct);
    }
}
