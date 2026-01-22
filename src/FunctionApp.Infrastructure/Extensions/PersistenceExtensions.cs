using FunctionApp.Configuration.Options;
using FunctionApp.Infrastructure.Persistence.DbContext;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;


namespace FunctionApp.Infrastructure.Extensions;

public static class PersistenceExtensions
{
    public static void AddPersistence(this IServiceCollection services)
    {
        services.AddDatabase();
        // services.AddRepositories(); // We can add this later
    }

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
}
