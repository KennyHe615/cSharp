using AutoMapper;

using FunctionApp.Application.References.DTOs;
using FunctionApp.Application.Shared.Mappers;
using FunctionApp.Domain.Entities.References;


namespace FunctionApp.Application.References.Mappers;

/// <summary>
/// Consolidate AutoMapper profile for all Reference-type entities.
/// </summary>
public class ReferencesProfile : Profile
{
    public ReferencesProfile()
    {
        // Skill Mapping
        CreateMap<SkillDto, Skill>()
            .ForMember(dest => dest.DateModified,
                       opt => opt.MapFrom<LocalOffsetResolver, DateTimeOffset?>(src => src.DateModified))
            .ForMember(dest => dest.AppCreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AppUpdatedAt, opt => opt.Ignore());

        // Add other reference mappings here (e.g., LanguageDto -> Language)
        // CreateMap<LanguageDto, Language>()
        //     .ForMember(dest => dest.AppCreatedAt, opt => opt.Ignore())
        //     .ForMember(dest => dest.AppUpdatedAt, opt => opt.Ignore());
    }
}
