using Shared.Providers;

using Microsoft.Extensions.DependencyInjection;


namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
