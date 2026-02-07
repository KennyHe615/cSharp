using Application.Common.Abstractions.Sync;
using Application.Common.Enums;


namespace Application.References.Handlers;

/// <summary>
/// A synchronization handler for Wrapup_Code data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="referencesSyncService">The service used to perform the wrapup_code synchronization logic.</param>
public class WrapupCodeSyncHandler(IReferencesSyncService referencesSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.WrapupCode;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return referencesSyncService.SyncWrapupCodesAsync(ct);
    }
}
