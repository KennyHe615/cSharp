using Application.Contracts.ExternalApis.Genesys.References;

using AutoMapper;

using Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Contracts;


namespace Infrastructure.ExternalApis.Providers.Genesys.Modules.References.Mappers;

public sealed class ReferencesRawContractProfile : Profile
{
    public ReferencesRawContractProfile()
    {
        CreateMap<SkillResponse, SkillRawContract>();

        CreateMap<PresenceDefinitionResponse, PresenceDefinitionRawContract>();

        CreateMap<GroupResponse, GroupRawContract>();

        CreateMap<WrapUpCodeResponse, WrapUpCodeRawContract>();
    }
}
