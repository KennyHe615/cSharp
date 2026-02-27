using System.ComponentModel.DataAnnotations;


namespace Infrastructure.Configuration.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    [Required]
    [Url]
    public string Uri { get; init; } = null!;

    [Required]
    [MinLength(1)]
    public string GenesysClientIdSecretPrefix { get; init; } = "GenesysClientId";

    [Required]
    [MinLength(1)]
    public string GenesysClientSecretPrefix { get; init; } = "GenesysClientSecret";

    [Required]
    [MinLength(1)]
    public string GenesysTokenSecretPrefix { get; init; } = "GenesysToken";

    [Required]
    [MinLength(1)]
    public string LandingDbConnStrSecretPrefix { get; init; } = "LandingDbConnStr";

    [Range(1, 1440, ErrorMessage = "Cache duration must be between 1 and 1440 minutes")]
    public int CacheDurationMinutes { get; set; } = 60;

    [Range(1, 10, ErrorMessage = "Max retry attempts must be between 1 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(100, 60000, ErrorMessage = "Retry delay must be between 1 and 60 seconds")]
    public int RetryDelayMilliseconds { get; set; } = 1000;

    public bool UseExponentialBackoff { get; set; } = true;
}

// public const string Uri = "https://kv-bi-services-dev-01.vault.azure.net/";
