namespace Application.Abstractions.Planning;

/// <summary>
/// Defines hit-count query capability for one analytics category.
/// </summary>
public interface IHitCountProvider
{
    /// <summary>
    /// Returns total hits within the UTC interval [start, end], where both boundaries are inclusive.
    /// </summary>
    /// <param name="start">UTC interval start (inclusive).</param>
    /// <param name="end">UTC interval end (inclusive).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Total hits for the specified interval.</returns>
    Task<int> GetHitCountAsync(DateTimeOffset start, DateTimeOffset end, CancellationToken ct = default);
}
