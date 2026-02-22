namespace Infrastructure.ExternalApis.Http;

/// <summary>
/// Contract for outbound HTTP operations used by infrastructure external API adapters.
/// </summary>
/// <remarks>
/// Implementations are responsible for applying resiliency, logging, and response deserialization.
/// </remarks>
public interface IHttpApiClient
{
    /// <summary>
    /// Gets the base URL configured for the underlying HTTP client.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Sends a GET request and deserializes the JSON response body.
    /// </summary>
    /// <typeparam name="T">Expected response payload type.</typeparam>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response payload.</returns>
    Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a POST request with JSON payload and deserializes the JSON response body.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TResponse">Response payload type.</typeparam>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="payload">Request payload.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response payload.</returns>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                    TRequest payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Sends a POST request with URL-encoded payload and deserializes the JSON response body.
    /// </summary>
    /// <typeparam name="TResponse">Response payload type.</typeparam>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="payload">URL-encoded request payload object.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response payload.</returns>
    Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                    object payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken ct = default);

    /// <summary>
    /// Sends a PUT request with JSON payload and deserializes the JSON response body.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TResponse">Response payload type.</typeparam>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="payload">Request payload.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response payload.</returns>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                   TRequest payload,
                                                   Dictionary<string, string>? headers = null,
                                                   CancellationToken ct = default);

    /// <summary>
    /// Sends a PATCH request with JSON payload and deserializes the JSON response body.
    /// </summary>
    /// <typeparam name="TRequest">Request payload type.</typeparam>
    /// <typeparam name="TResponse">Response payload type.</typeparam>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="payload">Request payload.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Deserialized response payload.</returns>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                     TRequest payload,
                                                     Dictionary<string, string>? headers = null,
                                                     CancellationToken ct = default);

    /// <summary>
    /// Sends a DELETE request.
    /// </summary>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    Task DeleteAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);

    /// <summary>
    /// Sends a HEAD request.
    /// </summary>
    /// <param name="endpoint">Relative endpoint path.</param>
    /// <param name="headers">Optional request headers.</param>
    /// <param name="ct">Cancellation token.</param>
    Task HeadAsync(string endpoint, Dictionary<string, string>? headers = null, CancellationToken ct = default);
}
