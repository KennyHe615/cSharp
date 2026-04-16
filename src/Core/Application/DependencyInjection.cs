using Application.Abstractions.Normalization;
using Application.Abstractions.Orchestration;
using Application.Behaviors;
using Application.Features.SyncTracking.References;
using Application.Features.SyncTracking.Shared;
using Application.Mediator;
using Application.Normalizers.Genesys;

using FluentValidation;

using Microsoft.Extensions.DependencyInjection;


namespace Application;

public static class DependencyInjection
{
    public static void AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ISimpleMediator, SimpleMediator>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.Scan(scan => scan.FromAssemblies(typeof(DependencyInjection).Assembly)
                                  .AddClasses(classes => classes.AssignableTo(typeof(IRequestHandler<,>)))
                                  .AsImplementedInterfaces()
                                  .WithScopedLifetime());

        services.AddScoped<IReferencesNormalizer, ReferencesNormalizer>();
        services.AddScoped<IUsersDetailsNormalizer, UsersDetailsNormalizer>();

        services.AddScoped<ISyncRunCoordinator, SyncRunCoordinator>();
        services.AddScoped<ISyncRequestRunner, SyncRequestRunner>();
        services.AddScoped<IReferencesSyncOrchestrator, ReferencesSyncOrchestrator>();
        services.AddScoped<ISyncExecutionDispatcher, SyncExecutionDispatcher>();
    }
}
