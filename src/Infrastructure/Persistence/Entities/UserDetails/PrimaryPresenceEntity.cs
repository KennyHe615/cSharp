using Infrastructure.ExternalApis.Providers.Genesys.Enums;


namespace Infrastructure.Persistence.Entities.UserDetails;

public class PrimaryPresenceEntity : Audit
{
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the Genesys start timestamp normalized to UTC.
    /// </summary>
    public DateTimeOffset StartTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the Genesys end timestamp normalized to UTC.
    /// </summary>
    public DateTimeOffset? EndTimeUtc { get; set; }

    /// <summary>
    /// Gets or sets the start timestamp converted to Eastern time.
    /// </summary>
    public DateTimeOffset StartTimeEastern { get; set; }

    public SystemPresence SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
