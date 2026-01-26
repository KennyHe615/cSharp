using FunctionApp.Application.Shared.Context;
using FunctionApp.Domain.Repositories;
using FunctionApp.Infrastructure.Persistence.DbContext;
using FunctionApp.Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.DependencyInjection;


namespace FunctionApp.Infrastructure.Extensions;

public static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services)
    {
        services.AddDatabase();

        services.AddUnitOfWork();
    }

    #region ========== *** Private Methods *** ==========

    private static void AddDatabase(this IServiceCollection services)
    {
        // 1. Register the LOB Context (Scoped) to hold the current LOB state
        services.AddScoped<ILobContext, LobContext>();

        // 2. Register the DbContext.
        // We no longer call options.UseSqlServer(...) here because the
        // connection string varies per LOB and is resolved dynamically
        // inside the FunctionAppDbContext.OnConfiguring method.
        services.AddDbContext<FunctionAppDbContext>();
    }

    private static void AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    #endregion
}
