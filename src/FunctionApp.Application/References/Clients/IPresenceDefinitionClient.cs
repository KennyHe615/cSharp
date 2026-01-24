using FunctionApp.Application.References.DTOs;


namespace FunctionApp.Application.References.Clients;

public interface IPresenceDefinitionClient
{
    Task<List<PresenceDefinitionDto>> GetPresenceDefinitionsAsync(CancellationToken cancellationToken);
}
