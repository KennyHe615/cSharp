using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using FunctionApp.Configuration.Options;


namespace FunctionApp.Infrastructure.KeyVault;

internal static class KeyVaultClientFactory
{
    public static SecretClient Create(KeyVaultOptions options)
    {
        string? clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        string? clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        string? tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

        Console.WriteLine("========== Key Vault Auth Debug ==========");
        Console.WriteLine($"AZURE_CLIENT_ID: {clientId ?? "NOT SET"}");
        Console.WriteLine($"AZURE_CLIENT_SECRET: {clientSecret ?? "NOT SET"}");
        Console.WriteLine($"AZURE_TENANT_ID: {tenantId ?? "NOT SET"}");
        Console.WriteLine("==========================================");

        DefaultAzureCredential credential = new();

        SecretClientOptions clientOptions = new()
                                            {
                                                Retry =
                                                {
                                                    Mode = options.UseExponentialBackoff
                                                        ? Azure.Core.RetryMode.Exponential
                                                        : Azure.Core.RetryMode.Fixed,
                                                    Delay = TimeSpan.FromMilliseconds(
                                                        options.RetryDelayMilliseconds),
                                                    MaxRetries = options.MaxRetryAttempts
                                                }
                                            };

        return new SecretClient(new Uri(options.VaultUri), credential, clientOptions);
    }
}
