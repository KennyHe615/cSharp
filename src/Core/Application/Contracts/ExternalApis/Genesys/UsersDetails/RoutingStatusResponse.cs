using Application.Enums;


namespace Application.Contracts.ExternalApis.Genesys.UsersDetails;

public class RoutingStatusResponse
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public RoutingStatusKind RoutingStatus { get; set; }
}
