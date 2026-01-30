namespace Infrastructure.ExternalServices.FlurlHttp;

/// <summary>
/// Abstraction over an HTTP client used by infrastructure external service adapters.
/// Implementations typically execute requests via resiliency policies (e.g., Polly)
/// and deserialize JSON responses.
/// </summary>
public interface IFlurlHttpClient
{
    /// <summary>
    /// Gets the base URL configured for the underlying HTTP client.
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Sends a GET request to the specified endpoint and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="T">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    Task<T?> GetAsync<T>(string endpoint,
                         Dictionary<string, string>? headers = null,
                         CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a JSON payload and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint,
                                                    TRequest payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a POST request with a URL-encoded payload and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload encoded as application/x-www-form-urlencoded.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    Task<TResponse?> PostUrlEncodedAsync<TResponse>(string endpoint,
                                                    object payload,
                                                    Dictionary<string, string>? headers = null,
                                                    CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PUT request with a JSON payload and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint,
                                                   TRequest payload,
                                                   Dictionary<string, string>? headers = null,
                                                   CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a PATCH request with a JSON payload and deserializes the JSON response.
    /// </summary>
    /// <typeparam name="TRequest">The request payload type.</typeparam>
    /// <typeparam name="TResponse">The JSON response type.</typeparam>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="payload">Payload serialized to JSON request body.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns>The deserialized response, or <c>null</c> if the operation returns <c>null</c>.</returns>
    Task<TResponse?> PatchAsync<TRequest, TResponse>(string endpoint,
                                                     TRequest payload,
                                                     Dictionary<string, string>? headers = null,
                                                     CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns><c>true</c> if the HTTP response indicates success; otherwise, <c>false</c>.</returns>
    Task<bool> DeleteAsync(string endpoint,
                           Dictionary<string, string>? headers = null,
                           CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a HEAD request to the specified endpoint.
    /// </summary>
    /// <param name="endpoint">Relative endpoint appended to <see cref="BaseUrl"/>.</param>
    /// <param name="headers">Optional headers to apply to the request.</param>
    /// <param name="cancellationToken">Cancellation token for the request execution.</param>
    /// <returns><c>true</c> if the HTTP response indicates success; otherwise, <c>false</c>.</returns>
    Task<bool> HeadAsync(string endpoint,
                         Dictionary<string, string>? headers = null,
                         CancellationToken cancellationToken = default);
}
