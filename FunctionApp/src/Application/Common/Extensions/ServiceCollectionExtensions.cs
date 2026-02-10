using Application.Common.Abstractions.Services;
using Application.Common.Services;

using Microsoft.Extensions.DependencyInjection;


namespace Application.Common.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // Register SyncOrchestrator as a Singleton to track and manage
        // active LOB sync jobs across function invocations.
        services.AddSingleton<ISyncOrchestrator, SyncOrchestrator>();

        services.AddScoped<IIntervalSubdivisionService, IntervalSubdivisionService>();
    }
}
