using Application.Enums;
using Application.Features.SyncTracking;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunRecoverySyncCommandValidatorTests
{
    private readonly RunRecoverySyncCommandValidator _sut = new RunRecoverySyncCommandValidator();

    [Fact]
    public void Validate_WithIntervalOnly_AnalyticsCategory_ShouldNotHaveValidationError()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    null);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithGenesysJobIdOnly_AnalyticsCategory_ShouldNotHaveValidationError()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.ConversationsDetails,
                                                                    null,
                                                                    null,
                                                                    "JOB-123");

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMissingIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.ConversationsAggregates,
                                                                    null,
                                                                    null,
                                                                    null);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    [Fact]
    public void Validate_WithBothIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    null,
                                                                    "JOB-123");

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Provide either Interval or GenesysJobId, not both.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdLongerThan100_ShouldHaveValidationError()
    {
        string longJobId = new string('A', 101);
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    null,
                                                                    null,
                                                                    longJobId);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData(" JOB-123")]
    [InlineData("JOB-123 ")]
    public void Validate_WithLeadingOrTrailingSpacesInGenesysJobId_ShouldHaveValidationError(string genesysJobId)
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    null,
                                                                    null,
                                                                    genesysJobId);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId must not contain leading or trailing spaces.");
    }

    [Fact]
    public void Validate_WithIntervalLongerThan50_ShouldHaveValidationError()
    {
        string longInterval = new string('I', 51);
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    longInterval,
                                                                    null,
                                                                    null);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithPageNumberLessThan1_ShouldHaveValidationError()
    {
        RunRecoverySyncCommand command = new RunRecoverySyncCommand(SyncCategory.UsersDetails,
                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                    0,
                                                                    null);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("PageNumber must be greater than or equal to 1 when provided.");
    }

    [Fact]
    public void Validate_WithReferencesCategory_ShouldHaveValidationError()
    {
        RunRecoverySyncCommand command =
            new RunRecoverySyncCommand(SyncCategory.Queue,
                                       "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                       null,
                                       null);

        TestValidationResult<RunRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage("Category is not supported for recovery sync.");
    }
}
