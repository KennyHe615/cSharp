using Domain.Repositories;

using Infrastructure.Persistence.DbContext;
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
        services.AddDbContext<FunctionAppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
