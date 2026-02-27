using Polly;


namespace Infrastructure.ExternalApis.Abstractions;

/// <summary>
/// Provides resiliency policy pipelines for outbound HTTP calls.
/// </summary>
public interface IHttpResiliencePolicyFactory
{
    /// <summary>
    /// Creates the policy pipeline for safe/idempotent HTTP operations
    /// (for example GET and HEAD).
    /// </summary>
    /// <returns>A composed asynchronous Polly policy.</returns>
    IAsyncPolicy CreateSafePolicy();

    /// <summary>
    /// Creates the policy pipeline for unsafe/non-idempotent HTTP operations
    /// (for example POST, PUT, PATCH, DELETE).
    /// </summary>
    /// <returns>A composed asynchronous Polly policy.</returns>
    IAsyncPolicy CreateUnsafePolicy();
}
