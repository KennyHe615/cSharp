using Application.Shared.Extensions;

using Configuration;

using Infrastructure.Extensions;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;

using Shared.Extensions;


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
                                                        services.AddConfiguration(context.Configuration);

                                                        // Register AutoMapper
                                                        services.AddAutoMapper(typeof(Infrastructure.AssemblyMarker));

                                                        // Configure infrastructure (consume the options)
                                                        services.AddInfrastructure();

                                                        // Register Application services (e.g., SyncOrchestrator)
                                                        services.AddServices();

                                                        // Register Shared services
                                                        services.AddSharedServices();

                                                        // DI for other services via Scrutor
                                                        services.Scan(scan => scan
                                                                              .FromAssembliesOf(
                                                                                  typeof(Application.AssemblyMarker),
                                                                                  typeof(Infrastructure.AssemblyMarker))
                                                                              .AddClasses(classes =>
                                                                                  classes.Where(type =>
                                                                                      type is
                                                                                      {
                                                                                          IsAbstract: false,
                                                                                          IsNestedPrivate: false
                                                                                      } &&
                                                                                      !type.IsAssignableTo(
                                                                                          typeof(IDisposable)) &&
                                                                                      (type.Namespace?.StartsWith(
                                                                                              "Application") ==
                                                                                          true ||
                                                                                          type.Namespace?.StartsWith(
                                                                                              "Infrastructure") ==
                                                                                          true) &&
                                                                                      type.Name != "FlurlHttpClient" &&
                                                                                      type.Name != "GenesysApiClient" &&
                                                                                      type.Name !=
                                                                                      "GenesysReferencesClient" &&
                                                                                      type.Name !=
                                                                                      "FlurlHttpClientFactory" &&
                                                                                      type.Name !=
                                                                                      "GenesysTokenClient" &&
                                                                                      type.Name !=
                                                                                      "GenesysTokenProvider" &&
                                                                                      !type.IsAssignableTo(
                                                                                          typeof(Exception))))
                                                                              .AsImplementedInterfaces()
                                                                              .WithScopedLifetime());
                                                    })
                                 .Build();

builder.Run();
