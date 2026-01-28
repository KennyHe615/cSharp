using System.ComponentModel.DataAnnotations;


namespace Configuration.Options;

public sealed class GenesysOptions
{
    public const string SectionName = "Genesys";

    [Required(ErrorMessage = "Genesys OAuth endpoint is required")]
    [Url(ErrorMessage = "Genesys OAuth endpoint must be a valid URL")]
    public string OAuthEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Genesys API endpoint is required")]
    [Url(ErrorMessage = "Genesys API endpoint must be a valid URL")]
    public string ApiEndpoint { get; set; } = string.Empty;
}
