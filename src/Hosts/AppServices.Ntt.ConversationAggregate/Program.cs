using Infrastructure.Observability;

using Microsoft.Extensions.Hosting;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);
builder.Services.AddApplicationInsightsForWorker("AppService.Ntt.ConversationAggregate");

IHost host = builder.Build();
host.Run();
