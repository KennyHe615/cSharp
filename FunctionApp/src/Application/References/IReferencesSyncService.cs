namespace Application.References;

public interface IReferencesSyncService
{
    /// <summary>
    /// Orchestrates the asynchronous synchronization process for skills.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncSkillAsync(CancellationToken ct);

    /// <summary>
    /// Orchestrates the asynchronous synchronization process for presence_definitions.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncPresenceDefinitionAsync(CancellationToken ct);
}
