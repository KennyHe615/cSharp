using Application.Contracts.ExternalApis.Genesys.References;
using Application.DTOs.References;


namespace Application.Abstractions.Normalization;

/// <summary>
/// Normalizes raw Genesys reference responses into persistence-ready application DTOs.
/// </summary>
public interface IReferencesNormalizer
{
    IReadOnlyCollection<SkillDto> NormalizeSkills(IReadOnlyCollection<SkillRawContract> responses);

    IReadOnlyCollection<PresenceDefinitionDto> NormalizePresenceDefinitions(
        IReadOnlyCollection<PresenceDefinitionRawContract> responses);

    IReadOnlyCollection<GroupDto> NormalizeGroups(IReadOnlyCollection<GroupRawContract> responses);

    IReadOnlyCollection<WrapUpCodeDto> NormalizeWrapUpCodes(IReadOnlyCollection<WrapUpCodeRawContract> responses);
}
