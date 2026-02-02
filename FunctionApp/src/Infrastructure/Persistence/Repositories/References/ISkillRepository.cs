using Application.Dtos.References;


namespace Infrastructure.Persistence.Repositories.References;

/// <summary>
/// Repository interface for managing Skill entities in the persistence layer.
/// </summary>
public interface ISkillRepository
{
    /// <summary>
    /// Performs an upsert operation on a collection of skills.
    /// </summary>
    /// <param name="skills">The collection of skill responses to be synchronized with the database.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task representing the asynchronous upsert operation.</returns>
    Task UpsertSkillAsync(IReadOnlyCollection<SkillResponse> skills, CancellationToken ct);
}
