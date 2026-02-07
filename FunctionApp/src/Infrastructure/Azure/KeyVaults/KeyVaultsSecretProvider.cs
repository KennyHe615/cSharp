using Application.Common.Abstractions.Providers;

using Azure;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// Implementation of <see cref="ISecretProvider"/> that interacts directly with Azure Key Vault using the <see cref="SecretClient"/>.
/// </summary>
/// <param name="secretClient">The Azure SDK client for interacting with Key Vault secrets.</param>
/// <param name="logger">The logger instance.</param>
public sealed class KeyVaultsSecretProvider(SecretClient secretClient,
                                            ILogger<KeyVaultsSecretProvider> logger) : ISecretProvider
{
    /// <inheritdoc />
    /// <exception cref="KeyVaultsException">Thrown when the secret is not found or an error occurs during retrieval.</exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken ct = default)
    {
        string? value = await TryGetSecretAsync(secretName, ct).ConfigureAwait(false);

        return value ?? throw new KeyVaultsException($"Secret '{secretName}' was not found in Key Vault.");
    }

    /// <inheritdoc />
    /// <exception cref="KeyVaultsException">Thrown when an unexpected error occurs during secret retrieval.</exception>
    public async Task<string?> TryGetSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            logger.LogDebug("Trying to retrieve secret '{SecretName}' from Key Vault", secretName);

            Response<KeyVaultSecret> response = await secretClient
                                                      .GetSecretAsync(secretName, cancellationToken: ct)
                                                      .ConfigureAwait(false);

            return response.Value.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            // For "token" handling case, not found is not exceptional for callers that can self-heal.
            return null;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to retrieve secret '{SecretName}' from Key Vault.", secretName);

            throw new KeyVaultsException($"Error retrieving secret '{secretName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex,
                                       "Unexpected failure retrieving secret '{SecretName}' from Key Vault.",
                                       secretName);

            throw new KeyVaultsException($"Unexpected error retrieving secret '{secretName}' from Key Vault.", ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
    /// <exception cref="KeyVaultsException">Thrown when the upsert operation fails.</exception>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            logger.LogInformation("Upserting secret '{SecretName}' in Key Vault", secretName);

            await secretClient.SetSecretAsync(secretName, value, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to upsert secret '{SecretName}'.", secretName);

            throw new KeyVaultsException($"Error upserting secret '{secretName}' in Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "Unexpected failure upserting secret '{SecretName}'.", secretName);

            throw new KeyVaultsException($"Unexpected error upserting secret '{secretName}' in Key Vault.", ex);
        }
    }

    /// <inheritdoc />
    /// <exception cref="KeyVaultsException">Thrown when the delete operation fails.</exception>
    public async Task DeleteSecretAsync(string secretName, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        try
        {
            logger.LogInformation("Deleting secret '{SecretName}' from Key Vault", secretName);

            await secretClient.StartDeleteSecretAsync(secretName, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (KeyVaultsException)
        {
            throw;
        }
        catch (RequestFailedException ex)
        {
            logger.LogErrorWithDetails(ex, "Failed to delete secret '{SecretName}'.", secretName);

            throw new KeyVaultsException($"Error deleting secret '{secretName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "Unexpected failure deleting secret '{SecretName}'.", secretName);

            throw new KeyVaultsException($"Unexpected error deleting secret '{secretName}' from Key Vault.", ex);
        }
    }
}
