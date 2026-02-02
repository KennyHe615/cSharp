using Application.Shared.Enums;


namespace Application.Shared.Interfaces;

/// <summary>
/// Defines a handler for a specific synchronization category.
/// </summary>
public interface ISyncCategoryHandler
{
    /// <summary>
    /// Gets the synchronization category that this handler manages.
    /// </summary>
    SyncCategory Category { get; }

    /// <summary>
    /// Executes the synchronization logic for the associated category.
    /// </summary>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous execution.</returns>
    Task ExecuteAsync(CancellationToken ct);
}
