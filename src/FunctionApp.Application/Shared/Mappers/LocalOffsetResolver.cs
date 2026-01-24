using AutoMapper;

using FunctionApp.Application.Shared.Providers;


namespace FunctionApp.Application.Shared.Mappers;

/// <summary>
/// A shared AutoMapper resolver that converts UTC DateTimeOffset to the local offset
/// provided by IDateTimeProvider. Used across all application modules.
/// </summary>
public class LocalOffsetResolver(IDateTimeProvider dateTimeProvider)
    : IMemberValueResolver<object, object, DateTimeOffset?, DateTimeOffset?>
{
    public DateTimeOffset? Resolve(object source,
                                   object destination,
                                   DateTimeOffset? sourceMember,
                                   DateTimeOffset? destMember,
                                   ResolutionContext context)
    {
        return sourceMember?.ToOffset(dateTimeProvider.LocalOffset);
    }
}
