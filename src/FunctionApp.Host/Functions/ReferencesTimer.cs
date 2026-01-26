using FunctionApp.Application.Shared.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Host.Functions;

public class ReferencesTimer(SyncOrchestrator orchestrator, ILogger<ReferencesTimer> logger)
{
    private const string TimerSchedule = "0 */1 * * * *"; // Every 30 minutes

    [Function("Sync-NTT")]
    public Task RunNtt([TimerTrigger(TimerSchedule)] TimerInfo timer, CancellationToken ct)
    {
        logger.LogInformation("NTT Sync Triggered.");

        return orchestrator.ExecuteSyncAsync("ntt", ct);
    }

    [Function("Sync-LCL")]
    public Task RunLcl([TimerTrigger(TimerSchedule)] TimerInfo timer, CancellationToken ct)
    {
        logger.LogInformation("LCL Sync Triggered.");

        return orchestrator.ExecuteSyncAsync("lcl", ct);
    }

    [Function("Sync-CRC")]
    public Task RunCrc([TimerTrigger(TimerSchedule)] TimerInfo timer, CancellationToken ct)
    {
        logger.LogInformation("CRC Sync Triggered.");

        return orchestrator.ExecuteSyncAsync("crc", ct);
    }
}
