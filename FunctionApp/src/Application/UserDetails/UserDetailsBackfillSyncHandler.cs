using Application.Common.Abstractions.Services;
using Application.Common.Enums;


namespace Application.UserDetails;

/// <summary>
/// Handles backfill synchronization for UserDetails data.
/// </summary>
/// <param name="userDetailsSyncService">The service used to perform the users_details synchronization logic.</param>
/// <remarks>
/// This handler is responsible for recovering failed or missing intervals by querying
/// the database and re-processing historical data that was not successfully synchronized.
/// Typically triggered by a daily timer function.
/// </remarks>
public class UserDetailsBackfillSyncHandler(IUserDetailsSyncService userDetailsSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.UserDetailsBackfill;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return userDetailsSyncService.SyncUserDetailsBackfillAsync(ct);
    }
}
