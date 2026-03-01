using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.Analytics.UsersDetails.Contracts;

public sealed class PrimaryPresenceResponse
{
    public DateTimeOffset StartTime { get; set; }

    public DateTimeOffset? EndTime { get; set; }

    public SystemPresenceKind SystemPresence { get; set; }

    public string? OrganizationPresenceId { get; set; }
}
