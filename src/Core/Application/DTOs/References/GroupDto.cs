using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.DTOs.References;

public sealed class GroupDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public int? MemberCount { get; set; }

    public StateKind? State { get; set; }

    public int? Version { get; set; }

    public GroupTypeKind? Type { get; set; }

    public bool? RulesVisible { get; set; }

    public VisibilityKind? Visibility { get; set; }

    public string? ChatJabberId { get; set; }

    public bool? RolesEnabled { get; set; }

    public bool? IncludeOwners { get; set; }
}
