using System.ComponentModel.DataAnnotations;


namespace Infrastructure.ExternalApis.Genesys;

public class GenesysOptions
{
    public const string SectionName = "Genesys";

    // public const string OAuthBaseUrl = "https://login.cac1.pure.cloud";
    // public const string ApiBaseUrl = "https://api.cac1.pure.cloud";
    [Required]
    public string OAuthBaseUrl { get; init; } = null!;

    [Required]
    public string ApiBaseUrl { get; init; } = null!;

    public int DefaultPageSize { get; init; } = 100;

    public string DefaultQueryOrder { get; init; } = "asc";

    [Range(1, 1_000_000)]
    public int MaxHitThreshold { get; init; } = 100_000;

    // Defined by Genesys
    public string OAuthEndpoint = "oauth/token";

    public int HistoricalDataLimitDays { get; init; } = 558;

    public int MaxIntervalDays { get; init; } = 7;
}
