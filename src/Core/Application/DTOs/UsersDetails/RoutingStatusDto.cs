using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.DTOs.UsersDetails;

public sealed class RoutingStatusDto
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public RoutingStatusKind RoutingStatus { get; set; }
}
