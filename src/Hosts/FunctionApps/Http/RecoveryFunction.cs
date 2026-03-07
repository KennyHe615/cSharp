using System.Text.Json;
using System.Text.Json.Serialization;

using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Contracts.InternalApis.Recovery;
using Application.Features.Recovery;
using Application.Mediator;

using FluentValidation;

using FunctionApp.Http.Common;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace FunctionApp.Http;

/// <summary>
/// HTTP trigger for creating recovery requests.
/// </summary>
/// <remarks>
/// This function validates request shape and input-level constraints, resolves LOB credentials,
/// dispatches the recovery command through the application mediator, and returns a standardized JSON response.
/// </remarks>
public sealed class RecoveryFunction(ISimpleMediator mediator,
                                     ILobContextAccessor lobContextAccessor,
                                     ICredentialProvider credentialProvider,
                                     ILogger<RecoveryFunction> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
                                                                {
                                                                    PropertyNameCaseInsensitive = true,
                                                                    Converters =
                                                                    {
                                                                        new
                                                                            JsonStringEnumConverter(allowIntegerValues
                                                                             : false)
                                                                    }
                                                                };

    /// <summary>
    /// Creates a recovery request record for a given LOB/category and optional interval/GenesysJobId.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>HTTP response containing either created payload or standardized error body.</returns>
    [Function("CreateRecoveryRequest")]
    public async Task<HttpResponseData> CreateRecoveryRequest(
        // Easy Auth (Microsoft Entra ID) should be enforced at Function App level in Azure.
        // Trigger key auth remains a secondary gate until the platform policy is finalized.
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "recovery")] HttpRequestData req,
        CancellationToken ct)
    {
        try
        {
            RecoveryRequest request =
                await HttpRequestParsers.DeserializeOrBadRequestAsync<RecoveryRequest>(req, JsonOptions, ct)
                                        .ConfigureAwait(false);

            await ValidateRequiredRequestFieldsAsync(req, request, ct)
               .ConfigureAwait(false);

            LobName lob = await ParseLobOrWriteBadRequestAsync(req, request.Lob, ct)
               .ConfigureAwait(false);

            using IDisposable scope = logger.BeginOperationScope(lob, "Recovery", "CreateRecoveryRequest");
            logger.LogInformation("Recovery request accepted for validation and processing.");

            await PopulateCredentialsAsync(lob, ct)
               .ConfigureAwait(false);

            CreateRecoveryRequestResponse result = await ProcessRecoveryRequestAsync(request, lob, ct)
               .ConfigureAwait(false);

            return await HttpResponseFactory.CreatedAsync(req, result, ct)
                                            .ConfigureAwait(false);
        }
        catch (BadRequestHandledException ex)
        {
            return ex.Response;
        }
        catch (ValidationException ex)
        {
            string error = ex.Errors.FirstOrDefault()
                            ?.ErrorMessage
                           ?? ex.Message;

            return await HttpResponseFactory.BadRequestAsync(req, error, ct)
                                            .ConfigureAwait(false);
        }
        catch (JsonException ex)
        {
            return await HandleJsonExceptionAsync(req, ex, ct)
               .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            logger.LogWarning("Recovery request processing was canceled by caller/host.");

            throw;
        }
        catch (Exception ex)
        {
            logger.LogErrorWithDetails(ex, "Error processing recovery request.");

            return await HttpResponseFactory.InternalServerErrorAsync(req,
                                                                      "An error occurred processing your request.",
                                                                      ct)
                                            .ConfigureAwait(false);
        }
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Validates mandatory top-level request fields required before business processing.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="request">Deserialized recovery request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <exception cref="BadRequestHandledException">
    /// Thrown when a validation failure response has already been generated.
    /// </exception>
    private static async Task ValidateRequiredRequestFieldsAsync(HttpRequestData req,
                                                                 RecoveryRequest request,
                                                                 CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Lob))
        {
            HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, "Lob is required.", ct)
                                                                 .ConfigureAwait(false);

            throw new BadRequestHandledException(response);
        }

        if (!request.Category.HasValue)
        {
            HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, "Category is required.", ct)
                                                                 .ConfigureAwait(false);

            throw new BadRequestHandledException(response);
        }
    }

    /// <summary>
    /// Parses and validates a raw LOB value into <see cref="LobName"/>.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="lobRaw">Raw LOB string from request body.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validated <see cref="LobName"/> value object.</returns>
    /// <exception cref="BadRequestHandledException">
    /// Thrown when a bad-request response has already been prepared for invalid LOB.
    /// </exception>
    private static async Task<LobName> ParseLobOrWriteBadRequestAsync(HttpRequestData req,
                                                                      string lobRaw,
                                                                      CancellationToken ct)
    {
        try
        {
            return new LobName(lobRaw);
        }
        catch (Exception)
        {
            string message =
                $"Invalid value for 'lob': '{lobRaw}'. Available values: {string.Join(" / ", LobName.AllowedValues)}.";

            HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, message, ct)
                                                                 .ConfigureAwait(false);

            throw new BadRequestHandledException(response);
        }
    }

    /// <summary>
    /// Resolves and populates runtime credentials for the selected LOB.
    /// </summary>
    /// <param name="lob">Validated LOB value.</param>
    /// <param name="ct">Cancellation token.</param>
    private async Task PopulateCredentialsAsync(LobName lob, CancellationToken ct)
    {
        lobContextAccessor.LobName = lob.Value;
        await credentialProvider.PopulateAsync(lobContextAccessor, ct)
                                .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds and dispatches the recovery command to the application layer.
    /// </summary>
    /// <param name="request">Deserialized recovery request payload.</param>
    /// <param name="lob">Validated LOB value.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Created recovery response from application layer.</returns>
    private async Task<CreateRecoveryRequestResponse> ProcessRecoveryRequestAsync(RecoveryRequest request,
        LobName lob,
        CancellationToken ct)
    {
        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(lob,
                                             request.Category!.Value,
                                             request.Interval,
                                             request.GenesysJobId);

        return await mediator.Send(command, ct)
                             .ConfigureAwait(false);
    }

    /// <summary>
    /// Converts JSON parsing exceptions into standardized client-facing responses.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="ex">JSON exception thrown during deserialization.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A standardized bad-request response.</returns>
    private async Task<HttpResponseData> HandleJsonExceptionAsync(HttpRequestData req,
                                                                  JsonException ex,
                                                                  CancellationToken ct)
    {
        if (JsonEnumParseErrorHelper.TryBuildMessage<RecoveryRequest>(ex, out string enumError))
        {
            return await HttpResponseFactory.BadRequestAsync(req, enumError, ct)
                                            .ConfigureAwait(false);
        }

        if (string.Equals(ex.Path, "$.interval", StringComparison.OrdinalIgnoreCase))
        {
            return await HttpResponseFactory.BadRequestAsync(req, ex.Message, ct)
                                            .ConfigureAwait(false);
        }

        logger.LogWarningWithDetails(ex, "Invalid JSON payload for recovery request.");

        return await HttpResponseFactory.BadRequestAsync(req, "Invalid JSON payload.", ct)
                                        .ConfigureAwait(false);
    }

    #endregion
}
