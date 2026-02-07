using Microsoft.Extensions.DependencyInjection;

using Shared.Time;


namespace Shared.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
    }
}
