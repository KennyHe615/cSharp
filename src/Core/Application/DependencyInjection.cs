using Application.Abstractions.Orchestration;
using Application.Behaviors;
using Application.Features.SyncTracking;
using Application.Mediator;

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

        services.AddScoped<ISyncRunCoordinator, SyncRunCoordinator>();
        services.AddScoped<ISyncRequestRunner, SyncRequestRunner>();
        services.AddScoped<ISyncExecutionDispatcher, SyncExecutionDispatcher>();
    }
}
