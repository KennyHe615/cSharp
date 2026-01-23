using FunctionApp.Configuration.Options;
using FunctionApp.Domain.Repositories;
using FunctionApp.Infrastructure.Persistence.DbContext;
using FunctionApp.Infrastructure.Persistence.Repositories;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


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
        services.AddDbContext<FunctionAppDbContext>((sp, options) =>
                                                    {
                                                        DatabaseOptions databaseOptions =
                                                            sp.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                                                        options.UseSqlServer(databaseOptions.ConnectionString,
                                                                             sqlOptions =>
                                                                             {
                                                                                 sqlOptions.EnableRetryOnFailure(
                                                                                     databaseOptions.MaxRetryCount);
                                                                                 sqlOptions.CommandTimeout(
                                                                                     databaseOptions.CommandTimeout);
                                                                             });
                                                        if (databaseOptions.EnableDetailedErrors)
                                                        {
                                                            options.EnableDetailedErrors();
                                                        }

                                                        if (databaseOptions.EnableSensitiveDataLogging)
                                                        {
                                                            options.EnableSensitiveDataLogging();
                                                        }
                                                    });
    }

    private static void AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    #endregion
}
