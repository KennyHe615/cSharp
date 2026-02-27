using Application.Enums;


namespace Application.DTOs.UserDetails;

public sealed class RoutingStatusDto
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public RoutingStatusKind RoutingStatus { get; set; }
}
