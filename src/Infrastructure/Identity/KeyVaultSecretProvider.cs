using Application.Abstractions.Identity;

using Azure;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;

using SharedKernel.Logging;


namespace Infrastructure.Identity;

/// <summary>
/// Azure Key Vault implementation of <see cref="ISecretProvider"/>.
/// </summary>
/// <param name="secretClient">Azure SDK client used to interact with Key Vault secrets.</param>
/// <param name="logger">Logger instance.</param>
public sealed class KeyVaultSecretProvider(SecretClient secretClient,
                                           ILogger<KeyVaultSecretProvider> logger) : ISecretProvider
{
    /// <inheritdoc />
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when the secret is not found or retrieval fails.
    /// </exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        string? value = await TryGetSecretAsync(secretName, ct).ConfigureAwait(false);

        return value ?? throw new KeyVaultSecretException($"Secret '{secretName}' was not found in Key Vault.");
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when Key Vault access fails.
    /// </exception>
    public async Task<string?> TryGetSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        try
        {
            logger.LogDebug("Trying to retrieve secret '{SecretName}' from Key Vault.", secretName);

            Response<KeyVaultSecret> response = await secretClient
                                                     .GetSecretAsync(secretName, cancellationToken: ct)
                                                     .ConfigureAwait(false);

            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // Missing secret is a valid outcome for TryGet semantics.
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to retrieve secret '{SecretName}' from Key Vault.", secretName);

            throw new KeyVaultSecretException($"Error retrieving secret '{secretName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex,
                                       "Unexpected failure retrieving secret '{SecretName}' from Key Vault.",
                                       secretName);

            throw new KeyVaultSecretException($"Unexpected error retrieving secret '{secretName}' from Key Vault.", ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="value"/> is null.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when upsert fails.
    /// </exception>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            logger.LogInformation("Upserting secret '{SecretName}' in Key Vault.", secretName);

            await secretClient.SetSecretAsync(secretName, value, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to upsert secret '{SecretName}'.", secretName);

            throw new KeyVaultSecretException($"Error upserting secret '{secretName}' in Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "Unexpected failure upserting secret '{SecretName}'.", secretName);

            throw new KeyVaultSecretException($"Unexpected error upserting secret '{secretName}' in Key Vault.", ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="secretName"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="KeyVaultSecretException">
    /// Thrown when delete fails.
    /// </exception>
    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(secretName))
        {
            throw new ArgumentException("Secret name cannot be null, empty, or whitespace.", nameof(secretName));
        }

        try
        {
            logger.LogInformation("Deleting secret '{SecretName}' from Key Vault.", secretName);

            await secretClient.StartDeleteSecretAsync(secretName, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to delete secret '{SecretName}'.", secretName);

            throw new KeyVaultSecretException($"Error deleting secret '{secretName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "Unexpected failure deleting secret '{SecretName}'.", secretName);

            throw new KeyVaultSecretException($"Unexpected error deleting secret '{secretName}' from Key Vault.", ex);
        }
    }
}
