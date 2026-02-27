using Application.DTOs.References;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Domain-grouped repository for all Reference-related persistence operations.
/// </summary>
public interface IReferencesRepository
{
    /// <summary>
    /// Synchronizes the provided skills with the database.
    /// </summary>
    Task UpsertSkillsAsync(IReadOnlyCollection<SkillDto> skills, CancellationToken ct);

    /// <summary>
    /// Synchronizes the provided presence_definitions with the database.
    /// </summary>
    Task UpsertPresenceDefinitionsAsync(IReadOnlyCollection<PresenceDefinitionDto> presenceDefinitions,
                                        CancellationToken ct);

    /// <summary>
    /// Synchronizes the provided groups with the database.
    /// </summary>
    Task UpsertGroupsAsync(IReadOnlyCollection<GroupDto> groups, CancellationToken ct);

    /// <summary>
    /// Synchronizes the provided wrap_up_codes with the database.
    /// </summary>
    Task UpsertWrapUpCodesAsync(IReadOnlyCollection<WrapUpCodeDto> wrapUpCodes, CancellationToken ct);
}
