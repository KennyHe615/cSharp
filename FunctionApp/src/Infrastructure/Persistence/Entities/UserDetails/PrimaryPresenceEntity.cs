using Application.Contracts.Enums;


namespace Infrastructure.Persistence.Entities.UserDetails;

public class PrimaryPresenceEntity : Audit
{
    public Guid UserId { get; set; }

    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public long? DurationInSeconds { get; set; }

    public SystemPresence SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
