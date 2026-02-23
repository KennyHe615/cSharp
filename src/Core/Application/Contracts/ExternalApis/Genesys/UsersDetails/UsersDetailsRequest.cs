namespace Application.Contracts.ExternalApis.Genesys.UsersDetails;

public sealed class UsersDetailsRequest
{
    public string Order { get; set; } = "asc";

    public string Interval { get; set; } = string.Empty;

    public Paging? Paging { get; set; }
}

public sealed class Paging
{
    public int PageSize { get; set; } = 100;

    public int PageNumber { get; set; } = 1;
}
