using Infrastructure;
using Infrastructure.Observability;

using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationInsightsForFunctions("FunctionApp");

builder.Build().Run();
