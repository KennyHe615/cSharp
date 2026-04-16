using Application.Contracts.ExternalApis.Genesys.Enums;


namespace Application.DTOs.References;

public sealed class WrapUpCodeDto
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? DivisionId { get; set; }

    public string? DivisionName { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public string? CreatedBy { get; set; }

    public string? ModifiedBy { get; set; }

    public StateKind State { get; set; }
}
