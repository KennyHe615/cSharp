using Application;

using Infrastructure;
using Infrastructure.Observability;

using Ntt.Analytics;
using Ntt.Analytics.Scheduling;
using Ntt.Analytics.Workers.Recovery;
using Ntt.Analytics.Workers.UsersDetails;


HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

builder.Configuration.SetBasePath(builder.Environment.ContentRootPath)
       .AddJsonFile("appsettings.json", true, true)
       .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true)
       .AddEnvironmentVariables();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationInsightsForWorker("Ntt.Analytics");

builder.Services.AddOptions<CronOrIntervalOptions>()
       .Bind(builder.Configuration.GetSection(CronOrIntervalOptions.SectionName))
       .ValidateDataAnnotations()
       .ValidateOnStart();

builder.Services.AddScoped<ScheduledWorkerLoopRunner>();
builder.Services.AddScoped<IScheduledWorkerLoop, UsersDetailsScheduledWorkerLoop>();
builder.Services.AddScoped<IScheduledWorkerLoop, RecoveryIntakeMaterializationScheduledWorkerLoop>();

builder.Services.AddScoped<UsersDetailsIncrementalWorker>();
builder.Services.AddScoped<UsersDetailsRecoveryWorker>();
builder.Services.AddScoped<RecoveryIntakeMaterializationWorker>();
builder.Services.AddHostedService<Worker>();

IHost host = builder.Build();
host.Run();
