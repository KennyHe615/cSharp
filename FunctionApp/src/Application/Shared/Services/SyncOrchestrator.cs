using System.Collections.Concurrent;

using Application.Shared.Context;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace Application.Shared.Services;

public sealed class SyncOrchestrator(IServiceProvider serviceProvider,
                                     ILogger<SyncOrchestrator> logger)
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> ActiveSyncs =
        new(StringComparer.OrdinalIgnoreCase);

    public async Task ExecuteSyncAsync(string lobName, CancellationToken externalToken)
    {
        if (string.IsNullOrWhiteSpace(lobName)) throw new ArgumentException("LOB name is required.", nameof(lobName));

        using CancellationTokenSource cts = await PrepareCancellationTokenSourceAsync(lobName, externalToken);

        try
        {
            await RunSyncInScopeAsync(lobName, cts.Token);
        }
        finally
        {
            ActiveSyncs.TryRemove(new KeyValuePair<string, CancellationTokenSource>(lobName, cts));
        }
    }

    #region ========== *** Private Methods *** ==========

    private async Task<CancellationTokenSource> PrepareCancellationTokenSourceAsync(
        string lobName,
        CancellationToken externalToken)
    {
        // Suspend previous job if it exists for this LOB
        CancellationTokenSource newCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        //  Atomic swap logic: replace the old CTS with the new one and cancel the old one.
        //  We avoid the AddOrUpdate lambda to eliminate "Captured variable disposed" warnings
        //  and use TryUpdate to ensure we only cancel the correct previous instance.
        while (true)
        {
            if (ActiveSyncs.TryGetValue(lobName, out CancellationTokenSource? oldCts))
            {
                if (!ActiveSyncs.TryUpdate(lobName, newCts, oldCts)) continue;

                try
                {
                    if (!oldCts.IsCancellationRequested)
                    {
                        logger.LogWarning("[LOB: {Lob}] New job arrived. Signaling cancellation to previous job.",
                                          lobName);

                        await oldCts.CancelAsync();
                    }
                }
                catch (ObjectDisposedException)
                {
                    // The old job finished and disposed its CTS just before we could cancel it.
                    // This is safe to ignore.
                }

                break;
            }

            if (ActiveSyncs.TryAdd(lobName, newCts))
            {
                break;
            }
        }

        return newCts;
    }

    private async Task RunSyncInScopeAsync(string lobName, CancellationToken token)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        InitializeLobContext(scope, lobName);

        try
        {
            // TODO: Wait for implement
            // IReferencesSyncService syncService = scope.ServiceProvider.GetRequiredService<IReferencesSyncService>();
            // if (syncService == null)
            // {
            //     logger.LogError("[LOB: {Lob}] IReferencesSyncService is not registered.", lobName);
            //
            //     return;
            // }
            //
            // await syncService.SyncAllAsync(token);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("[LOB: {Lob}] Sync job was successfully suspended/cancelled.", lobName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[LOB: {Lob}] Critical failure in sync job.", lobName);

            throw;
        }
    }

    private static void InitializeLobContext(IServiceScope scope, string lobName)
    {
        ILobContextAccessor accessor = scope.ServiceProvider.GetRequiredService<ILobContextAccessor>();
        accessor.LobName = lobName;

        // Resolve ILobContext once to ensure the settings exist for this LOB name
        _ = scope.ServiceProvider.GetRequiredService<ILobContext>();
    }

    #endregion
}
