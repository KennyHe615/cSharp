namespace Application.References.Services;

public interface IReferencesSyncService
{
    Task SyncAllAsync(CancellationToken cancellationToken);
}
