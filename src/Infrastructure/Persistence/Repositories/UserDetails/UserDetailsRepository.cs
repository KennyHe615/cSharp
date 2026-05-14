using Application.Abstractions.Persistence;
using Application.DTOs.UsersDetails;

using AutoMapper;

using Infrastructure.Persistence.Entities.UserDetails;


namespace Infrastructure.Persistence.Repositories.UserDetails;

public sealed class UserDetailsRepository(IUnitOfWork uow,
                                          IMapper mapper) : IUserDetailsRepository
{
    /// <inheritdoc />
    public async Task UpsertUserDetailsAsync(IReadOnlyCollection<PrimaryPresenceDto> primaryPresence,
                                             IReadOnlyCollection<RoutingStatusDto> routingStatus,
                                             CancellationToken ct)
    {
        List<PrimaryPresenceEntity> ppEntities = mapper.Map<List<PrimaryPresenceEntity>>(primaryPresence);
        List<RoutingStatusEntity> rsEntities = mapper.Map<List<RoutingStatusEntity>>(routingStatus);

        await uow.UpsertRangeWithMergeAsync(ppEntities, ApplyPrimaryPresenceLatestState, ct)
                 .ConfigureAwait(false);

        await uow.UpsertRangeWithMergeAsync(rsEntities, ApplyRoutingStatusLatestState, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    #region ========== *** Private Section *** ==========

    private static void ApplyPrimaryPresenceLatestState(PrimaryPresenceEntity existing, PrimaryPresenceEntity incoming)
    {
        if (!ShouldApplyIncoming(existing.EndTimeUtc, incoming.EndTimeUtc)) return;

        existing.EndTimeUtc = incoming.EndTimeUtc;
        existing.StartTimeEastern = incoming.StartTimeEastern;
        existing.SystemPresence = incoming.SystemPresence;
        existing.OrganizationPresenceId = incoming.OrganizationPresenceId;
    }

    private static void ApplyRoutingStatusLatestState(RoutingStatusEntity existing, RoutingStatusEntity incoming)
    {
        if (!ShouldApplyIncoming(existing.EndTimeUtc, incoming.EndTimeUtc)) return;

        existing.EndTimeUtc = incoming.EndTimeUtc;
        existing.StartTimeEastern = incoming.StartTimeEastern;
        existing.RoutingStatus = incoming.RoutingStatus;
    }

    private static bool ShouldApplyIncoming(DateTimeOffset? existingEndTimeUtc, DateTimeOffset? incomingEndTimeUtc)
    {
        if (!existingEndTimeUtc.HasValue) return true;

        return incomingEndTimeUtc.HasValue && incomingEndTimeUtc.Value >= existingEndTimeUtc.Value;
    }

    #endregion
}
