using Microsoft.Extensions.DependencyInjection;


namespace Application.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // Register SyncOrchestrator as a Singleton to track and manage
        // active LOB sync jobs across function invocations.
        // services.AddSingleton<SyncOrchestrator>();
    }
}
