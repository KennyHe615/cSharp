using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.Contracts.ExternalApis.Genesys.UsersDetails;

public sealed class PrimaryPresenceRawContract
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public SystemPresenceKind SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
