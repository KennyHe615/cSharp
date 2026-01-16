using Microsoft.Extensions.Hosting;


var builder = Host.CreateDefaultBuilder(args);

builder.ConfigureFunctionsWorkerDefaults();

// Add Application Insights
// builder.Services.AddApplicationInsightsTelemetryWorkerService()
//        .ConfigureFunctionsApplicationInsights();

builder.Build()
       .Run();
