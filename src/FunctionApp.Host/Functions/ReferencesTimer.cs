using System.Text.Json;

using FunctionApp.Application.References.Services;
using FunctionApp.Application.Shared.Providers;
using FunctionApp.Application.Shared.Secrets;
using FunctionApp.Infrastructure.Extensions;
using FunctionApp.Infrastructure.ExternalServices.FlurlHttp;
using FunctionApp.Infrastructure.Persistence.DbContext;
using FunctionApp.Infrastructure.Providers;
using FunctionApp.Infrastructure.Security;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Host.Functions;

public class ReferencesTimer(ILogger<ReferencesTimer> logger,
                             IDateTimeProvider dateTimeProvider,
                             IFlurlHttpClient flurlHttpClient,
                             ISecretProvider secretProvider,
                             IReferencesSyncService referencesSyncService,
                             FunctionAppDbContext dbContext)
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
            // #region Test Api

            // logger.LogInformation("========== Testing HttpClient + Token Infrastructure ==========");
            //
            // logger.LogInformation("Calling Genesys API: /api/v2/routing/skills?pageSize=500");
            //
            // JsonElement response = await httpClient.GetAsync<JsonElement>("/api/v2/routing/skills?pageSize=500",
            //                                                               cancellationToken);
            //
            // logger.LogInformation("===== FULL API RESPONSE =====");
            // string jsonResponse =
            //     JsonSerializer.Serialize(response, options: new JsonSerializerOptions { WriteIndented = true });
            // logger.LogInformation("{Response}", jsonResponse);
            // logger.LogInformation("=============================");
            //
            // if (response.TryGetProperty("total", value: out JsonElement totalElement))
            //     logger.LogInformation("✅ API call successful! Total skills: {Total}", args: totalElement.GetInt64());
            // else
            //     logger.LogInformation("✅ API call successful! Response received.");

            // #endregion
            //
            // #region Test KeyVault
            //
            // logger.LogInformation("========== Testing KeyVault Infrastructure ==========");
            //
            // string genesysClientSecret = await secretProvider.GetSecretAsync("azureTableConnString", cancellationToken);
            //
            // logger.LogInformation("✅ Successfully retrieved secret from Key Vault (Value: {Value})",
            //                       genesysClientSecret);
            //
            // logger.LogInformation("========== KeyVault Test Complete ==========");
            //
            // #endregion

            // #region Test Database
            //
            // logger.LogInformation("========== Testing Database Connection ==========");
            //
            // try
            // {
            //     // Using OpenConnectionAsync instead of CanConnectAsync to throw the actual exception
            //     await dbContext.Database.OpenConnectionAsync(cancellationToken);
            //     logger.LogInformation("✅ Successfully connected to the database!");
            //     await dbContext.Database.CloseConnectionAsync();
            // }
            // catch (Exception ex)
            // {
            //     logger.LogError(ex, "❌ Failed to connect to the database.");
            //     logger.LogError("Connection Error: {Message}", ex.Message);
            //     if (ex.InnerException != null)
            //     {
            //         logger.LogError("Inner Error: {Message}", ex.InnerException.Message);
            //     }
            // }
            //
            // logger.LogInformation("========== Database Test Complete ==========");
            //
            // #endregion

            #region Test Sync all references

            await referencesSyncService.SyncAllAsync(cancellationToken);

            #endregion
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error testing HttpClient + Token infrastructure");
            logger.LogError("Exception details: {Message}", ex.Message);
            if (ex.InnerException != null)
            {
                logger.LogError("Inner exception: {InnerMessage}", ex.InnerException.Message);
            }
        }
    }
}
