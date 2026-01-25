using FunctionApp.Application.References.DTOs;


namespace FunctionApp.Application.References.Clients;

public interface IReferencesClient
{
    Task<List<GroupResponseDto>> GetGroupsAsync(CancellationToken cancellationToken);

    Task<List<PresenceDefinitionResponseDto>> GetPresenceDefinitionsAsync(CancellationToken cancellationToken);

    Task<List<SkillResponseDto>> GetSkillsAsync(CancellationToken cancellationToken);
}
