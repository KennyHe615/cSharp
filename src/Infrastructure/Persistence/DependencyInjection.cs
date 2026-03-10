using Application.Abstractions.Persistence;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Interceptors;
using Infrastructure.Persistence.Mappers;
using Infrastructure.Persistence.Repositories.References;
using Infrastructure.Persistence.Repositories.SyncTracking;
using Infrastructure.Persistence.Repositories.UserDetails;
using Infrastructure.Time;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using SharedKernel.Time;


namespace Infrastructure.Persistence;

public static class DependencyInjection
{
    public static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();

        services.AddAutoMapper(typeof(MappingAssemblyMarker));

        // Connection string comes from ILobContext at runtime in AppDbContext.OnConfiguring.
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IReferencesRepository, ReferencesRepository>();
        services.AddScoped<IUserDetailsRepository, UserDetailsRepository>();

        services.AddScoped<ISyncRequestRepository, SyncRequestRepository>();
        services.AddScoped<ISyncRunRepository, SyncRunRepository>();
        services.AddScoped<ISyncCheckpointRepository, SyncCheckpointRepository>();
    }
}
