using Infrastructure.Observability;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationInsightsForWorker("AppService.Ntt");

IHost host = builder.Build();
host.Run();
