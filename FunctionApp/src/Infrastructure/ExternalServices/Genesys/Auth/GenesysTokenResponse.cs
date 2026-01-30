using System.Text.Json.Serialization;


namespace Infrastructure.ExternalServices.Genesys.Auth;

/// <summary>
/// Represents the OAuth token response from Genesys Cloud API.
/// Used for deserializing the token endpoint response after client credentials authentication.
/// </summary>
/// <remarks>
/// Response format from POST https://login.{region}.pure.cloud/oauth/token?grant_type=client_credentials
/// with Basic authentication header containing base64-encoded client credentials.
/// </remarks>
public class GenesysTokenResponse
{
    /// <summary>
    /// The OAuth 2.0 access token used for authenticating API requests to Genesys Cloud.
    /// Should be included in the Authorization header as "Bearer {token}".
    /// </summary>
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    /// <summary>
    /// The type of token issued, typically "bearer".
    /// </summary>
    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; } = "bearer";

    /// <summary>
    /// The lifetime in seconds of the access token.
    /// For example, a value of 86400 indicates the token expires in 24 hours.
    /// </summary>
    /// <remarks>
    /// Tokens are typically cached with a safety margin (e.g., ExpiresIn - 300 seconds)
    /// to prevent usage of tokens near expiration.
    /// </remarks>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; } = 86399;
}
