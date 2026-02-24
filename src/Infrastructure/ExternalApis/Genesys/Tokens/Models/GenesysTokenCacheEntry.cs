namespace Infrastructure.ExternalApis.Genesys.Tokens.Models;

/// <summary>
/// In-memory cache payload for a Genesys OAuth token.
/// </summary>
public sealed record GenesysTokenCacheEntry(string AccessToken,
                                            DateTimeOffset ExpiresAtUtc);
