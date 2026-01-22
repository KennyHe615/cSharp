using System.ComponentModel.DataAnnotations;


namespace FunctionApp.Configuration.Options;

public sealed class GenesysOptions
{
    public const string SectionName = "Genesys";

    [Required(ErrorMessage = "OAuth endpoint is required")]
    [Url(ErrorMessage = "Must be a valid URL")]
    public string OAuthEndpoint { get; set; } = string.Empty;

    [Required(ErrorMessage = "Client ID is required")]
    public string ClientId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Client secret is required")]
    public string ClientSecret { get; set; } = string.Empty;

    [Required(ErrorMessage = "API endpoint is required")]
    [Url(ErrorMessage = "Must be a valid URL")]
    public string ApiEndpoint { get; set; } = string.Empty;
}
