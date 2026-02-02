using Application.Dtos.References;
using Application.References;

using Infrastructure.Persistence.Entities.References;
using Infrastructure.Services;


namespace Infrastructure.Persistence.Repositories.References;

public sealed class ReferencesWriter(IMappedUpsertService mappedUpsert) : IReferencesWriter
{
    public Task UpsertPresenceDefinitionsAsync(IReadOnlyList<PresenceDefinitionResponse> items,
                                               CancellationToken cancellationToken)
    {
        return mappedUpsert.UpsertAsync<PresenceDefinitionResponse, PresenceDefinition>(items, cancellationToken);
    }

    public Task UpsertGroupsAsync(IReadOnlyList<GroupResponse> items, CancellationToken cancellationToken)
    {
        return mappedUpsert.UpsertAsync<GroupResponse, Group>(items, cancellationToken);
    }
}
