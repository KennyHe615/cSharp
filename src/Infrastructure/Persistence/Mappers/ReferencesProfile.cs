using Application.DTOs.References;

using AutoMapper;

using Infrastructure.Persistence.Entities.References;


namespace Infrastructure.Persistence.Mappers;

public sealed class ReferencesProfile : Profile
{
    public ReferencesProfile()
    {
        CreateMap<SkillDto, Skill>();

        CreateMap<PresenceDefinitionDto, PresenceDefinition>();

        CreateMap<GroupDto, Group>();

        CreateMap<WrapUpCodeDto, WrapUpCode>();
    }
}
