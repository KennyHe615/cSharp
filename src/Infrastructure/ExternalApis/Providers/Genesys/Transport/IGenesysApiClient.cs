namespace Infrastructure.ExternalApis.Providers.Genesys.Transport;

/// <summary>
/// Authenticated Genesys API client abstraction.
///
/// This contract is a Genesys-specific wrapper over the shared HTTP pipeline:
/// - attaches bearer token automatically,
/// - delegates resilience behavior (retry/circuit-breaker) to shared HTTP policies,
/// - refreshes token on 401 via provider callback.
/// </summary>
public interface IGenesysApiClient
{
    /// <summary>
    /// Gets the configured Genesys API base URL.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Sends an authenticated GET request and deserializes the JSON response body.
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated POST request with JSON payload and deserializes the JSON response body.
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                    TRequest payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated POST request with URL-encoded payload and deserializes the JSON response body.
    /// </summary>
    Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                    object payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated PUT request with JSON payload and deserializes the JSON response body.
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                   TRequest payload,
                                                   Dictionary<string, string>? headers = null,
                                                   CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated PATCH request with JSON payload and deserializes the JSON response body.
    /// </summary>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                     TRequest payload,
                                                     Dictionary<string, string>? headers = null,
                                                     CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated DELETE request.
    /// </summary>
    Task DeleteAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);

    /// <summary>
    /// Sends an authenticated HEAD request.
    /// </summary>
    Task HeadAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);
}
