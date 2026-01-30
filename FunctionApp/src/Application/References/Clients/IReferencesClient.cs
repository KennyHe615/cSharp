using Shared.Genesys.Models.References;


namespace Application.References.Clients;

public interface IReferencesClient
{
    Task<List<GroupResponse>> GetGroupsAsync(CancellationToken cancellationToken);

    Task<List<PresenceDefinitionResponse>> GetPresenceDefinitionsAsync(CancellationToken cancellationToken);

    Task<List<SkillResponse>> GetSkillsAsync(CancellationToken cancellationToken);
}
