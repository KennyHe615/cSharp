using Application.Shared.Repositories;

using Infrastructure.Persistence.FunctionAppDbContext;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Repositories;

using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Extensions;

public static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services)
    {
        // NOT calling options.UseSqlServer(...) here because the
        // connection string varies per LOB and is resolved dynamically
        // inside the FunctionAppDbContext.OnConfiguring method.
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<FunctionAppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
