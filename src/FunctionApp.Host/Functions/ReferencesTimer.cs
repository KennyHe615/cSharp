using FunctionApp.Application.Shared.Providers;
using FunctionApp.Application.Shared.Services;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Host.Functions;

public class ReferencesTimer(SyncOrchestrator orchestrator,
                             IDateTimeProvider dateTimeProvider,
                             ILogger<ReferencesTimer> logger)
{
    private const string TimerSchedule = "0 */1 * * * *"; // Every 30 minutes

    [Function("Sync-NTT-References")]
    public Task RunNtt([TimerTrigger(TimerSchedule)] TimerInfo timer, FunctionContext context, CancellationToken ct)
    {
        logger.LogCritical("[LOB: NTT]✅Synchronization of [References] STARTS ON **{Time}**",
                           dateTimeProvider.FormatLocalTimestamp());

        return orchestrator.ExecuteSyncAsync("NTT", ct);
    }

    [Function("Sync-LCL-References")]
    public Task RunLcl([TimerTrigger(TimerSchedule)] TimerInfo timer, CancellationToken ct)
    {
        logger.LogCritical("[LOB: LCL]✅Synchronization of [References] STARTS ON **{Time}**",
                           dateTimeProvider.FormatLocalTimestamp());

        return orchestrator.ExecuteSyncAsync("LCL", ct);
    }

    [Function("Sync-CRC-References")]
    public Task RunCrc([TimerTrigger(TimerSchedule)] TimerInfo timer, CancellationToken ct)
    {
        logger.LogCritical("[LOB: CRC]✅Synchronization of [References] STARTS ON **{Time}**",
                           dateTimeProvider.FormatLocalTimestamp());

        return orchestrator.ExecuteSyncAsync("CRC", ct);
    }
}
