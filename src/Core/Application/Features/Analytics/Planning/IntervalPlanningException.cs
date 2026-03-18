namespace Application.Features.Analytics.Planning;

/// <summary>
/// Represents planner-specific failures while building Genesys-safe intervals.
/// </summary>
public sealed class IntervalPlanningException : Exception
{
    public IntervalPlanningException(string message) : base(message)
    {
    }

    public IntervalPlanningException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
