using Application.Shared.Providers;

using Azure;
using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.Logging;

using Shared.Extensions;


namespace Infrastructure.Azure.KeyVaults;

public sealed class KeyVaultsSecretProvider(SecretClient secretClient,
                                            ILogger<KeyVaultsSecretProvider> logger) : ISecretProvider
{
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogDebug("Retrieving secret '{SecretName}' from Key Vault", secretName);

            Response<KeyVaultSecret> response =
                await secretClient.GetSecretAsync(secretName, cancellationToken: cancellationToken);

            return response.Value.Value;
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                            "Failed to retrieve secret '{SecretName}' from Key Vault. Exception: {ExceptionJson}",
                            secretName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error retrieving secret '{secretName}' from Key Vault.", ex);
        }
    }

    public async Task UpsertSecretAsync(string secretName, string value, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Upserting secret '{SecretName}' in Key Vault", secretName);
            await secretClient.SetSecretAsync(secretName, value, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                            "Failed to upsert secret '{SecretName}'. Exception: {ExceptionJson}",
                            secretName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error upserting secret '{secretName}' in Key Vault.", ex);
        }
    }

    public async Task DeleteSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Deleting secret '{SecretName}' from Key Vault", secretName);
            await secretClient.StartDeleteSecretAsync(secretName, cancellationToken);
        }
        catch (RequestFailedException ex)
        {
            logger.LogError(ex,
                            "Failed to delete secret '{SecretName}'. Exception: {ExceptionJson}",
                            secretName,
                            ex.ToJson());

            throw new KeyVaultsException($"Error deleting secret '{secretName}' from Key Vault.", ex);
        }
    }
}
