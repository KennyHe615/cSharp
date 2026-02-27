using Application.Abstractions.Normalization;
using Application.Contracts.ExternalApis.Genesys.References;
using Application.DTOs.References;
using Application.Enums;

using SharedKernel.Extensions;


namespace Application.Normalizers.Genesys;

/// <summary>
/// Applies explicit normalization rules from Genesys reference payloads to application DTOs.
/// </summary>
public sealed class ReferencesNormalizer : IReferencesNormalizer
{
    public IReadOnlyCollection<SkillDto> NormalizeSkills(IReadOnlyCollection<SkillRawContract> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return responses.Select(skill => new SkillDto
                                         {
                                             Id = skill.Id,
                                             Name = skill.Name,
                                             DateModified = skill.DateModified,
                                             State = skill.State ?? StateKind.Inactive,
                                             Version = skill.Version
                                         })
                        .ToList();
    }

    public IReadOnlyCollection<PresenceDefinitionDto> NormalizePresenceDefinitions(
        IReadOnlyCollection<PresenceDefinitionRawContract> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return responses.Select(presence => new PresenceDefinitionDto
                                            {
                                                Id = presence.Id,
                                                Type = presence.Type,
                                                LanguageLabel =
                                                    ResolveLanguageLabel(presence
                                                                            .LanguageLabels),
                                                SystemPresence = presence.SystemPresence,
                                                DivisionId = presence.DivisionId,
                                                Deactivated = presence.Deactivated
                                            })
                        .ToList();
    }

    public IReadOnlyCollection<GroupDto> NormalizeGroups(IReadOnlyCollection<GroupRawContract> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return responses.Select(group => new GroupDto
                                         {
                                             Id = group.Id,
                                             Name = group.Name,
                                             Description = group.Description,
                                             DateModified = group.DateModified,
                                             MemberCount = group.MemberCount,
                                             State = group.State,
                                             Version = group.Version,
                                             Type = group.Type,
                                             RulesVisible = group.RulesVisible,
                                             Visibility = group.Visibility,
                                             ChatJabberId = group.Chat?.GetStringByPath("jabberId"),
                                             RolesEnabled = group.RolesEnabled,
                                             IncludeOwners = group.IncludeOwners
                                         })
                        .ToList();
    }

    public IReadOnlyCollection<WrapUpCodeDto> NormalizeWrapUpCodes(IReadOnlyCollection<WrapUpCodeRawContract> responses)
    {
        ArgumentNullException.ThrowIfNull(responses);

        return responses.Select(code => new WrapUpCodeDto
                                        {
                                            Id = code.Id,
                                            Name = code.Name,
                                            DivisionId = code.Division?.GetStringByPath("id"),
                                            DivisionName = code.Division?.GetStringByPath("name"),
                                            DateCreated = code.DateCreated,
                                            DateModified = code.DateModified,
                                            CreatedBy = code.CreatedBy,
                                            ModifiedBy = code.ModifiedBy,
                                            // Genesys wrap-up response currently does not provide state.
                                            State = null
                                        })
                        .ToList();
    }

    private static string? ResolveLanguageLabel(Dictionary<string, string>? labels)
    {
        if (labels is null || labels.Count == 0)
        {
            return null;
        }

        return labels.GetStringByPath("en_US")
               ?? labels.GetStringByPath("en")
               ?? labels.Values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
    }
}
