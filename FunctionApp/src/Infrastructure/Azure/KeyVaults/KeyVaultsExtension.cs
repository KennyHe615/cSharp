using Application.Shared.Providers;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Azure.Security.KeyVault.Secrets;


namespace Infrastructure.Azure.KeyVaults;

/// <summary>
/// DI extensions for registering Azure Key Vault secret providers.
/// </summary>
public static class KeyVaultsExtension
{
    /// <summary>
    /// Registers the Key Vault client and secret provider stack:
    /// <list type="bullet">
    /// <item><description><see cref="SecretClient"/> as singleton.</description></item>
    /// <item><description><see cref="KeyVaultsSecretProvider"/> as scoped.</description></item>
    /// <item><description><see cref="ISecretProvider"/> as scoped, decorated with <see cref="KeyVaultsSecretCache"/>.</description></item>
    /// </list>
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    public static void AddKeyVaultsSecretProvider(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(sp =>
                              {
                                  KeyVaultsOptions options = sp.GetRequiredService<IOptions<KeyVaultsOptions>>().Value;

                                  return KeyVaultsClientFactory.Create(options);
                              });

        services.AddScoped<KeyVaultsSecretProvider>();

        services.AddScoped<ISecretProvider>(sp =>
                                            {
                                                KeyVaultsSecretProvider innerProvider =
                                                    sp.GetRequiredService<KeyVaultsSecretProvider>();

                                                IMemoryCache cache = sp.GetRequiredService<IMemoryCache>();

                                                IOptions<KeyVaultsOptions> options =
                                                    sp.GetRequiredService<IOptions<KeyVaultsOptions>>();

                                                ILogger<KeyVaultsSecretCache> logger =
                                                    sp.GetRequiredService<ILogger<KeyVaultsSecretCache>>();

                                                return new KeyVaultsSecretCache(innerProvider, cache, options, logger);
                                            });
    }
}
