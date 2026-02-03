using Application.Shared.Enums;
using Application.Shared.Interfaces;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Functions.Timers.References;

public sealed class ReferencesNttTimer(ISyncOrchestrator orchestrator,
                                       ILogger<ReferencesNttTimer> logger)
{
    private const string TimerTriggerExpression = "0 */1 * * * *";
    private const string LobName = GenesysConstants.NttOrg;

    [Function("References-Ntt-Timer")]
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

            await Task.WhenAll(skillsTask, presenceTask, groupTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌Error in References-Ntt-Timer function");
            logger.LogError("Exception details: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
        }
    }
}
