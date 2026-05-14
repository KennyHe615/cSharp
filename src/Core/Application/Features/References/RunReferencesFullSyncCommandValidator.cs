using Application.Enums;

using FluentValidation;


namespace Application.Features.References;

/// <summary>
/// Validates <see cref="RunReferencesFullSyncCommand"/> shape and enum integrity.
/// This validator intentionally checks enum validity only; category support wiring is handled downstream.
/// </summary>
public sealed class RunReferencesFullSyncCommandValidator : AbstractValidator<RunReferencesFullSyncCommand>
{
    /// <summary>
    /// Creates validation rules for references full-sync command input.
    /// </summary>
    public RunReferencesFullSyncCommandValidator()
    {
        RuleFor(x => x.Category)
               .IsInEnum()
               .WithMessage($"Category is invalid. Allowed values: {string.Join(", ", Enum.GetNames<SyncReferenceCategory>())}.");
    }
}
