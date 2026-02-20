using Infrastructure.Observability;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationInsightsForWorker("AppService.Crc");

IHost host = builder.Build();
host.Run();
