namespace Application.References.Services;

/// <summary>
/// Service interface responsible for synchronizing skill data from external sources.
/// </summary>
public interface ISkillSyncService
{
    /// <summary>
    /// Orchestrates the asynchronous synchronization process for skills.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncSkillAsync(CancellationToken ct);
}
