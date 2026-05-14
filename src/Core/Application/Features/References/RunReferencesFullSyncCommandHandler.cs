using Application.Abstractions.Orchestration.Sync;
using Application.Abstractions.Persistence.SyncTracking;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.References;

/// <summary>
/// Handles references full-sync by resolving incremental request scope and delegating execution.
/// </summary>
public sealed class RunReferencesFullSyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                        ISyncRequestRunner syncRequestRunner)
        : IRequestHandler<RunReferencesFullSyncCommand, long>
{
    /// <inheritdoc />
    public async Task<long> Handle(RunReferencesFullSyncCommand request, CancellationToken ct = default)
    {
        const SyncMode mode = SyncMode.Full;

        SyncRequestResolveResult resolveResult =
                await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                    mode,
                                                                    null,
                                                                    null,
                                                                    null,
                                                                    ct)
                                           .ConfigureAwait(false);

        await syncRequestRunner.ExecuteAsync(resolveResult.Id, ct)
                               .ConfigureAwait(false);

        return resolveResult.Id;
    }
}
