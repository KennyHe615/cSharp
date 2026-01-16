using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApps.Host.Functions;

public class ReferencesTimer(ILogger<ReferencesTimer> logger)
{
    [Function("ReferencesTimer")]
    public void Run([TimerTrigger("0 * * * * *")] TimerInfo myTimer)
    {
        logger.LogInformation("C# Timer trigger function executed at: {DateTime}", DateTime.UtcNow);

        if (myTimer.ScheduleStatus is not null)
        {
            logger.LogInformation("Current timer scheduled for: {ScheduleStatusNext}", myTimer.ScheduleStatus.Next);
        }
    }
}
