using System.Text.Json;
using System.Text.Json.Serialization;

using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.Recovery;
using Application.Features.Recovery;
using Application.Mediator;

using FluentValidation;

using FunctionApps.Http.Common;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;


namespace FunctionApps.Http;

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
    #region ========== *** Fields and Properties *** ==========

    private readonly ISimpleMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    private readonly ILobContextAccessor _lobContextAccessor =
            lobContextAccessor ?? throw new ArgumentNullException(nameof(lobContextAccessor));

    private readonly ICredentialProvider _credentialProvider =
            credentialProvider ?? throw new ArgumentNullException(nameof(credentialProvider));

    private readonly ILogger<RecoveryFunction> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
                                                                {
                                                                    PropertyNameCaseInsensitive = true,
                                                                    UnmappedMemberHandling =
                                                                            JsonUnmappedMemberHandling.Disallow,
                                                                    Converters =
                                                                    {
                                                                        new
                                                                                JsonStringEnumConverter(allowIntegerValues
                                                                                    : false)
                                                                    }
                                                                };

    private const string LogCategory = "Recovery";

    #endregion

    /// <summary>
    /// Creates a recovery request for the specified LOB and category, then dispatches it through the application mediator.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A <see cref="HttpResponseData"/> containing:
    /// 201 with the created recovery payload on newly created recovery requests,
    /// 202 with the accepted recovery payload on reused or reopened recovery requests,
    /// 400 for invalid client input,
    /// or 500 for unexpected server errors.
    /// <para>
    /// When provided, <c>GenesysJobId</c> is supported only for ConversationsDetails recovery.
    /// </para>
    /// </returns>
    [Function("CreateRecoveryRequest")]
    public async Task<HttpResponseData> CreateRecoveryRequest(
            // Easy Auth (Microsoft Entra ID) should be enforced at Function App level in Azure.
            // Trigger key auth remains a secondary gate until the platform policy is finalized.
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "recovery")] HttpRequestData req,
            CancellationToken ct)
    {
        const string logEntity = "CreateRecoveryRequest";
        LobName lob = default;

        try
        {
            RecoveryRequest request =
                    await HttpRequestParsers.DeserializeOrBadRequestAsync<RecoveryRequest>(req, JsonOptions, ct)
                                            .ConfigureAwait(false);

            RecoveryCategory category = await ValidateRequiredRequestFieldsAsync(req, request, ct)
                                               .ConfigureAwait(false);

            lob = await ParseLobOrWriteBadRequestAsync(req, request.Lob, ct)
                         .ConfigureAwait(false);

            using IDisposable scope = _logger.BeginOperationScope(lob, LogCategory, logEntity);
            _logger.LogInformation(LobLogTemplates.LobCategoryEntity
                                   + "Request accepted for validation and processing.",
                                   lob,
                                   LogCategory,
                                   logEntity);

            await PopulateCredentialsAsync(lob, ct)
                   .ConfigureAwait(false);

            CreateRecoveryRequestResponse result = await ProcessRecoveryRequestAsync(request,
                                                               lob,
                                                               category,
                                                               ct)
                                                          .ConfigureAwait(false);

            return await WriteSuccessfulRecoveryResponseAsync(req, result, ct)
                          .ConfigureAwait(false);
        }
        catch (BadRequestHandledException ex)
        {
            return ex.Response;
        }
        catch (ValidationException ex)
        {
            string error = ex.Errors.Select(x => x.ErrorMessage)
                             .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))
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
            _logger.LogWarning(LobLogTemplates.LobCategoryEntity + "Request processing was canceled by caller/host.",
                               lob,
                               LogCategory,
                               logEntity);

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogErrorWithDetails(ex,
                                        LobLogTemplates.LobCategoryEntity + "Error processing request.",
                                        lob,
                                        LogCategory,
                                        logEntity);

            return await HttpResponseFactory.InternalServerErrorAsync(req,
                                                                      "An error occurred processing your request.",
                                                                      ct)
                                            .ConfigureAwait(false);
        }
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Validates required top-level fields and returns a non-null category value for downstream processing.
    /// </summary>
    /// <param name="req">Incoming HTTP request.</param>
    /// <param name="request">Deserialized recovery request payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The validated <see cref="RecoveryCategory"/> value.</returns>
    /// <exception cref="BadRequestHandledException">
    /// Thrown when required fields are missing and a 400 response has already been prepared.
    /// </exception>
    private static async Task<RecoveryCategory> ValidateRequiredRequestFieldsAsync(HttpRequestData req,
        RecoveryRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Lob))
        {
            HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, "Lob is required.", ct)
                                                                 .ConfigureAwait(false);

            throw new BadRequestHandledException(response);
        }

        // ReSharper disable once InvertIf
        if (!request.Category.HasValue)
        {
            HttpResponseData response = await HttpResponseFactory.BadRequestAsync(req, "Category is required.", ct)
                                                                 .ConfigureAwait(false);

            throw new BadRequestHandledException(response);
        }

        return request.Category.Value;
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
        catch (ArgumentException)
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
        _lobContextAccessor.LobName = lob.Value;
        await _credentialProvider.PopulateAsync(_lobContextAccessor, ct)
                                 .ConfigureAwait(false);
    }

    /// <summary>
    /// Builds the recovery command and dispatches it through the mediator.
    /// </summary>
    /// <param name="request">Validated recovery request payload.</param>
    /// <param name="lob">Validated LOB value object.</param>
    /// <param name="category">Validated recovery category.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The application-layer response for the created recovery request.</returns>
    /// <remarks>
    /// <c>GenesysJobId</c> is forwarded as part of the command only for supported recovery categories.
    /// </remarks>
    private async Task<CreateRecoveryRequestResponse> ProcessRecoveryRequestAsync(RecoveryRequest request,
        LobName lob,
        RecoveryCategory category,
        CancellationToken ct)
    {
        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(lob,
                                                                                    category,
                                                                                    request.Interval,
                                                                                    request.GenesysJobId);

        return await _mediator.Send(command, ct)
                              .ConfigureAwait(false);
    }

    /// <summary>
    /// Writes a success response for a resolved recovery request.
    /// </summary>
    /// <param name="req">Incoming HTTP request used to create the response.</param>
    /// <param name="result">Resolved recovery request response from the application layer.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// A 201 Created response when a new recovery request was created,
    /// or a 202 Accepted response when an existing recovery request was reused or reopened.
    /// </returns>
    private static async Task<HttpResponseData> WriteSuccessfulRecoveryResponseAsync(HttpRequestData req,
        CreateRecoveryRequestResponse result,
        CancellationToken ct)
    {
        if (string.Equals(result.Data.RequestAction,
                          nameof(AnalyticsRecoveryRequestResolveAction.Created),
                          StringComparison.Ordinal))
        {
            return await HttpResponseFactory.CreatedAsync(req, result, ct)
                                            .ConfigureAwait(false);
        }

        return await HttpResponseFactory.AcceptedAsync(req, result, ct)
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

        if (JsonEnumParseErrorHelper.TryBuildUnsupportedFieldMessage<RecoveryRequest>(ex,
                out string unsupportedFieldError))
        {
            return await HttpResponseFactory.BadRequestAsync(req, unsupportedFieldError, ct)
                                            .ConfigureAwait(false);
        }

        if (string.Equals(ex.Path, "$.interval", StringComparison.OrdinalIgnoreCase))
        {
            return await HttpResponseFactory.BadRequestAsync(req, ex.Message, ct)
                                            .ConfigureAwait(false);
        }

        _logger.LogWarningWithDetails(ex, "Invalid JSON payload for recovery request.");

        return await HttpResponseFactory.BadRequestAsync(req, "Invalid JSON payload.", ct)
                                        .ConfigureAwait(false);
    }

    #endregion
}
