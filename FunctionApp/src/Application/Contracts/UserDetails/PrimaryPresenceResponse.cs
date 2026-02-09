using Application.Contracts.Enums;


namespace Application.Contracts.UserDetails;

public sealed class PrimaryPresenceResponse
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public SystemPresence SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
