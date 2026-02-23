using Infrastructure.Configuration;
using Infrastructure.Context;
using Infrastructure.ExternalApis;
using Infrastructure.Identity;
using Infrastructure.Observability;
using Infrastructure.Persistence;
using Infrastructure.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddConfiguration(configuration);

        services.AddContext();

        services.AddExternalApis(configuration);

        services.AddIdentity(configuration);

        services.AddObservability(configuration);

        services.AddPersistence(configuration);

        services.AddStorage(configuration);
    }
}
