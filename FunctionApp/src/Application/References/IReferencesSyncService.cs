namespace Application.References;

public interface IReferencesSyncService
{
    /// <summary>
    /// Orchestrates the asynchronous synchronization process for skills.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncSkillsAsync(CancellationToken ct);

    /// <summary>
    /// Orchestrates the asynchronous synchronization process for presence_definitions.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncPresenceDefinitionsAsync(CancellationToken ct);

    /// <summary>
    /// Orchestrates the asynchronous synchronization process for groups.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncGroupsAsync(CancellationToken ct);

    /// <summary>
    /// Orchestrates the asynchronous synchronization process for wrapup_codes.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous synchronization operation.</returns>
    Task SyncWrapupCodesAsync(CancellationToken ct);
}
