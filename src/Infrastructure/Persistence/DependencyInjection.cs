using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Interceptors;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


namespace Infrastructure.Persistence;

public static class DependencyInjection
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
           .AddOptions<DatabaseOptions>()
           .Bind(configuration.GetSection(DatabaseOptions.SectionName))
           .ValidateDataAnnotations()
           .ValidateOnStart();

        // Connection string comes from ILobContext at runtime in AppDbContext.OnConfiguring.
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>();

        // Enable after UnitOfWork implementation is active.
        // services.AddScoped<IUnitOfWork, UnitOfWork>();
    }
}
