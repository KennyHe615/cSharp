using Application.Dtos.References;


namespace Application.References;

/// <summary>
/// Domain-grouped repository for all Reference-related persistence operations.
/// </summary>
public interface IReferencesRepository
{
    /// <summary>
    /// Synchronizes the provided skills with the database.
    /// </summary>
    Task UpsertSkillsAsync(IReadOnlyCollection<SkillResponse> skills, CancellationToken ct);

    /// <summary>
    /// Synchronizes the provided presence_definitions with the database.
    /// </summary>
    Task UpsertPresenceDefinitionsAsync(IReadOnlyCollection<PresenceDefinitionResponse> presenceDefinitions,
                                        CancellationToken ct);

    /// <summary>
    /// Synchronizes the provided groups with the database.
    /// </summary>
    Task UpsertGroupsAsync(IReadOnlyCollection<GroupResponse> groups, CancellationToken ct);
}
