using Application.Abstractions.Persistence;
using Application.DTOs.References;

using AutoMapper;

using Infrastructure.ExternalApis.Genesys.Models.Enums;
using Infrastructure.Persistence.Entities.References;

using Group=Infrastructure.Persistence.Entities.References.Group;


namespace Infrastructure.Persistence.Repositories.References;

public sealed class ReferencesRepository(IUnitOfWork uow,
                                         IMapper mapper) : IReferencesRepository
{
    public async Task UpsertSkillsAsync(IReadOnlyCollection<SkillDto> skills, CancellationToken ct)
    {
        List<Skill> mappedEntities = mapper.Map<List<Skill>>(skills);

        await uow.UpsertRangeAsync(mappedEntities, s => s.State = State.Inactive, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    public async Task UpsertPresenceDefinitionsAsync(IReadOnlyCollection<PresenceDefinitionDto> presenceDefinitions,
                                                     CancellationToken ct)
    {
        List<PresenceDefinition> mappedEntities = mapper.Map<List<PresenceDefinition>>(presenceDefinitions);

        await uow.UpsertRangeAsync(mappedEntities, pd => pd.Deactivated = true, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    public async Task UpsertGroupsAsync(IReadOnlyCollection<GroupDto> groups, CancellationToken ct)
    {
        List<Group> mappedEntities = mapper.Map<List<Group>>(groups);

        await uow.UpsertRangeAsync(mappedEntities, g => g.State = State.Inactive, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }

    public async Task UpsertWrapUpCodesAsync(IReadOnlyCollection<WrapUpCodeDto> wrapUpCodes, CancellationToken ct)
    {
        List<WrapUpCode> mappedEntities = mapper.Map<List<WrapUpCode>>(wrapUpCodes);

        await uow.UpsertRangeAsync(mappedEntities, w => w.State = State.Inactive, ct)
                 .ConfigureAwait(false);

        await uow.SaveChangesAsync(ct)
                 .ConfigureAwait(false);
    }
}
