using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddApplicationInsightsCustom();
        // services.AddBlobStorageClient();
        // services.AddFlurlClients();
        // services.AddRepositories();
    }
}
