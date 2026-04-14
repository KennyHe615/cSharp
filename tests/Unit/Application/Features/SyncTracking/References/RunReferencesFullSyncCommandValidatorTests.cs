using Application.Enums;
using Application.Features.SyncTracking.References;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking.References;

public sealed class RunReferencesFullSyncCommandValidatorTests
{
    private readonly RunReferencesFullSyncCommandValidator _sut = new RunReferencesFullSyncCommandValidator();

    [Fact]
    public void Validate_WithDefinedCategory_ShouldNotHaveValidationError()
    {
        RunReferencesFullSyncCommand command = new RunReferencesFullSyncCommand(SyncReferenceCategory.WrapUpCode);

        TestValidationResult<RunReferencesFullSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithOutOfRangeCategory_ShouldHaveValidationError_WithAllowedValuesMessage()
    {
        RunReferencesFullSyncCommand command = new RunReferencesFullSyncCommand((SyncReferenceCategory)999);

        TestValidationResult<RunReferencesFullSyncCommand> result = _sut.TestValidate(command);

        string expectedMessage =
            $"Category is invalid. Allowed values: {string.Join(", ", Enum.GetNames<SyncReferenceCategory>())}.";

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage(expectedMessage);
    }
}
