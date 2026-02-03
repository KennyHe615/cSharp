namespace Infrastructure.Persistence.Entities.References;

public class WrapupCode : Audit
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Guid? DivisionId { get; set; }

    public string? DivisionName { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? ModifiedBy { get; set; }
}
