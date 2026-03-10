using Application.Enums;
using Application.Features.SyncTracking;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunIncrementalSyncCommandValidatorTests
{
    private readonly RunIncrementalSyncCommandValidator _sut = new RunIncrementalSyncCommandValidator();

    [Fact]
    public void Validate_InvalidCategory_ShouldHaveValidationError()
    {
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand((SyncCategory)(-1), null, null);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage("Category is invalid.");
    }

    [Fact]
    public void Validate_AnalyticsCategory_WithMissingInterval_ShouldHaveValidationError()
    {
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.UsersDetails, null, null);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval is required for analytics incremental sync.");
    }

    [Fact]
    public void Validate_ReferencesCategory_WithMissingInterval_ShouldNotHaveValidationError()
    {
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Queue, null, null);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Interval);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_IntervalLongerThan50_ShouldHaveValidationError()
    {
        string longInterval = new string('I', 51);
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Queue, longInterval, null);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_IntervalAt50Chars_ShouldNotHaveValidationError()
    {
        string interval = new string('I', 50);
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Queue, interval, null);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Interval);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_PageNumberLessThan1_ShouldHaveValidationError()
    {
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Flow, null, 0);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("PageNumber must be greater than or equal to 1 when provided.");
    }

    [Fact]
    public void Validate_PageNumberAt1_ShouldNotHaveValidationError()
    {
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Flow, null, 1);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.PageNumber);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_AnalyticsCategory_WithIntervalAndValidPage_ShouldNotHaveValidationError()
    {
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.ConversationsDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", 1);

        TestValidationResult<RunIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
