using Application.Abstractions.Persistence;
using Application.Abstractions.Planning;
using Application.DTOs.Planning;
using Application.DTOs.Recovery;
using Application.Enums;
using Application.Mediator;

using SharedKernel.Extensions;
using SharedKernel.Time;


namespace Application.Features.Recovery;

/// <summary>
/// Handles materialization of recovery intake requests into executable sync_request rows.
/// </summary>
public sealed class MaterializeRecoveryIntakeCommandHandler(IRecoveryIntakeWorkRepository intakeWorkRepository,
                                                            ISyncRequestRepository syncRequestRepository,
                                                            IIntervalPlanner intervalPlanner)
        : IRequestHandler<MaterializeRecoveryIntakeCommand, bool>
{
    /// <summary>
    /// Starts one pending recovery intake request, creates executable recovery sync_request rows, and finalizes the intake row.
    /// </summary>
    /// <param name="request">Materialization command with an optional category filter.</param>
    /// <param name="ct">Cancellation token from caller or host.</param>
    /// <returns><c>true</c> when an intake request was processed; otherwise <c>false</c>.</returns>
    public async Task<bool> Handle(MaterializeRecoveryIntakeCommand request, CancellationToken ct = default)
    {
        AnalyticsRecoveryRequestDto? intake =
                await intakeWorkRepository.TryStartNextPendingAsync(request.Category?.ToString(), ct)
                                          .ConfigureAwait(false);

        if (intake is null) return false;

        try
        {
            SyncAnalyticsCategory category = intake.Category.ReadEnum<SyncAnalyticsCategory>();

            if (!string.IsNullOrWhiteSpace(intake.GenesysJobId))
            {
                await CreateExecutableRequestAsync(category,
                                                   null,
                                                   intake.GenesysJobId,
                                                   ct)
                       .ConfigureAwait(false);
            }
            else
            {
                string intervalText = intake.Interval
                                      ?? throw new
                                              InvalidOperationException($"Recovery intake request '{intake.Id}' does not contain a UTC interval.");

                UtcInterval interval = UtcInterval.Parse(intervalText);

                IReadOnlyList<PlannedIntervalDto> plannedIntervals =
                        await intervalPlanner.PlanAsync(category, interval, ct)
                                             .ConfigureAwait(false);

                foreach (PlannedIntervalDto plannedInterval in plannedIntervals)
                {
                    await CreateExecutableRequestAsync(category,
                                                       plannedInterval.Interval.ToString(),
                                                       null,
                                                       ct)
                           .ConfigureAwait(false);
                }
            }

            bool completed = await intakeWorkRepository.TryMarkCompletedAsync(intake.Id, ct)
                                                       .ConfigureAwait(false);

            if (!completed)
            {
                throw new
                        InvalidOperationException($"Recovery intake request '{intake.Id}' could not be marked completed.");
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            string failureReason = (string.IsNullOrWhiteSpace(ex.Message)
                                            ? ex.GetType()
                                                .Name
                                            : ex.Message).Truncate(1000)!;

            bool failed = await intakeWorkRepository.TryMarkFailedAsync(intake.Id, failureReason, ct)
                                                    .ConfigureAwait(false);

            if (!failed)
            {
                throw new
                        InvalidOperationException($"Recovery intake request '{intake.Id}' could not be marked failed.",
                                                  ex);
            }

            return true;
        }
    }

    #region ========== *** Private Section *** ==========

    private async Task CreateExecutableRequestAsync(SyncAnalyticsCategory category,
                                                    string? interval,
                                                    string? genesysJobId,
                                                    CancellationToken ct)
    {
        await syncRequestRepository.CreateOrGetByScopeAsync(category.ToString(),
                                                            SyncMode.Recovery,
                                                            interval,
                                                            null,
                                                            genesysJobId,
                                                            ct)
                                   .ConfigureAwait(false);
    }

    #endregion
}
