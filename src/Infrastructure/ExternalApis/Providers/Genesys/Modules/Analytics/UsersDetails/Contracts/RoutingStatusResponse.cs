using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Contracts;

public class RoutingStatusResponse
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public RoutingStatusKind RoutingStatus { get; set; }
}
