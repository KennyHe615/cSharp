namespace Application.UserDetails;

public interface IUserDetailsSyncService
{
    Task SyncUserDetailsIncrementalAsync(CancellationToken ct);

    Task SyncUserDetailsBackfillAsync(CancellationToken ct);
}
