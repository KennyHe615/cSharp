using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsRecoverySyncCommandValidatorTests
{
    private readonly RunAnalyticsRecoverySyncCommandValidator _sut = new RunAnalyticsRecoverySyncCommandValidator();

    #region ========== *** Validate_Interval *** ==========

    [Fact]
    public void Validate_WithIntervalOnly_AnalyticsCategory_ShouldNotHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                            null,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithIntervalLongerThan50_ShouldHaveValidationError()
    {
        string longInterval = new string('I', 51);
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            longInterval,
                                                            null,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval cannot exceed 50 characters.");
    }

    [Fact]
    public void Validate_WithPageNumberLessThan1_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                            0,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("PageNumber must be greater than or equal to 1 when provided.");
    }

    #endregion

    #region ========== *** Validate_Category *** ==========

    [Fact]
    public void Validate_InvalidCategory_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand((SyncAnalyticsCategory)(-1),
                                                            "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                            null,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage("Category is invalid.");
    }

    [Theory]
    [InlineData(SyncAnalyticsCategory.UsersDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsAggregates)]
    public void Validate_EachAnalyticsCategory_WithIntervalOnly_ShouldNotHaveValidationError(
                    SyncAnalyticsCategory category)
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(category,
                                                            "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                            null,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    [Fact]
    public void Validate_WithGenesysJobIdOnly_AnalyticsCategory_ShouldNotHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                            null,
                                                            null,
                                                            "JOB-123");

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMissingIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsAggregates,
                                                            null,
                                                            null,
                                                            null);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Either Interval or GenesysJobId must be provided.");
    }

    [Fact]
    public void Validate_WithBothIntervalAndGenesysJobId_ShouldHaveValidationError()
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                            null,
                                                            "JOB-123");

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x)
              .WithErrorMessage("Provide either Interval or GenesysJobId, not both.");
    }

    [Fact]
    public void Validate_WithGenesysJobIdLongerThan100_ShouldHaveValidationError()
    {
        string longJobId = new string('A', 101);
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            null,
                                                            null,
                                                            longJobId);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId cannot exceed 100 characters.");
    }

    [Theory]
    [InlineData(" JOB-123")]
    [InlineData("JOB-123 ")]
    public void Validate_WithLeadingOrTrailingSpacesInGenesysJobId_ShouldHaveValidationError(string genesysJobId)
    {
        RunAnalyticsRecoverySyncCommand command =
                        new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                            null,
                                                            null,
                                                            genesysJobId);

        TestValidationResult<RunAnalyticsRecoverySyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.GenesysJobId)
              .WithErrorMessage("GenesysJobId must not contain leading or trailing spaces.");
    }
}
