using FunctionApp.Configuration;
using FunctionApp.Infrastructure.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using FunctionApp.Infrastructure;


IHost builder = new HostBuilder().ConfigureFunctionsWorkerDefaults()
                                 .ConfigureAppConfiguration((context, configBuilder) =>
                                                            {
                                                                string env = context.HostingEnvironment.EnvironmentName;

                                                                // Always load local settings in Development
                                                                configBuilder.AddJsonFile(env == "Development"
                                                                        ? "local.settings.json"
                                                                        // Load environment-specific appsettings
                                                                        : $"appsettings.{env}.json",
                                                                    true,
                                                                    true);

                                                                // Always load environment variables
                                                                configBuilder.AddEnvironmentVariables();
                                                            })
                                 .ConfigureServices((context, services) =>
                                                    {
                                                        // Bind configuration to strongly-typed options
                                                        services.AddFunctionAppConfiguration(context.Configuration);

                                                        // Configure infrastructure (consume the options)
                                                        services.AddInfrastructureServices();

                                                        // DI for other services via Scrutor
                                                        services.Scan(scan => scan
                                                                              .FromAssembliesOf(typeof(AssemblyMarker))
                                                                              .AddClasses(classes =>
                                                                                  classes.Where(type =>
                                                                                      !type.IsAbstract &&
                                                                                      !type.IsAssignableTo(
                                                                                          typeof(Exception))))
                                                                              .AsImplementedInterfaces()
                                                                              .WithScopedLifetime());
                                                    })
                                 .Build();

builder.Run();
