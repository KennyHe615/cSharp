using Azure.Identity;
using Azure.Security.KeyVault.Secrets;

using Configuration.Options;


namespace Infrastructure.Azure.KeyVaults;

internal static class KeyVaultsClientFactory
{
    public static SecretClient Create(KeyVaultsOptions options)
    {
        string? clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
        string? clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
        string? tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

        Console.WriteLine("========== Key Vaults Auth Debug ==========");
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
                                                        ? global::Azure.Core.RetryMode.Exponential
                                                        : global::Azure.Core.RetryMode.Fixed,
                                                    Delay = TimeSpan.FromMilliseconds(options.RetryDelayMilliseconds),
                                                    MaxRetries = options.MaxRetryAttempts
                                                }
                                            };

        return new SecretClient(new Uri(options.VaultUri), credential, clientOptions);
    }
}
