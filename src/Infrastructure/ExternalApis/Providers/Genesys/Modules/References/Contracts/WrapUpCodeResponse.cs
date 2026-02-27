namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;

/// <summary>
/// Represents a Wrap_Up_Code from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "",
///     "name": "",
///     "division": {
///         "id": "",
///         "name": "",
///         "selfUri": ""
///     },
///     "dateCreated": "2025-05-01T20:15:17.956Z",
///     "dateModified": "2025-06-01T20:15:17.956Z",
///     "createdBy": "",
///     "modifiedBy": "",
///     "selfUri": ""
/// }
/// </example>
public sealed class WrapUpCodeResponse
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
