using Application.Shared.Providers;

using Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace Infrastructure.Azure.KeyVaults;

// TODO: The Key Vaults session is NOT tested yet.
public static class KeyVaultsExtension
{
    public static void AddKeyVaultsSecretProvider(this IServiceCollection services)
    {
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
