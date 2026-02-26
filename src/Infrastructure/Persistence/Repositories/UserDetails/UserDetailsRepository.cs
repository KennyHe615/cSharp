using Application.Abstractions.Persistence;
using Application.DTOs.UserDetails;

using AutoMapper;

using Infrastructure.Persistence.Entities.UserDetails;


namespace Infrastructure.Persistence.Repositories.UserDetails;

public sealed class UserDetailsRepository(IUnitOfWork uow,
                                          IMapper mapper) : IUserDetailsRepository
{
    public async Task UpsertUserDetailsAsync(IReadOnlyCollection<PrimaryPresenceDto> primaryPresence,
                                             IReadOnlyCollection<RoutingStatusDto> routingStatus,
                                             CancellationToken ct)
    {
        List<PrimaryPresenceEntity> ppEntities = mapper.Map<List<PrimaryPresenceEntity>>(primaryPresence);
        List<RoutingStatusEntity> rsEntities = mapper.Map<List<RoutingStatusEntity>>(routingStatus);

        await uow.UpsertRangeAsync(ppEntities, null, ct)
                 .ConfigureAwait(false);
        await uow.UpsertRangeAsync(rsEntities, null, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }
}
