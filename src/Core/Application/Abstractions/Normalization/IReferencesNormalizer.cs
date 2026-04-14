using Application.Contracts.ExternalApis.Genesys.References;
using Application.DTOs.References;


namespace Application.Abstractions.Normalization;

/// <summary>
/// Normalizes raw Genesys reference responses into persistence-ready application DTOs.
/// </summary>
public interface IReferencesNormalizer
{
    /// <summary>
    /// Normalizes raw skill payloads.
    /// </summary>
    /// <param name="responses">Raw skill contracts from the provider.</param>
    /// <returns>A read-only collection of normalized <see cref="SkillDto"/> instances.</returns>
    IReadOnlyCollection<SkillDto> NormalizeSkills(IReadOnlyCollection<SkillRawContract> responses);

    /// <summary>
    /// Normalizes raw presence definition payloads.
    /// </summary>
    /// <param name="responses">Raw presence definition contracts from the provider.</param>
    /// <returns>A read-only collection of normalized <see cref="PresenceDefinitionDto"/> instances.</returns>
    IReadOnlyCollection<PresenceDefinitionDto> NormalizePresenceDefinitions(
        IReadOnlyCollection<PresenceDefinitionRawContract> responses);

    /// <summary>
    /// Normalizes raw group payloads.
    /// </summary>
    /// <param name="responses">Raw group contracts from the provider.</param>
    /// <returns>A read-only collection of normalized <see cref="GroupDto"/> instances.</returns>
    IReadOnlyCollection<GroupDto> NormalizeGroups(IReadOnlyCollection<GroupRawContract> responses);

    /// <summary>
    /// Normalizes raw wrap-up code payloads.
    /// </summary>
    /// <param name="responses">Raw wrap-up code contracts from the provider.</param>
    /// <returns>A read-only collection of normalized <see cref="WrapUpCodeDto"/> instances.</returns>
    IReadOnlyCollection<WrapUpCodeDto> NormalizeWrapUpCodes(IReadOnlyCollection<WrapUpCodeRawContract> responses);
}
