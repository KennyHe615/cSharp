using Application.Shared.Enums;
using Application.Shared.Interfaces;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Shared.Constants;


namespace Functions.Timers.References;

public sealed class ReferencesLclTimer(ISyncOrchestrator orchestrator,
                                       ILogger<ReferencesNttTimer> logger)
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
            logger.LogInformation("Starting references sync for LOB {Lob}", LobName);

            await orchestrator.ExecuteAsync(LobName, SyncCategory.Skills, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌Error in References-Lcl-Timer function");
            logger.LogError("Exception details: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
        }
    }
}
