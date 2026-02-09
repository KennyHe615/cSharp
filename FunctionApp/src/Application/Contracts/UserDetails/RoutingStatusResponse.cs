using Application.Contracts.Enums;


namespace Application.Contracts.UserDetails;

public class RoutingStatusResponse
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public RoutingStatus RoutingStatus { get; set; }
}
