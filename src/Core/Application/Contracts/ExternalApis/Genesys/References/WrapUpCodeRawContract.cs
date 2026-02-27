namespace Application.Contracts.ExternalApis.Genesys.References;

public sealed class WrapUpCodeRawContract
{
    public Guid Id { get; set; }

    public string? Name { get; set; }

    public Dictionary<string, string>? Division { get; set; }

    public DateTimeOffset? DateCreated { get; set; }

    public DateTimeOffset? DateModified { get; set; }

    public string? CreatedBy { get; set; }

    public string? ModifiedBy { get; set; }

    public string? SelfUri { get; set; }
}
