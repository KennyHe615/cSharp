using FunctionApp.Infrastructure.Extensions;
using FunctionApp.Infrastructure.Providers;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Host.Functions;

public class ReferencesTimer(ILogger<ReferencesTimer> logger,
                             IDateTimeProvider dateTimeProvider)
{
    private const string _timerTriggerExpression = "0 */1 * * * *";

    [Function("ReferencesTimer")]
    public void Run([TimerTrigger(_timerTriggerExpression)] TimerInfo myTimer,
                    FunctionContext context,
                    CancellationToken cancellationToken)
    {
        var executionDetails = new
                               {
                                   ExecutionTime = dateTimeProvider.FormatLocalTimestamp(),
                                   FunctionName = context.FunctionDefinition.Name,
                               };

        logger.LogInfoStructuredDetails(executionDetails);
        logger.LogWarningStructuredDetails(executionDetails);
        logger.LogErrorStructuredDetails(executionDetails);
        logger.LogCriticalStructuredDetails(executionDetails);
    }
}
