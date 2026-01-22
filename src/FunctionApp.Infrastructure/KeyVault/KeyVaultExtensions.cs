using FunctionApp.Application.Shared.Secrets;
using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.KeyVault;

public static class KeyVaultExtensions
{
    public static void AddKeyVaultSecretProvider(this IServiceCollection services)
    {
        services.AddSingleton(sp =>
                              {
                                  KeyVaultOptions options = sp.GetRequiredService<IOptions<KeyVaultOptions>>().Value;

                                  return KeyVaultClientFactory.Create(options);
                              });

        services.AddScoped<KeyVaultSecretProvider>();

        services.AddScoped<ISecretProvider>(sp =>
                                            {
                                                KeyVaultSecretProvider innerProvider =
                                                    sp.GetRequiredService<KeyVaultSecretProvider>();

                                                IMemoryCache cache = sp.GetRequiredService<IMemoryCache>();

                                                IOptions<KeyVaultOptions> options =
                                                    sp.GetRequiredService<IOptions<KeyVaultOptions>>();

                                                ILogger<KeyVaultSecretCache> logger =
                                                    sp.GetRequiredService<ILogger<KeyVaultSecretCache>>();

                                                return new KeyVaultSecretCache(innerProvider, cache, options, logger);
                                            });
    }
}
