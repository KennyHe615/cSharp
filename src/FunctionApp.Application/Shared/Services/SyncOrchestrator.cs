using System.Collections.Concurrent;

using FunctionApp.Application.References.Services;
using FunctionApp.Application.Shared.Context;
using FunctionApp.Configuration.Options;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;


namespace FunctionApp.Application.Shared.Services;

public sealed class SyncOrchestrator(IServiceProvider serviceProvider,
                                     IConfiguration configuration,
                                     ILogger<SyncOrchestrator> logger)
{
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> ActiveSyncs = new();

    public async Task ExecuteSyncAsync(string lobName, CancellationToken externalToken)
    {
        LobSettings settings = GetLobSettings(lobName);

        using CancellationTokenSource cts = await PrepareCancellationTokenSourceAsync(lobName, externalToken);

        try
        {
            await RunSyncInScopeAsync(lobName, settings, cts.Token);
        }
        finally
        {
            ActiveSyncs.TryRemove(new KeyValuePair<string, CancellationTokenSource>(lobName, cts));
        }
    }

    #region ========== *** Private Methods *** ==========

    private LobSettings GetLobSettings(string lobName)
    {
        // Discovery & Validation
        IConfigurationSection lobsSection = configuration.GetSection(MultiLobOptions.SectionName);
        LobSettings? settings = lobsSection.GetSection(lobName).Get<LobSettings>();

        if (settings == null ||
            string.IsNullOrEmpty(settings.GenesysClientId) ||
            string.IsNullOrEmpty(settings.GenesysClientSecret) ||
            string.IsNullOrEmpty(settings.DatabaseConnectionString))
        {
            throw new InvalidOperationException(
                $"LOB configuration for '{lobName}' was not found or is incomplete in the '{MultiLobOptions.SectionName}' section.");
        }

        return settings;
    }

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

    private async Task RunSyncInScopeAsync(string lobName, LobSettings settings, CancellationToken token)
    {
        using IServiceScope scope = serviceProvider.CreateScope();

        InitializeLobContext(scope, lobName, settings);

        try
        {
            IReferencesSyncService syncService = scope.ServiceProvider.GetRequiredService<IReferencesSyncService>();

            await syncService.SyncAllAsync(token);
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

    private static void InitializeLobContext(IServiceScope scope, string lobName, LobSettings settings)
    {
        ILobContext lobContext = scope.ServiceProvider.GetRequiredService<ILobContext>();
        lobContext.LobName = lobName.ToUpperInvariant(); // Ensure it's stored as NTT/LCL/etc.
        lobContext.LobSettings = settings;
    }

    #endregion
}
