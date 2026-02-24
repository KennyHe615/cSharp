using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Infrastructure.Configuration.Options;

using Microsoft.Extensions.Options;


namespace Infrastructure.Identity;

public sealed class KeyVaultSecretClientFactory(IOptions<KeyVaultOptions> options) : IKeyVaultSecretClientFactory
{
    public SecretClient Create()
    {
        KeyVaultOptions value = options.Value;

        if (!Uri.TryCreate(value.Uri, UriKind.Absolute, out Uri? vaultUri))
        {
            throw new
                KeyVaultSecretException($"Key Vault configuration error: '{KeyVaultOptions.SectionName}:Uri' must be a valid absolute URI.");
        }

        DefaultAzureCredential credential = new DefaultAzureCredential();

        int delayMs = Math.Max(0, value.RetryDelayMilliseconds);
        int maxRetries = Math.Max(0, value.MaxRetryAttempts);

        SecretClientOptions clientOptions = new SecretClientOptions
                                            {
                                                Retry =
                                                {
                                                    Mode = value.UseExponentialBackoff
                                                        ? RetryMode.Exponential
                                                        : RetryMode.Fixed,
                                                    Delay =
                                                        TimeSpan
                                                           .FromMilliseconds(delayMs),
                                                    MaxDelay = TimeSpan.FromSeconds(30),
                                                    MaxRetries = maxRetries
                                                }
                                            };

        try
        {
            return new SecretClient(vaultUri, credential, clientOptions);
        }
        catch (Exception ex)
        {
            throw new
                KeyVaultSecretException("Failed to create Azure Key Vault SecretClient. Check VaultUri and Azure credential configuration.",
                                        ex);
        }
    }
}
