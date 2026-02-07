using Application.Common.Abstractions.Sync;
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

        // Category handlers
        // services.AddScoped<ISyncCategoryHandler, SkillSyncHandler>();
        // services.AddScoped<ISyncCategoryHandler, PresenceDefinitionSyncHandler>();
        // services.AddScoped<ISyncCategoryHandler, ReferencesSyncHandler>();
    }
}
