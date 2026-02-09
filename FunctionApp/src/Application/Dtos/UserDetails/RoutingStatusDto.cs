using Application.Contracts.Enums;


namespace Application.Dtos.UserDetails;

public sealed class RoutingStatusDto
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public RoutingStatus RoutingStatus { get; set; }
}
