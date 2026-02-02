using AutoMapper;

using Shared.Providers;


namespace Infrastructure.Persistence.Mappers.Shared;

/// <summary>
/// A shared AutoMapper resolver that converts UTC DateTimeOffset to the est offset
/// provided by IDateTimeProvider. Used across all application modules.
/// </summary>
public class EstOffsetResolver(IDateTimeProvider dateTimeProvider)
    : IMemberValueResolver<object, object, DateTimeOffset?, DateTimeOffset?>
{
    public DateTimeOffset? Resolve(object source,
                                   object destination,
                                   DateTimeOffset? sourceMember,
                                   DateTimeOffset? destMember,
                                   ResolutionContext context)
    {
        return sourceMember is not null ? dateTimeProvider.ConvertToEst(sourceMember.Value) : null;
    }
}
