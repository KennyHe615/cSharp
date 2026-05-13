using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.DTOs.UsersDetails;

public sealed class RoutingStatusDto
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTimeUtc { get; set; }

    public DateTimeOffset? EndTimeUtc { get; set; }

    public DateTimeOffset StartTimeEastern { get; set; }

    public RoutingStatusKind RoutingStatus { get; set; }
}
