using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Configuration.Options;

using Shared.Constants;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// Factory responsible for creating configured instances of <see cref="SecretClient"/> for Azure Key Vault access.
/// </summary>
/// <remarks>
/// This factory validates configuration values and applies retry settings based on <see cref="KeyVaultsOptions"/>.
/// Authentication is handled via <see cref="DefaultAzureCredential"/>.
/// </remarks>
internal static class KeyVaultsClientFactory
{
    private const string KvUri = KeyVaultsConstants.Uri;

    /// <summary>
    /// Creates a new <see cref="SecretClient"/> for the configured Key Vault.
    /// </summary>
    /// <param name="options">
    /// Key Vault client configuration, including the vault URI and retry settings.
    /// </param>
    /// <returns>
    /// A configured <see cref="SecretClient"/> instance.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/> is <see langword="null"/>.
    /// </exception>
    /// <exception cref="KeyVaultsException">
    /// Thrown when <see cref="KeyVaultsConstants.Uri"/> is missing or invalid, or when client creation fails.
    /// </exception>
    public static SecretClient Create(KeyVaultsOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(KvUri))
        {
            throw new KeyVaultsException("Key Vault configuration error: KeyVaults.Uri must be provided.");
        }

        if (!Uri.TryCreate(KvUri, UriKind.Absolute, out Uri? vaultUri))
        {
            throw new KeyVaultsException(
                $"Key Vault configuration error: KeyVaults.Uri is not a valid absolute URI. Value: `{KvUri}`");
        }

        int delayMs = Math.Max(0, options.RetryDelayMilliseconds);
        int maxRetries = Math.Max(0, options.MaxRetryAttempts);

        DefaultAzureCredential credential = new();

        SecretClientOptions clientOptions = new()
                                            {
                                                Retry =
                                                {
                                                    Mode = options.UseExponentialBackoff
                                                        ? RetryMode.Exponential
                                                        : RetryMode.Fixed,
                                                    Delay = TimeSpan.FromMilliseconds(delayMs),
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
            throw new KeyVaultsException(
                "Failed to create Azure Key Vault SecretClient. Check VaultUri and Azure credential configuration.",
                ex);
        }
    }
}
