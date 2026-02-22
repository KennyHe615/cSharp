using Flurl.Http;

using Polly;


namespace Infrastructure.ExternalApis.Http;

/// <summary>
/// Provides cached <see cref="FlurlClient"/> instances and shared resiliency policy pipelines
/// for outbound HTTP API calls.
/// </summary>
/// <remarks>
/// Implementations are expected to be long-lived and thread-safe, because they own client caching
/// and policy reuse across requests.
/// </remarks>
public interface IHttpApiClientFactory : IDisposable
{
    /// <summary>
    /// Gets a cached <see cref="FlurlClient"/> for the specified base URL, or creates and configures one.
    /// </summary>
    /// <param name="baseUrl">The absolute base URL used as the cache key.</param>
    /// <returns>A configured <see cref="FlurlClient"/> instance.</returns>
    FlurlClient GetOrAddClient(string baseUrl);

    /// <summary>
    /// Gets the resiliency policy pipeline for safe/idempotent HTTP operations (for example GET and HEAD).
    /// </summary>
    /// <returns>A composed asynchronous Polly policy.</returns>
    IAsyncPolicy GetSafePolicy();

    /// <summary>
    /// Gets the resiliency policy pipeline for unsafe/non-idempotent HTTP operations
    /// (for example POST, PUT, PATCH, DELETE).
    /// </summary>
    /// <returns>A composed asynchronous Polly policy.</returns>
    IAsyncPolicy GetUnsafePolicy();
}
