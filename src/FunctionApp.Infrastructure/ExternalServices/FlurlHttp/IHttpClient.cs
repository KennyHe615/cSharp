namespace FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

/// <summary>
/// Interface for HTTP client operations (calling external services)
/// </summary>
public interface IHttpClient
{
    /// <summary>
    /// Gets the configured base URL for services
    /// </summary>
    string BaseUrl { get; }

    /// <summary>
    /// Send GET request to an external service with circuit breaker protection
    /// </summary>
    Task<T?> GetAsync<T>(string endpoint, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send POST request to external service with circuit breaker protection
    /// </summary>
    Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send PUT request to external service with circuit breaker protection
    /// </summary>
    Task<TResponse?> PutAsync<TRequest, TResponse>(string endpoint, TRequest payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send DELETE request to external service with circuit breaker protection
    /// </summary>
    Task<bool> DeleteAsync(string endpoint, CancellationToken cancellationToken = default);
}
