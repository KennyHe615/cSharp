using Application.Common.Abstractions.Services;
using Application.Common.Enums;


namespace Application.UserDetails;

/// <summary>
/// Handles incremental synchronization for UserDetails data.
/// </summary>
/// <param name="userDetailsSyncService">The service used to perform the users_details synchronization logic.</param>
/// <remarks>
/// This handler performs regular incremental synchronization of recent UserDetails data
/// from the Genesys API. Typically triggered by a half-hourly timer function.
/// </remarks>
public class UserDetailsIncrementalSyncHandler(IUserDetailsSyncService userDetailsSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.UserDetailsIncremental;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return userDetailsSyncService.SyncUserDetailsIncrementalAsync(ct);
    }
}
