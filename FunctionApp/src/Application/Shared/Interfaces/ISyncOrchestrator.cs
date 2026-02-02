using Application.Shared.Enums;


namespace Application.Shared.Interfaces;

/// <summary>
/// Defines an orchestrator responsible for managing the synchronization workflow across different Lines of Business (LOBs).
/// </summary>
public interface ISyncOrchestrator
{
    /// <summary>
    /// Executes the synchronization process for a specific Line of Business and synchronization category.
    /// </summary>
    /// <param name="lobName">The unique identifier or name of the Line of Business to synchronize.</param>
    /// <param name="category">The specific category of data to be synchronized (e.g., References, Skills).</param>
    /// <param name="externalToken">A cancellation token to observe while waiting for the task to complete.</param>
    /// <returns>A task representing the asynchronous orchestration operation.</returns>
    Task ExecuteAsync(string lobName, SyncCategory category, CancellationToken externalToken);
}
