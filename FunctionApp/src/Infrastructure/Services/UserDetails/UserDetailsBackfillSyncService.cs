using Application.Common.Abstractions.Context;
using Application.Common.Abstractions.Services;
using Application.Normalizers.UserDetails;
using Application.UserDetails;

using Infrastructure.ExternalServices.Genesys.Providers;

using Microsoft.Extensions.Logging;


namespace Infrastructure.Services.UserDetails;

public class UserDetailsBackfillSyncService : UserDetailsSyncServiceBase
{
    private readonly IIntervalSubdivisionService _subdivisionService;
    private readonly UserDetailsHitCountProvider _hitCountProvider;

    public UserDetailsBackfillSyncService(IUserDetailsClient client,
                                          IIntervalSubdivisionService subdivisionService,
                                          IUserDetailsNormalizer normalizer,
                                          UserDetailsHitCountProvider hitCountProvider,
                                          IUserDetailsRepository repository,
                                          ILobContext lobContext,
                                          ILogger<UserDetailsBackfillSyncService> logger) : base(
        client,
        normalizer,
        repository,
        lobContext,
        logger)
    {
        _subdivisionService = subdivisionService ?? throw new ArgumentNullException(nameof(subdivisionService));
        _hitCountProvider = hitCountProvider ?? throw new ArgumentNullException(nameof(hitCountProvider));
    }

    public async Task RecoverFailedIntervalsAsync(CancellationToken ct)
    {
        // Placeholder
        await Task.CompletedTask;
    }
}
