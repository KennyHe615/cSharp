using Application.Common.Enums;
using Application.Common.Models;


namespace Application.Common.Abstractions.Services;

/// <summary>
/// Defines a service for subdividing time intervals into smaller chunks based on hit count thresholds.
/// </summary>
public interface IIntervalSubdivisionService
{
    /// <summary>
    /// Subdivides the specified interval into smaller intervals based on hit count thresholds.
    /// </summary>
    /// <param name="interval">The interval to subdivide.</param>
    /// <param name="category">The synchronization category determining the hit count provider.</param>
    /// <param name="ct">The cancellation token to abort the operation.</param>
    /// <returns>A list of subdivided intervals with page information.</returns>
    Task<List<IntervalWithPages>> SubdivideAsync(Interval interval,
                                                 SyncCategory category,
                                                 CancellationToken ct = default);
}
