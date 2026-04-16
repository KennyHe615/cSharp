using Application.Enums;
using Application.Mediator;


namespace Application.Features.SyncTracking.References;

/// <summary>
/// Requests execution of a full references sync for a single references category.
/// </summary>
/// <param name="Category">Target references category to run.</param>
public sealed record RunReferencesFullSyncCommand(SyncReferenceCategory Category) : IRequest<long>;
