using FunctionApp.Application.Shared.Services;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Application.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddApplicationServices(this IServiceCollection services)
    {
        // Register SyncOrchestrator as a Singleton to track and manage
        // active LOB sync jobs across function invocations.
        services.AddSingleton<SyncOrchestrator>();
    }
}
