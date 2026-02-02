using Application.References.Handlers;
using Application.Shared.Interfaces;
using Application.Shared.Services;

using Microsoft.Extensions.DependencyInjection;


namespace Application.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        // Register SyncOrchestrator as a Singleton to track and manage
        // active LOB sync jobs across function invocations.
        services.AddSingleton<ISyncOrchestrator, SyncOrchestrator>();

        // Category handlers
        services.AddScoped<ISyncCategoryHandler, SkillSyncHandler>();
        // services.AddScoped<ISyncCategoryHandler, ReferencesSyncHandler>();
    }
}
