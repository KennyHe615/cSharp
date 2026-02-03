using System.Collections.Concurrent;

using Application.Shared.Context;
using Application.Shared.Enums;
using Application.Shared.Extensions;
using Application.Shared.Interfaces;
using Application.Shared.Providers;
using Application.Shared.Records;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Application.Shared.Services;

/// <summary>
/// Orchestrates the synchronization process by managing execution scopes, resolving LOB-specific secrets,
/// and ensuring concurrency control for active sync tasks.
/// </summary>
/// <param name="serviceProvider">The root service provider used to create execution scopes.</param>
/// <param name="logger">The logger instance.</param>
public sealed class SyncOrchestrator(IServiceProvider serviceProvider,
                                     ILogger<SyncOrchestrator> logger) : ISyncOrchestrator
{
    /// <summary>
    /// Tracks active synchronization tasks to allow cancellation of stale or overlapping jobs.
    /// </summary>
    private static readonly ConcurrentDictionary<SyncKey, CancellationTokenSource> ActiveSyncs = new();

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <paramref name="lobName"/> is <c>null</c> or empty.</exception>
    public async Task ExecuteAsync(string lobName, SyncCategory category, CancellationToken externalToken)
    {
        if (string.IsNullOrWhiteSpace(lobName))
        {
            throw new ArgumentException("LOB name is required.", nameof(lobName));
        }

        SyncKey key = new(lobName, category);

        logger.LogDebug("[LOB: {Lob}] [Category: {Category}] Orchestration started.", lobName, category);

        using CancellationTokenSource cts =
            await PrepareCancellationTokenSourceAsync(key, externalToken).ConfigureAwait(false);

        try
        {
            await RunInScopeAsync(lobName, category, cts.Token).ConfigureAwait(false);
        }
        finally
        {
            // Remove the CTS from the active tracking map if it's still the instance we started with.
            ActiveSyncs.TryRemove(new KeyValuePair<SyncKey, CancellationTokenSource>(key, cts));
        }
    }

    #region ========== *** Private Methods *** ==========

    /// <summary>
    /// Prepares a linked <see cref="CancellationTokenSource"/> and performs an atomic swap if a job is already running for the same key.
    /// </summary>
    /// <param name="key">The unique key for the synchronization task.</param>
    /// <param name="externalToken">The external cancellation token (e.g., from the Function host).</param>
    /// <returns>A new <see cref="CancellationTokenSource"/> linked to the external token.</returns>
    private async Task<CancellationTokenSource> PrepareCancellationTokenSourceAsync(
        SyncKey key,
        CancellationToken externalToken)
    {
        // Suspend previous job if it exists for this LOB
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        //  Atomic swap logic: replace the old CTS with the new one and cancel the old one.
        while (true)
        {
            if (ActiveSyncs.TryGetValue(key, out CancellationTokenSource? oldCts))
            {
                if (!ActiveSyncs.TryUpdate(key, newCts, oldCts)) continue;

                try
                {
                    if (!oldCts.IsCancellationRequested)
                    {
                        logger.LogWarning(
                            "[LOB: {Lob}] [Category: {Category}] New job arrived. Signaling cancellation to previous job.",
                            key.LobName,
                            key.Category);

                        await oldCts.CancelAsync().ConfigureAwait(false);
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The old job finished and disposed its CTS just before we could cancel it.
                    // This is safe to ignore.
                }

                break;
            }

            if (ActiveSyncs.TryAdd(key, newCts)) break;
        }

        return newCts;
    }

    /// <summary>
    /// Executes the synchronization logic within a new DI scope.
    /// </summary>
    /// <param name="lobName">The name of the LOB.</param>
    /// <param name="category">The sync category.</param>
    /// <param name="token">The cancellation token.</param>
    private async Task RunInScopeAsync(string lobName, SyncCategory category, CancellationToken token)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        await InitializeLobContextAsync(scope, lobName, token).ConfigureAwait(false);

        ILogger<SyncOrchestrator> scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<SyncOrchestrator>>();

        using IDisposable loggingScope = scopedLogger.BeginOperationScope($"{category} Sync", lobName);

        try
        {
            ISyncCategoryHandler handler = ResolveHandler(scope, category);

            await handler.ExecuteAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("[LOB: {Lob}] [Category: {Category}] Sync job was successfully suspended/cancelled.",
                              lobName,
                              category);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LOB: {Lob}] [Category: {Category}] Critical failure in sync job.", lobName, category);

            throw;
        }
    }

    /// <summary>
    /// Initializes the LOB context for the current scope by resolving secrets and triggering context validation.
    /// </summary>
    /// <param name="scope">The current execution scope.</param>
    /// <param name="lobName">The name of the LOB.</param>
    /// <param name="token">The cancellation token.</param>
    private static async Task InitializeLobContextAsync(IServiceScope scope, string lobName, CancellationToken token)
    {
        ILobContextAccessor accessor = scope.ServiceProvider.GetRequiredService<ILobContextAccessor>();
        accessor.LobName = lobName;

        ILobSecretsResolver resolver = scope.ServiceProvider.GetRequiredService<ILobSecretsResolver>();
        await resolver.PopulateAsync(accessor, token).ConfigureAwait(false);

        // Resolving ILobContext here triggers the validation of the populated accessor values
        // (ClientId, ClientSecret, ConnStr) within the LobContext constructor/implementation.
        _ = scope.ServiceProvider.GetRequiredService<ILobContext>();
    }

    /// <summary>
    /// Resolves the appropriate <see cref="ISyncCategoryHandler"/> for the specified category from the scope.
    /// </summary>
    /// <param name="scope">The current execution scope.</param>
    /// <param name="category">The category to resolve.</param>
    /// <returns>The resolved handler.</returns>
    /// <exception cref="NotSupportedException">Thrown when no handler is registered for the category.</exception>
    private static ISyncCategoryHandler ResolveHandler(IServiceScope scope, SyncCategory category)
    {
        IEnumerable<ISyncCategoryHandler> handlers =
            scope.ServiceProvider.GetRequiredService<IEnumerable<ISyncCategoryHandler>>();

        ISyncCategoryHandler? handler = handlers.FirstOrDefault(h => h.Category == category);

        return handler ?? throw new NotSupportedException($"Sync category {category} is not registered.");
    }

    #endregion
}
