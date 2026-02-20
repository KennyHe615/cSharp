namespace Infrastructure.Persistence.Entities.References;

public class WrapUpCode : Audit
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public string? DivisionId { get; set; }

    public string? DivisionName { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public string? CreatedBy { get; set; }

    public string? ModifiedBy { get; set; }
}
