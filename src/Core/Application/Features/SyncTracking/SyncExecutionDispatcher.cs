using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;


namespace Application.Features.SyncTracking;

/// <summary>
/// Default dispatcher that maps (category, mode) to concrete sync execution routines
/// and tracks dispatch stage status through checkpoints.
/// </summary>
public sealed class SyncExecutionDispatcher(ISyncCheckpointRepository syncCheckpointRepository)
    : ISyncExecutionDispatcher
{
    /// <inheritdoc />
    /// <exception cref="NotSupportedException">
    /// Thrown when the provided (category, mode) route is unsupported.
    /// </exception>
    public async Task ExecuteAsync(long runId,
                                   SyncCategory category,
                                   SyncMode mode,
                                   string? interval,
                                   int? pageNumber,
                                   string? genesysJobId,
                                   CancellationToken ct)
    {
        string dispatchCursor = BuildDispatchCursor(category,
                                                    mode,
                                                    interval,
                                                    pageNumber,
                                                    genesysJobId);

        await syncCheckpointRepository.UpsertAsync(runId,
                                                   "Dispatch",
                                                   dispatchCursor,
                                                   SyncRunStatus.Running,
                                                   null,
                                                   ct)
                                      .ConfigureAwait(false);

        try
        {
            await ((category, mode) switch
                   {
                       (SyncCategory.UsersDetails, SyncMode.Incremental) =>
                           ExecuteUsersDetailsIncrementalAsync(runId,
                                                               interval,
                                                               pageNumber,
                                                               ct),

                       (SyncCategory.UsersDetails, SyncMode.Recovery) => ExecuteUsersDetailsRecoveryAsync(runId,
                        interval,
                        pageNumber,
                        genesysJobId,
                        ct),

                       (SyncCategory.ConversationsDetails, SyncMode.Incremental) =>
                           ExecuteConversationsDetailsIncrementalAsync(runId,
                                                                       interval,
                                                                       pageNumber,
                                                                       ct),

                       (SyncCategory.ConversationsDetails, SyncMode.Recovery) =>
                           ExecuteConversationsDetailsRecoveryAsync(runId,
                                                                    interval,
                                                                    pageNumber,
                                                                    genesysJobId,
                                                                    ct),

                       (SyncCategory.ConversationsAggregates, SyncMode.Incremental) =>
                           ExecuteConversationsAggregatesIncrementalAsync(runId,
                                                                          interval,
                                                                          pageNumber,
                                                                          ct),

                       (SyncCategory.ConversationsAggregates, SyncMode.Recovery) =>
                           ExecuteConversationsAggregatesRecoveryAsync(runId,
                                                                       interval,
                                                                       pageNumber,
                                                                       genesysJobId,
                                                                       ct),

                       (SyncCategory.User, SyncMode.Incremental) => ExecuteReferenceUserIncrementalAsync(runId,
                        interval,
                        pageNumber,
                        ct),

                       (SyncCategory.Queue, SyncMode.Incremental) => ExecuteReferenceQueueIncrementalAsync(runId,
                        interval,
                        pageNumber,
                        ct),

                       (SyncCategory.Flow, SyncMode.Incremental) => ExecuteReferenceFlowIncrementalAsync(runId,
                        interval,
                        pageNumber,
                        ct),

                       (SyncCategory.Group, SyncMode.Incremental) => ExecuteReferenceGroupIncrementalAsync(runId,
                        interval,
                        pageNumber,
                        ct),

                       (SyncCategory.Skill, SyncMode.Incremental) => ExecuteReferenceSkillIncrementalAsync(runId,
                        interval,
                        pageNumber,
                        ct),

                       (SyncCategory.PresenceDefinition, SyncMode.Incremental) =>
                           ExecuteReferencePresenceDefinitionIncrementalAsync(runId,
                                                                              interval,
                                                                              pageNumber,
                                                                              ct),

                       (SyncCategory.WrapUpCode, SyncMode.Incremental) =>
                           ExecuteReferenceWrapUpCodeIncrementalAsync(runId,
                                                                      interval,
                                                                      pageNumber,
                                                                      ct),

                       // Explicit business guardrail: references do not support recovery.
                       (SyncCategory.User or SyncCategory.Queue or SyncCategory.Flow or SyncCategory.Group
                        or SyncCategory.Skill or SyncCategory.PresenceDefinition
                        or SyncCategory.WrapUpCode, SyncMode.Recovery) => throw new
                           NotSupportedException("Recovery mode is not supported for References categories."),

                       _ => throw new
                           NotSupportedException($"Unsupported sync execution route: Category={category}, Mode={mode}.")
                   }).ConfigureAwait(false);

            await syncCheckpointRepository.UpsertAsync(runId,
                                                       "Dispatch",
                                                       dispatchCursor,
                                                       SyncRunStatus.Completed,
                                                       null,
                                                       ct)
                                          .ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
        {
            await syncCheckpointRepository.UpsertAsync(runId,
                                                       "Dispatch",
                                                       dispatchCursor,
                                                       SyncRunStatus.Canceled,
                                                       ex.Message,
                                                       CancellationToken.None)
                                          .ConfigureAwait(false);

            throw;
        }
        catch (Exception ex)
        {
            await syncCheckpointRepository.UpsertAsync(runId,
                                                       "Dispatch",
                                                       dispatchCursor,
                                                       SyncRunStatus.Failed,
                                                       ex.Message,
                                                       ct)
                                          .ConfigureAwait(false);

            throw;
        }
    }

    #region ========== *** Private Section *** ==========

    /// <summary>
    /// Builds a deterministic checkpoint cursor for dispatcher stage identity.
    /// </summary>
    /// <param name="category">Sync category.</param>
    /// <param name="mode">Sync mode.</param>
    /// <param name="interval">Optional interval selector.</param>
    /// <param name="pageNumber">Optional page selector.</param>
    /// <param name="genesysJobId">Optional external job id selector.</param>
    /// <returns>Normalized dispatch cursor string.</returns>
    private static string BuildDispatchCursor(SyncCategory category,
                                              SyncMode mode,
                                              string? interval,
                                              int? pageNumber,
                                              string? genesysJobId)
    {
        string i = string.IsNullOrWhiteSpace(interval) ? "-" : interval.Trim();
        string p = pageNumber.HasValue ? pageNumber.Value.ToString() : "-";
        string g = string.IsNullOrWhiteSpace(genesysJobId) ? "-" : genesysJobId.Trim();

        return $"{category}|{mode}|{i}|{p}|{g}";
    }

    /// <summary>
    /// Placeholder route for UsersDetails incremental execution.
    /// </summary>
    private static Task ExecuteUsersDetailsIncrementalAsync(long runId,
                                                            string? interval,
                                                            int? pageNumber,
                                                            CancellationToken ct)
    {
        Console.WriteLine("UsersDetails Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteUsersDetailsRecoveryAsync(long runId,
                                                         string? interval,
                                                         int? pageNumber,
                                                         string? genesysJobId,
                                                         CancellationToken ct)
    {
        Console.WriteLine("UsersDetails Recovery execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteConversationsDetailsIncrementalAsync(long runId,
                                                                    string? interval,
                                                                    int? pageNumber,
                                                                    CancellationToken ct)
    {
        Console.WriteLine("ConversationsDetails Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteConversationsDetailsRecoveryAsync(long runId,
                                                                 string? interval,
                                                                 int? pageNumber,
                                                                 string? genesysJobId,
                                                                 CancellationToken ct)
    {
        Console.WriteLine("ConversationsDetails Recovery execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteConversationsAggregatesIncrementalAsync(long runId,
                                                                       string? interval,
                                                                       int? pageNumber,
                                                                       CancellationToken ct)
    {
        Console.WriteLine("ConversationsAggregates Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteConversationsAggregatesRecoveryAsync(long runId,
                                                                    string? interval,
                                                                    int? pageNumber,
                                                                    string? genesysJobId,
                                                                    CancellationToken ct)
    {
        Console.WriteLine("ConversationsAggregates Recovery execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceUserIncrementalAsync(long runId,
                                                             string? interval,
                                                             int? pageNumber,
                                                             CancellationToken ct)
    {
        Console.WriteLine("Reference User Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceQueueIncrementalAsync(long runId,
                                                              string? interval,
                                                              int? pageNumber,
                                                              CancellationToken ct)
    {
        Console.WriteLine("Reference Queue Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceFlowIncrementalAsync(long runId,
                                                             string? interval,
                                                             int? pageNumber,
                                                             CancellationToken ct)
    {
        Console.WriteLine("Reference Flow Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceGroupIncrementalAsync(long runId,
                                                              string? interval,
                                                              int? pageNumber,
                                                              CancellationToken ct)
    {
        Console.WriteLine("Reference Group Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceSkillIncrementalAsync(long runId,
                                                              string? interval,
                                                              int? pageNumber,
                                                              CancellationToken ct)
    {
        Console.WriteLine("Reference Skill Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferencePresenceDefinitionIncrementalAsync(
        long runId,
        string? interval,
        int? pageNumber,
        CancellationToken ct)
    {
        Console.WriteLine("Reference PresenceDefinition Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    private static Task ExecuteReferenceWrapUpCodeIncrementalAsync(long runId,
                                                                   string? interval,
                                                                   int? pageNumber,
                                                                   CancellationToken ct)
    {
        Console.WriteLine("Reference WrapUpCode Incremental execution is not wired yet.");

        return Task.CompletedTask;
    }

    #endregion
}
