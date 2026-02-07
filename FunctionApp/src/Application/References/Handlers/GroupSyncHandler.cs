using Application.Common.Abstractions.Sync;
using Application.Common.Enums;


namespace Application.References.Handlers;

/// <summary>
/// A synchronization handler for Group data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="referencesSyncService">The service used to perform the group synchronization logic.</param>
public class GroupSyncHandler(IReferencesSyncService referencesSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.Group;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return referencesSyncService.SyncGroupsAsync(ct);
    }
}
