using Application.Common.Abstractions.Services;
using Application.Common.Enums;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Shared.Constants;
using Shared.Extensions;


namespace Functions.Timers.UserDetails;

public class UserDetailsNttTimer(ISyncOrchestrator orchestrator,
                                 ILogger<UserDetailsNttTimer> logger)
{
    private const string TimerTriggerExpression = "0 */1 * * * *";
    private const string LobName = GenesysConstants.NttOrg;

    [Function("UserDetails-Ntt-Timer")]
    public async Task RunAsync([TimerTrigger(TimerTriggerExpression)] TimerInfo myTimer,
                               FunctionContext context,
                               CancellationToken ct)
    {
        try
        {
            logger.LogInformation(CommonConstants.LobCategoryLogPrefix + "Timer trigger start",
                                  LobName,
                                  SyncCategory.UserDetailsIncremental);

            await orchestrator.ExecuteAsync(LobName, SyncCategory.UserDetailsIncremental, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex,
                                       $"❌{CommonConstants.LobCategoryLogPrefix} Error",
                                       LobName,
                                       SyncCategory.UserDetailsIncremental);

            throw;
        }
    }
}
