using System.ComponentModel.DataAnnotations;


namespace FunctionApp.Configuration.Options;

/// <summary>
/// Contains only the shared LOB settings.
/// Individual LOBs are discovered dynamically at runtime.
/// </summary>
public sealed class MultiLobOptions
{
    public const string SectionName = "Lobs";

    // Shared LOB Defaults
    [Url(ErrorMessage = "Must be a valid URL")]
    public string GenesysOAuthEndpoint { get; set; } = string.Empty;

    [Url(ErrorMessage = "Must be a valid URL")]
    public string GenesysApiEndpoint { get; set; } = string.Empty;

    [Range(1, 10, ErrorMessage = "Max retry count must be between 1 and 10")]
    public int DatabaseMaxRetryCount { get; set; } = 3;

    [Range(5, 300, ErrorMessage = "Command timeout must be between 5 and 300 seconds")]
    public int DatabaseCommandTimeout { get; set; } = 30;

    public bool DatabaseEnableDetailedErrors { get; set; } = false;

    public bool DatabaseEnableSensitiveDataLogging { get; set; } = false;
}

public sealed class LobSettings
{
    public string GenesysClientId { get; set; } = string.Empty;

    public string GenesysClientSecret { get; set; } = string.Empty;

    public string DatabaseConnectionString { get; set; } = string.Empty;
}
