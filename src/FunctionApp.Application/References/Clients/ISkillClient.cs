using FunctionApp.Application.References.DTOs;


namespace FunctionApp.Application.References.Clients;

public interface ISkillClient
{
    Task<List<SkillDto>> GetSkillsAsync(CancellationToken cancellationToken);
}
