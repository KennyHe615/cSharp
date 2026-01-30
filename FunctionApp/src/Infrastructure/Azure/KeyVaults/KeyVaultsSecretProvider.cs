using Application.Shared.Providers;

using Azure;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// Key Vault implementation of <see cref="ISecretProvider"/> using <see cref="SecretClient"/>.
/// </summary>
/// <remarks>
/// This provider normalizes secret names via <see cref="StringExtensions.NormalizeSecretName(string?)"/> to comply with
/// Azure Key Vault naming requirements, and it normalizes failures by throwing <see cref="KeyVaultsException"/> for
/// Key Vault related errors.
/// </remarks>
public sealed class KeyVaultsSecretProvider(SecretClient secretClient,
                                            ILogger<KeyVaultsSecretProvider> logger) : ISecretProvider
{
    /// <summary>
    /// Retrieves the current value of a secret from Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The secret name to retrieve (will be normalized to Key Vault naming rules).</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The resolved secret value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or invalid after normalization.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="KeyVaultsException">Thrown when Key Vault access fails or an unexpected error occurs.</exception>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        try
        {
            logger.LogDebug("Retrieving secret '{SecretName}' from Key Vault", normalizedName);

            Response<KeyVaultSecret> response = await secretClient
                                                      .GetSecretAsync(normalizedName,
                                                                      cancellationToken: cancellationToken)
                                                      .ConfigureAwait(false);

            return response.Value.Value;
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
            logger.LogError(ex,
                            "Failed to retrieve secret '{SecretName}' from Key Vault. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error retrieving secret '{normalizedName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "Unexpected failure retrieving secret '{SecretName}' from Key Vault. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Unexpected error retrieving secret '{normalizedName}' from Key Vault.", ex);
        }
    }

    /// <summary>
    /// Creates or updates a secret value in Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The secret name to upsert (will be normalized to Key Vault naming rules).</param>
    /// <param name="value">The secret value to store.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or invalid after normalization.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="KeyVaultsException">Thrown when Key Vault access fails or an unexpected error occurs.</exception>
    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        ArgumentNullException.ThrowIfNull(value);

        try
        {
            logger.LogInformation("Upserting secret '{SecretName}' in Key Vault", normalizedName);

            await secretClient.SetSecretAsync(secretName, value, cancellationToken).ConfigureAwait(false);
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
            logger.LogError(ex,
                            "Failed to upsert secret '{SecretName}'. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error upserting secret '{normalizedName}' in Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "Unexpected failure upserting secret '{SecretName}'. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Unexpected error upserting secret '{normalizedName}' in Key Vault.", ex);
        }
    }

    /// <summary>
    /// Starts deletion of a secret in Azure Key Vault.
    /// </summary>
    /// <param name="secretName">The secret name to delete (will be normalized to Key Vault naming rules).</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="secretName"/> is null, empty, or invalid after normalization.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    /// <exception cref="KeyVaultsException">Thrown when Key Vault access fails or an unexpected error occurs.</exception>
    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        string normalizedName = secretName.NormalizeSecretName();

        try
        {
            logger.LogInformation("Deleting secret '{SecretName}' from Key Vault", normalizedName);

            await secretClient.StartDeleteSecretAsync(secretName, cancellationToken).ConfigureAwait(false);
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
            logger.LogError(ex,
                            "Failed to delete secret '{SecretName}'. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error deleting secret '{normalizedName}' from Key Vault.", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                            "Unexpected failure deleting secret '{SecretName}'. Exception: {ExceptionJson}",
                            normalizedName,
                            ex.ToJson());

            throw new KeyVaultsException($"Unexpected error deleting secret '{normalizedName}' from Key Vault.", ex);
        }
    }
}
