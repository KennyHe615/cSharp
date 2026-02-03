using Application.Dtos.References;

using AutoMapper;

using Infrastructure.Persistence.Entities.References;
using Infrastructure.Persistence.Mappers.Shared;

using Shared.Extensions;


namespace Infrastructure.Persistence.Mappers;

public class ReferencesProfile : Profile
{
    public ReferencesProfile()
    {
        #region ========== *** Skill *** ==========

        CreateMap<SkillResponse, Skill>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.DateModified,
                       opt => opt.MapFrom<EstOffsetResolver, DateTimeOffset?>(src => src.DateModified));

        #endregion

        #region ========== *** Presence Definition *** ==========

        CreateMap<PresenceDefinitionResponse, PresenceDefinition>()
            .ForMember(dest => dest.LanguageLabel,
                       opt => opt.MapFrom(src => src.LanguageLabels != null
                                              ? src.LanguageLabels.GetValueOrDefault("en").Truncate(255) ??
                                                src.LanguageLabels.GetValueOrDefault("en_US").Truncate(255) ?? "N/A"
                                              : "N/A"));

        #endregion

        #region ========== *** Group *** ==========

        CreateMap<GroupResponse, Group>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Truncate(255)))
            .ForMember(dest => dest.DateModified,
                       opt => opt.MapFrom<EstOffsetResolver, DateTimeOffset?>(src => src.DateModified))
            .ForMember(dest => dest.ChatJabberId,
                       opt => opt.MapFrom(src => (src.Chat != null ? src.Chat.GetValueOrDefault("jabberId") : "N/A")
                                              .Truncate(255)));

        #endregion
    }
}
