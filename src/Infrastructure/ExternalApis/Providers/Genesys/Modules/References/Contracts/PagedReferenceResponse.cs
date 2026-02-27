namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;

/// <example>
/// {
///     "entities": [],
///     "pageSize": 100,
///     "pageNumber": 1,
///     "total": 1,
///     "pageCount": 1,
///     "nextUri": "",
///     "firstUri": "",
///     "lastUri": "",
///     "selfUri": "",
/// }
/// </example>
public sealed class PagedReferenceResponse<T>
{
    public List<T> Entities { get; set; } = [];

    public int? PageSize { get; set; }

    public int? PageNumber { get; set; }

    public long? Total { get; set; }

    public int? PageCount { get; set; }

    public string? NextUri { get; set; }

    public string? FirstUri { get; set; }

    public string? LastUri { get; set; }

    public string? SelfUri { get; set; }
}
