using System.Text.Json.Serialization;


namespace Infrastructure.ExternalApis.Genesys.Models.Auth;

/// <summary>
/// Represents the OAuth token response from Genesys Cloud API.
/// Used for deserializing the token endpoint response after client credentials authentication.
/// </summary>
public sealed class GenesysTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; init; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; } = "bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; } = 86400;
}
