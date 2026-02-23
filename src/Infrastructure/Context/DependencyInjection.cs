using Application.Abstractions.Context;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Context;

public static class DependencyInjection
{
    public static void AddContext(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ILobContext, LobContext>();

        services.AddScoped<ILobContextAccessor, LobContextAccessor>();
    }
}
