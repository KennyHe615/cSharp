using Application.Common.Abstractions.Sync;
using Application.Common.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Shared.Constants;
using Shared.Extensions;


namespace Functions.Timers.References;

public sealed class ReferencesCrcTimer(ISyncOrchestrator orchestrator,
                                       ILogger<ReferencesCrcTimer> logger)
{
    private const string TimerTriggerExpression = "0 */1 * * * *";
    private const string LobName = GenesysConstants.CrcOrg;

    [Function("References-Crc-Timer")]
    public async Task RunAsync([TimerTrigger(TimerTriggerExpression)] TimerInfo myTimer,
                               FunctionContext context,
                               CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Starting references sync for LOB {Lob}", LobName);

            Task skillsTask = orchestrator.ExecuteAsync(LobName, SyncCategory.Skill, ct);
            Task presenceTask = orchestrator.ExecuteAsync(LobName, SyncCategory.PresenceDefinition, ct);
            Task groupTask = orchestrator.ExecuteAsync(LobName, SyncCategory.Group, ct);
            Task wrapupCodeTask = orchestrator.ExecuteAsync(LobName, SyncCategory.WrapupCode, ct);

            await Task.WhenAll(skillsTask, presenceTask, groupTask, wrapupCodeTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "❌ References sync failed for LOB: {Lob}", LobName);

            throw;
        }
    }
}
