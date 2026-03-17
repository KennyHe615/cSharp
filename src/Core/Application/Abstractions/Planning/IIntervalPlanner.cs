using Application.DTOs.Planning;
using Application.Enums;

using SharedKernel.Time;


namespace Application.Abstractions.Planning;

/// <summary>
/// Builds provider-safe execution slices for analytics sync categories based on a UTC interval.
/// </summary>
public interface IIntervalPlanner
{
    /// <summary>
    /// Plans sub-intervals for the specified analytics category so each slice can be processed
    /// within provider constraints (historical window, max interval span, and hit threshold).
    /// </summary>
    /// <param name="category">
    /// Target analytics category. Expected values are <see cref="SyncCategory.UsersDetails"/> or
    /// <see cref="SyncCategory.ConversationsDetails"/>.
    /// </param>
    /// <param name="interval">Source UTC interval to split.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// Ordered list of planned slices covering the input interval from start to end without overlap.
    /// Each item includes total hits and derived page metadata.
    /// </returns>
    Task<IReadOnlyList<PlannedIntervalDto>> PlanAsync(SyncCategory category,
                                                      UtcInterval interval,
                                                      CancellationToken ct = default);
}
