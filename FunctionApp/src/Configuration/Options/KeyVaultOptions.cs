using System.ComponentModel.DataAnnotations;


namespace Configuration.Options;

public sealed class KeyVaultOptions
{
    public const string SectionName = "KeyVault";

    [Required(ErrorMessage = "Key Vault URI is required")]
    [Url(ErrorMessage = "Must be a valid URL")]
    public string VaultUri { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Cache duration must be at least 1 minute")]
    public int CacheDurationMinutes { get; set; } = 60;

    [Range(1, 10, ErrorMessage = "Max retry attempts must be between 1 and 10")]
    public int MaxRetryAttempts { get; set; } = 3;

    [Range(100, 60000, ErrorMessage = "Retry delay must be between 1 and 60 seconds")]
    public int RetryDelayMilliseconds { get; set; } = 1000;

    public bool UseExponentialBackoff { get; set; } = true;
}
