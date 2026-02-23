using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Identity;

public static class DependencyInjection
{
    public static void AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<LobOptions>().Bind(configuration.GetSection("Lobs"));

        services.AddScoped<AzureManagedIdentityCredentialProvider>();

        services.AddScoped<LobCredentialProvider>();
    }
}
