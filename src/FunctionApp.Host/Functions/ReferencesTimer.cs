using System.Text.Json;

using FunctionApp.Infrastructure.Extensions;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.Providers;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Host.Functions;

public class ReferencesTimer(ILogger<ReferencesTimer> logger,
                             IDateTimeProvider dateTimeProvider,
                             IHttpClient httpClient)
{
    private const string TimerTriggerExpression = "0 */1 * * * *";

    [Function("ReferencesTimer")]
    public async Task Run([TimerTrigger(TimerTriggerExpression)] TimerInfo myTimer,
                          FunctionContext context,
                          CancellationToken cancellationToken)
    {
        var executionDetails = new
                               {
                                   ExecutionTime = dateTimeProvider.FormatLocalTimestamp(),
                                   FunctionName = context.FunctionDefinition.Name
                               };

        logger.LogInfoStructuredDetails(executionDetails);

        try
        {
            logger.LogInformation("========== Testing HttpClient + Token Infrastructure ==========");

            logger.LogInformation("Calling Genesys API: /api/v2/routing/skills?pageSize=500");

            JsonElement response = await httpClient.GetAsync<JsonElement>("/api/v2/routing/skills?pageSize=500",
                                                                          cancellationToken);

            logger.LogInformation("===== FULL API RESPONSE =====");
            string jsonResponse =
                JsonSerializer.Serialize(response, options: new JsonSerializerOptions { WriteIndented = true });
            logger.LogInformation("{Response}", jsonResponse);
            logger.LogInformation("=============================");

            if (response.TryGetProperty("total", value: out JsonElement totalElement))
                logger.LogInformation("✅ API call successful! Total skills: {Total}", args: totalElement.GetInt64());
            else
                logger.LogInformation("✅ API call successful! Response received.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error testing HttpClient + Token infrastructure");
            logger.LogError("Exception details: {Message}", ex.Message);
            if (ex.InnerException != null)
                logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
        }
    }
}
