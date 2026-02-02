using Application.References.Services;
using Application.Shared.Enums;
using Application.Shared.Interfaces;


namespace Application.References;

/// <summary>
/// A synchronization handler specifically for reference data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="referencesSyncService">The service responsible for performing the actual reference data synchronization.</param>
public class ReferencesSyncHandler(IReferencesSyncService referencesSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.Skills;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return referencesSyncService.SyncAllAsync(ct);
    }
}
