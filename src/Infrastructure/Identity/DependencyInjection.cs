using Application.Abstractions.Identity;

using Azure.Security.KeyVault.Secrets;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SharedKernel.Environment;


namespace Infrastructure.Identity;

public static class DependencyInjection
{
    public static void AddIdentity(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddMemoryCache();

        services.AddSingleton<IKeyVaultSecretClientFactory, KeyVaultSecretClientFactory>();

        services.AddSingleton<SecretClient>(sp => sp.GetRequiredService<IKeyVaultSecretClientFactory>().Create());

        services.AddSingleton(sp =>
                              {
                                  IHostEnvironment hostEnvironment = sp.GetRequiredService<IHostEnvironment>();

                                  return AppEnvironment.FromHostEnvironment(hostEnvironment.EnvironmentName);
                              });

        services.AddScoped<KeyVaultSecretProvider>();
        services.AddScoped<ISecretProvider, CachedKeyVaultSecretProvider>();

        services.AddScoped<ICredentialProvider, LobCredentialProvider>();
    }
}
