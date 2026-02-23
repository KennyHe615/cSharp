namespace Application.Contracts.ExternalApis.Genesys.References;

/// <summary>
/// Represents a Wrap_Up_Code from the Genesys API.
/// </summary>
/// <example>
/// {
///     "id": "d1827868-eeac-4e7e-8495-4b9f60f4a499",
///     "name": "N/O | I Have to Speak to my Spouse",
///     "division": {
///         "id": "c4f759db-6269-4b1e-8074-67d47ed44c15",
///         "name": "Financeit",
///         "selfUri": "/api/v2/authorization/divisions/c4f759db-6269-4b1e-8074-67d47ed44c15"
///     },
///     "dateCreated": "2025-05-01T20:15:17.956Z",
///     "dateModified": "2025-06-01T20:15:17.956Z",
///     "createdBy": "3d6aca2c-00d8-43af-a8b3-f63b5761b9f9",
///     "modifiedBy": "5fe9a50b-e419-40cb-9d5a-94828d10630d",
///     "selfUri": "/api/v2/routing/wrapupcodes/d1827868-eeac-4e7e-8495-4b9f60f4a499"
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
