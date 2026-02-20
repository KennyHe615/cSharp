using System.ComponentModel.DataAnnotations;


namespace Infrastructure.Persistence;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";

    [Range(1, 10, ErrorMessage = "Max retry count must be between 1 and 10")]
    public int MaxRetryCount { get; set; } = 3;

    [Range(5, 300, ErrorMessage = "Command timeout must be between 5 and 300 seconds")]
    public int CommandTimeout { get; set; } = 30;

    public bool EnableDetailedErrors { get; set; } = false;

    public bool EnableSensitiveDataLogging { get; set; } = false;
}
