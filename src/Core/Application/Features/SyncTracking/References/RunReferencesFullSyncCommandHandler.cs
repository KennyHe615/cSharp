using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking.References;

/// <summary>
/// Handles references full-sync command by delegating to the sync request/run orchestration pipeline.
/// </summary>
public sealed class RunReferencesFullSyncCommandHandler(ISyncRequestRepository syncRequestRepository,
                                                        ISyncRequestRunner syncRequestRunner)
    : IRequestHandler<RunReferencesFullSyncCommand, long>
{
    /// <inheritdoc />
    public async Task<long> Handle(RunReferencesFullSyncCommand request, CancellationToken ct = default)
    {
        const SyncMode mode = SyncMode.Incremental;// References supports full-refresh via incremental mode only.

        long requestId = await syncRequestRepository.CreateOrGetByScopeAsync(request.Category.ToString(),
                                                                             mode,
                                                                             null,
                                                                             null,
                                                                             null,
                                                                             ct)
                                                    .ConfigureAwait(false);

        await syncRequestRunner.ExecuteAsync(requestId, ct)
                               .ConfigureAwait(false);

        return requestId;
    }
}
