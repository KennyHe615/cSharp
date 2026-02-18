namespace Application.UserDetails;

public interface IUserDetailsSyncService
{
    Task SyncUserDetailsIncrementalAsync(CancellationToken ct);

    Task SyncUserDetailsRecoveryAsync(CancellationToken ct);
}
