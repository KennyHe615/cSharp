using Infrastructure.Configuration;
using Infrastructure.Context;
using Infrastructure.ExternalApis;
using Infrastructure.Identity;
using Infrastructure.Observability;
using Infrastructure.Persistence;
using Infrastructure.Storage;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Concurrency;


namespace Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddConfiguration(configuration);

        services.AddContext();

        services.AddExternalApis(configuration);

        services.AddIdentity();

        services.AddObservability(configuration);

        services.AddPersistence(configuration);

        services.AddStorage(configuration);

        services.AddSingleton<KeyedSemaphoreLock>();
    }
}
