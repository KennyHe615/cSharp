using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddInfrastructureServices(this IServiceCollection services)
    {
        services.AddApplicationInsights();
        services.AddFlurlHttpClient();
        // services.AddBlobStorageClient();
        // services.AddRepositories();
    }
}
