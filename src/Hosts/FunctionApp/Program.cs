using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Hosting;

using Infrastructure.Configuration;
using Infrastructure.Observability;


FunctionsApplicationBuilder builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplicationInsightsForFunctions("FunctionApp");

builder.Build().Run();
