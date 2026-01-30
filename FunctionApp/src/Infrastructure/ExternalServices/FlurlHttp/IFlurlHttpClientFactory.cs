using Flurl.Http;

using Polly;


namespace Infrastructure.ExternalServices.FlurlHttp;

/// <summary>
/// Factory responsible for creating and caching <see cref="FlurlClient"/> instances and providing
/// shared Polly resiliency policies for HTTP operations.
/// </summary>
public interface IFlurlHttpClientFactory : IDisposable
{
    /// <summary>
    /// Gets an existing <see cref="FlurlClient"/> for the given base URL or creates and configures a new one.
    /// </summary>
    /// <param name="baseUrl">The base URL used as the cache key.</param>
    /// <returns>A configured <see cref="FlurlClient"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="baseUrl"/> is null, empty, or whitespace.</exception>
    public FlurlClient GetOrAddClient(string baseUrl);

    /// <summary>
    /// Gets the shared Polly policy used for safe/idempotent HTTP methods (e.g., GET, HEAD).
    /// </summary>
    public IAsyncPolicy GetSafePolicy();

    /// <summary>
    /// Gets the shared Polly policy used for unsafe/non\-idempotent HTTP methods (e.g., POST, PUT, PATCH, DELETE).
    /// </summary>
    public IAsyncPolicy GetUnsafePolicy();
}
