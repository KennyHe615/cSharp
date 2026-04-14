using System.Diagnostics;

using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Enums;
using Application.Features.SyncTracking.References;
using Application.Mediator;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using SharedKernel.Lobs;
using SharedKernel.Logging;
using SharedKernel.Time;


namespace FunctionApps.Timer;

/// <summary>
/// Default host-level runner for references full-sync timer execution.
/// </summary>
public sealed class ReferencesTimerRunner(ISimpleMediator mediator,
                                          ILobContextAccessor lobContextAccessor,
                                          ICredentialProvider credentialProvider,
                                          IDateTimeProvider dateTimeProvider,
                                          ILogger<ReferencesTimerRunner> logger) : IReferencesTimerRunner
{
    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="lob"/> is empty or whitespace.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the invocation is canceled by the host.</exception>
    public async Task RunAsync(LobName lob, TimerInfo timer, CancellationToken ct)
    {
        const string domain = "References.FullSync";
        Stopwatch overall = Stopwatch.StartNew();

        SyncReferenceCategory[] categories = Enum.GetValues<SyncReferenceCategory>();

        using IDisposable scope = logger.BeginOperationScope(lob, domain);
        logger.LogInformation(LobLogTemplates.LobCategory
                              + "STARTED. TriggerAtEst = {TriggeredAtEst} IsPastDue = {IsPastDue} Categories Count = {CategoryCount}.",
                              lob.Value,
                              domain,
                              dateTimeProvider.EstNow,
                              timer.IsPastDue,
                              categories.Length);

        lobContextAccessor.LobName = lob.Value;
        await credentialProvider.PopulateAsync(lobContextAccessor, ct)
                                .ConfigureAwait(false);

        int executed = await ExecuteCategoriesAsync(lob, categories, ct)
           .ConfigureAwait(false);

        overall.Stop();
        logger.LogInformation(LobLogTemplates.LobCategory
                              + "COMPLETED. CompletedAtEst = {CompletedAtEst} Categories Count = {CategoryCount} Executed = {ExecutedCount} Duration = {Duration} Second.",
                              lob.Value,
                              domain,
                              dateTimeProvider.EstNow,
                              categories.Length,
                              executed,
                              Math.Round(overall.Elapsed.TotalSeconds, 2, MidpointRounding.AwayFromZero));
    }

    #region ========== *** Private Section *** ==========
    /// <summary>
    /// Executes references full-sync category commands sequentially for the given LOB.
    /// Logs per-category start/completion, and skips categories that are not wired yet.
    /// </summary>
    /// <param name="lob">Target LOB used for scoped logging and command execution context.</param>
    /// <param name="categories">Ordered category list to execute.</param>
    /// <param name="ct">Cancellation token propagated from the host invocation.</param>
    /// <returns>The number of categories that completed successfully (excluding skipped unsupported categories).</returns>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested during execution.</exception>
    /// <exception cref="Exception">
    /// Propagates non-<see cref="NotSupportedException"/> failures from command dispatch.
    /// </exception>
    private async Task<int> ExecuteCategoriesAsync(LobName lob,
                                                   SyncReferenceCategory[] categories,
                                                   CancellationToken ct)
    {
        int executed = 0;

        for (int index = 0; index < categories.Length; index++)
        {
            SyncReferenceCategory category = categories[index];
            Stopwatch categoryWatch = Stopwatch.StartNew();

            logger.LogInformation(LobLogTemplates.LobCategory + "STARTED. Sequence = {Sequence}/{Total}.",
                                  lob.Value,
                                  category,
                                  index + 1,
                                  categories.Length);

            try
            {
                await mediator.Send(new RunReferencesFullSyncCommand(category), ct)
                              .ConfigureAwait(false);

                executed++;
                categoryWatch.Stop();
                logger.LogInformation(LobLogTemplates.LobCategory + "COMPLETED. Duration = {Duration} Second.",
                                      lob.Value,
                                      category,
                                      Math.Round(categoryWatch.Elapsed.TotalSeconds, 2, MidpointRounding.AwayFromZero));
            }
            catch (NotSupportedException)
            {
                categoryWatch.Stop();
                logger.LogWarning(LobLogTemplates.LobCategory
                                  + "SKIPPED. Category not wired yet. Duration = {Duration} Second.",
                                  lob.Value,
                                  category,
                                  Math.Round(categoryWatch.Elapsed.TotalSeconds, 2, MidpointRounding.AwayFromZero));
            }
        }

        return executed;
    }
    #endregion
}
