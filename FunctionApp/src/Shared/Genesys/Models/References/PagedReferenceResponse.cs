namespace Shared.Genesys.Models.References;

public class PagedReferenceResponse<T>
{
    public List<T> Entities { get; set; } = [];

    public int? PageSize { get; set; }

    public int? PageNumber { get; set; }

    public long? Total { get; set; }

    public string? NextUri { get; set; }

    public string? FirstUri { get; set; }

    public string? LastUri { get; set; }

    public string? SelfUri { get; set; }

    public int? PageCount { get; set; }
}
