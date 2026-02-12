using Application.Common.Abstractions.Services;
using Application.Common.Enums;


namespace Application.UserDetails;

/// <summary>
/// A synchronization handler for Users_Details data, implementing the <see cref="ISyncCategoryHandler"/> interface.
/// </summary>
/// <param name="userDetailsSyncService">The service used to perform the users_details synchronization logic.</param>
public class UserDetailsSyncHandler(IUserDetailsSyncService userDetailsSyncService) : ISyncCategoryHandler
{
    /// <inheritdoc />
    public SyncCategory Category => SyncCategory.UserDetails;

    /// <inheritdoc />
    public Task ExecuteAsync(CancellationToken ct)
    {
        return userDetailsSyncService.SyncUserDetailsAsync(ct);
    }
}
