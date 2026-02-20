using Application.Abstractions.Context;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Context;

public static class DependencyInjection
{
    public static IServiceCollection AddLobContext(this IServiceCollection services)
    {
        services.AddScoped<ILobContextAccessor, LobContextAccessor>();
        services.AddScoped<ILobContext, LobContext>();

        return services;
    }
}
