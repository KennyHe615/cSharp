namespace Application.Abstractions.Identity;

/// <summary>
/// Abstraction for secure secret operations used by infrastructure services.
/// </summary>
public interface ISecretProvider
{
    /// <summary>
    /// Retrieves the value of a secret.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The secret value.</returns>
    public Task<string> GetSecretAsync(string secretName, CancellationToken ct = default);

    /// <summary>
    /// Attempts to retrieve the value of a secret, returning <c>null</c> if it does not exist.
    /// </summary>
    /// <param name="secretName">The name of the secret to retrieve.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The secret value if found; otherwise, <c>null</c>.</returns>
    Task<string?> TryGetSecretAsync(string secretName, CancellationToken ct = default);

    /// <summary>
    /// Creates or updates a secret with the specified value.
    /// </summary>
    /// <param name="secretName">The name of the secret to upsert.</param>
    /// <param name="value">The value to store.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task UpsertSecretAsync(string secretName, string value, CancellationToken ct = default);

    /// <summary>
    /// Deletes a secret from the secure storage.
    /// </summary>
    /// <param name="secretName">The name of the secret to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DeleteSecretAsync(string secretName, CancellationToken ct = default);
}
