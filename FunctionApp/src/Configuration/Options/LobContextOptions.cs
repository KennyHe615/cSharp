using System.ComponentModel.DataAnnotations;


namespace Configuration.Options;

public sealed class LobContextOptions : Dictionary<string, LobSettings>
{
    public const string SectionName = "LobContext";
}

public sealed class LobSettings
{
    [Required(ErrorMessage = "Genesys Client ID is required")]
    public string GenesysClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Genesys Client Secret is required")]
    public string GenesysClientSecret { get; set; } = string.Empty;

    [Required(ErrorMessage = "Database Connection String is required")]
    public string DatabaseConnectionString { get; set; } = string.Empty;
}
