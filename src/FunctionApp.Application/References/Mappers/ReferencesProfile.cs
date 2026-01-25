using AutoMapper;

using FunctionApp.Application.References.DTOs;
using FunctionApp.Application.Shared.Extensions;
using FunctionApp.Application.Shared.Mappers;
using FunctionApp.Domain.Entities.References;


namespace FunctionApp.Application.References.Mappers;

public class ReferencesProfile : Profile
{
    public ReferencesProfile()
    {
        #region ========== *** Group *** ==========

        CreateMap<GroupResponseDto, Group>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Truncate(255)))
            .ForMember(dest => dest.DateModified,
                       opt => opt.MapFrom<LocalOffsetResolver, DateTimeOffset?>(src => src.DateModified))
            .ForMember(dest => dest.ChatJabberId,
                       opt => opt.MapFrom(src => (src.Chat != null ? src.Chat.GetValueOrDefault("jabberId") : "N/A")
                                              .Truncate(255)))
            .ForMember(dest => dest.AppCreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AppUpdatedAt, opt => opt.Ignore());

        #endregion

        #region ========== *** Presence Definition *** ==========

        CreateMap<PresenceDefinitionResponseDto, PresenceDefinition>()
            .ForMember(dest => dest.LanguageLabel,
                       opt => opt.MapFrom(src => src.LanguageLabels != null
                                              ? src.LanguageLabels.GetValueOrDefault("en").Truncate(255) ??
                                                src.LanguageLabels.GetValueOrDefault("en_US").Truncate(255) ?? "N/A"
                                              : "N/A"))
            .ForMember(dest => dest.AppCreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AppUpdatedAt, opt => opt.Ignore());

        #endregion

        #region ========== *** Skill *** ==========

        CreateMap<SkillResponseDto, Skill>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.DateModified,
                       opt => opt.MapFrom<LocalOffsetResolver, DateTimeOffset?>(src => src.DateModified))
            .ForMember(dest => dest.AppCreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.AppUpdatedAt, opt => opt.Ignore());

        #endregion
    }
}
