using Application;

using FunctionApps.Timer;

using Infrastructure;
using Infrastructure.Observability;

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;


FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile("appsettings.json", true, true)
       .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
       .AddEnvironmentVariables();

builder.ConfigureFunctionsWebApplication();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationInsightsForFunctions("FunctionApps");
builder.Services.AddScoped<IReferencesTimerRunner, ReferencesTimerRunner>();

builder.Build()
       .Run();
