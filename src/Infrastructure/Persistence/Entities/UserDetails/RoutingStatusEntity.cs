using Infrastructure.ExternalApis.Providers.Genesys.Enums;


namespace Infrastructure.Persistence.Entities.UserDetails;

public class RoutingStatusEntity : Audit
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public RoutingStatus RoutingStatus { get; set; }
}
