using Application.Enums;


namespace Application.DTOs.UserDetails;

public sealed class PrimaryPresenceDto
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public SystemPresenceKind SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
