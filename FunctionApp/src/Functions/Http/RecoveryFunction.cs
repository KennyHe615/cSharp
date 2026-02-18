using System.Net;
using System.Text.Json;

using Application.Common.Enums;
using Application.Common.Mediator;
using Application.Contracts.Recovery;
using Application.Features.Recovery;

using Azure.Core.Serialization;

using FluentValidation;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;


namespace Functions.Http;

public class RecoveryFunction(ISimpleMediator mediator,
                              ILogger<RecoveryFunction> logger,
                              JsonSerializerOptions jsonOptions)
{
    [Function("CreateRecoveryRequest")]
    public async Task<HttpResponseData> CreateRecoveryRequest(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "recovery")] HttpRequestData req)
    {
        JsonObjectSerializer serializer = new(jsonOptions);

        try
        {
            // Read the request body
            using StreamReader reader = new(req.Body);
            string requestBody = await reader.ReadToEndAsync();

            // Deserialize the command
            CreateRecoveryRequestCommand? command =
                JsonSerializer.Deserialize<CreateRecoveryRequestCommand>(requestBody, jsonOptions);

            if (command == null)
            {
                HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badRequestResponse.WriteAsJsonAsync(new
                                                          {
                                                              Error = "Invalid request body"
                                                          },
                                                          serializer);

                return badRequestResponse;
            }

            CreateRecoveryRequestResponse result = await mediator.Send(command);

            HttpResponseData response = req.CreateResponse(HttpStatusCode.OK);

            await response.WriteAsJsonAsync(result, serializer);

            return response;
        }
        catch (JsonException ex) when (ex.Message.Contains("Unknown RecoveryLob value"))
        {
            // Handle JSON deserialization errors for LOB enum conversion
            logger.LogWarning(ex, "JSON deserialization failed for LOB value");

            string errorMessage =
                $"Invalid Lob value. Available values: {string.Join(" / ", Enum.GetValues<RecoveryLob>().Select(e => e.ToString()))}";

            HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new
                                                      {
                                                          Error = errorMessage
                                                      },
                                                      serializer);

            return badRequestResponse;
        }
        catch (JsonException ex) when (ex.Message.Contains("Unknown SyncCategory value"))
        {
            // Handle JSON deserialization errors for Category enum conversion
            logger.LogWarning(ex, "JSON deserialization failed for Category value");

            string errorMessage =
                $"Invalid Category value. Available values: {string.Join(" / ", new[] { SyncCategory.UserDetailsRecovery })}";

            HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new
                                                      {
                                                          Error = errorMessage
                                                      },
                                                      serializer);

            return badRequestResponse;
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation failed for recovery request");

            string[] errors = ex.Errors.Select(e => e.ErrorMessage).ToArray();

            HttpResponseData badRequestResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequestResponse.WriteAsJsonAsync(new
                                                      {
                                                          Errors = errors
                                                      },
                                                      serializer);

            return badRequestResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing recovery request");

            HttpResponseData errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteAsJsonAsync(new
                                                 {
                                                     Error = "An error occurred processing your request"
                                                 },
                                                 serializer);

            return errorResponse;
        }
    }
}
