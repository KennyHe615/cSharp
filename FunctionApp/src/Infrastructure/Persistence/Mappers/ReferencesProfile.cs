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
            .ForMember(dest => dest.DateModified, opt => opt.MapFromEst(src => src.DateModified));

        #endregion

        #region ========== *** Presence Definition *** ==========

        CreateMap<PresenceDefinitionResponse, PresenceDefinition>()
            .ForMember(dest => dest.LanguageLabel,
                       opt => opt.MapFrom(src => src.LanguageLabels.GetValue("en", 255) ??
                                                 src.LanguageLabels.GetValue("en_US", 255)));

        #endregion

        #region ========== *** Group *** ==========

        CreateMap<GroupResponse, Group>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description.Truncate(255)))
            .ForMember(dest => dest.DateModified, opt => opt.MapFromEst(src => src.DateModified))
            .ForMember(dest => dest.ChatJabberId, opt => opt.MapFrom(src => src.Chat.GetValue("jabberId", 255)));

        #endregion

        #region ========== *** Wrapup Code *** ==========

        CreateMap<WrapupCodeResponse, WrapupCode>()
            .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Truncate(255)))
            .ForMember(dest => dest.DivisionId, opt => opt.MapFrom(src => src.Division.GetValue("id").ToGuid()))
            .ForMember(dest => dest.DivisionName, opt => opt.MapFrom(src => src.Division.GetValue("name", 255)))
            .ForMember(dest => dest.DateCreated, opt => opt.MapFromEst(src => src.DateCreated))
            .ForMember(dest => dest.DateModified, opt => opt.MapFromEst(src => src.DateModified));

        #endregion
    }
}
