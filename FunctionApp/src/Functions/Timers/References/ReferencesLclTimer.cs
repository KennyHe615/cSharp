using Application.Common.Abstractions.Services;
using Application.Common.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Shared.Constants;
using Shared.Extensions;


namespace Functions.Timers.References;

public sealed class ReferencesLclTimer(ISyncOrchestrator orchestrator,
                                       ILogger<ReferencesLclTimer> logger)
{
    private const string TimerTriggerExpression = "0 */1 * * * *";
    private const string LobName = GenesysConstants.LclOrg;

    [Function("References-Lcl-Timer")]
    public async Task RunAsync([TimerTrigger(TimerTriggerExpression)] TimerInfo myTimer,
                               FunctionContext context,
                               CancellationToken ct)
    {
        try
        {
            logger.LogInformation(CommonConstants.LobCategoryLogPrefix + "Timer trigger start",
                                  LobName,
                                  nameof(SyncCategory.References));

            Task skillsTask = orchestrator.ExecuteAsync(LobName, SyncCategory.Skill, ct);
            Task presenceTask = orchestrator.ExecuteAsync(LobName, SyncCategory.PresenceDefinition, ct);
            Task groupTask = orchestrator.ExecuteAsync(LobName, SyncCategory.Group, ct);
            Task wrapupCodeTask = orchestrator.ExecuteAsync(LobName, SyncCategory.WrapupCode, ct);

            await Task.WhenAll(skillsTask, presenceTask, groupTask, wrapupCodeTask).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex,
                                       $"❌{CommonConstants.LobCategoryLogPrefix} Error",
                                       LobName,
                                       nameof(SyncCategory.References));

            throw;
        }
    }
}
