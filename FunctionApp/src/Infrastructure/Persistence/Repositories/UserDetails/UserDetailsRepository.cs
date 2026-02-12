using Application.Common.Abstractions.Persistence;
using Application.Dtos.UserDetails;
using Application.UserDetails;

using AutoMapper;

using Infrastructure.Persistence.Entities.UserDetails;


namespace Infrastructure.Persistence.Repositories.UserDetails;

/// <summary>
/// Repository for persisting user details data to the database.
/// </summary>
public class UserDetailsRepository(IUnitOfWork uow,
                                   IMapper mapper) : IUserDetailsRepository
{
    /// <inheritdoc />
    public async Task UpsertUserDetailsAsync(List<PrimaryPresenceDto> primaryPresence,
                                             List<RoutingStatusDto> routingStatus,
                                             CancellationToken ct)
    {
        List<PrimaryPresenceEntity>? ppEntities = mapper.Map<List<PrimaryPresenceEntity>>(primaryPresence);
        List<RoutingStatusEntity>? rsEntities = mapper.Map<List<RoutingStatusEntity>>(routingStatus);

        await uow.UpsertRangeAsync(ppEntities, null, ct);

        await uow.UpsertRangeAsync(rsEntities, null, ct);

        await uow.SaveChangesAsync(ct);
    }
}
