using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using FluentValidation.TestHelper;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsIncrementalSyncCommandValidatorTests
{
    private readonly RunAnalyticsIncrementalSyncCommandValidator _sut =
                    new RunAnalyticsIncrementalSyncCommandValidator();

    #region ========== *** Validate_Category *** ==========

    [Fact]
    public void Validate_InvalidCategory_ShouldHaveValidationError()
    {
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand((SyncAnalyticsCategory)(-1),
                                                               "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                               null);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Category)
              .WithErrorMessage("Category is invalid.");
    }

    [Fact]
    public void Validate_AnalyticsCategory_WithMissingInterval_ShouldHaveValidationError()
    {
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails, null, null);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval is required for analytics incremental sync.");
    }

    [Fact]
    public void Validate_AnalyticsCategory_WithIntervalAndValidPage_ShouldNotHaveValidationError()
    {
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                               "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                               1);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(SyncAnalyticsCategory.UsersDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsDetails)]
    [InlineData(SyncAnalyticsCategory.ConversationsAggregates)]
    public void Validate_EachAnalyticsCategory_WithMissingInterval_ShouldHaveValidationError(
                    SyncAnalyticsCategory category)
    {
        RunAnalyticsIncrementalSyncCommand command = new RunAnalyticsIncrementalSyncCommand(category, null, null);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval is required for analytics incremental sync.");
    }

    #endregion

    #region ========== *** Validate_Interval *** ==========

    [Fact]
    public void Validate_InvalidIntervalFormat_ShouldHaveValidationError()
    {
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                               "not-an-interval",
                                                               null);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval format is invalid. Expected UTC interval: yyyy-MM-ddTHH:mmZ/yyyy-MM-ddTHH:mmZ.");
    }

    [Fact]
    public void Validate_IntervalLongerThan50_ShouldHaveValidationError()
    {
        string longInterval = new string('I', 51);
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails, longInterval, null);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Interval)
              .WithErrorMessage("Interval cannot exceed 50 characters.");
    }

    #endregion

    [Fact]
    public void Validate_PageNumberLessThan1_ShouldHaveValidationError()
    {
        RunAnalyticsIncrementalSyncCommand command =
                        new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                               "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                               0);

        TestValidationResult<RunAnalyticsIncrementalSyncCommand> result = _sut.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.PageNumber)
              .WithErrorMessage("PageNumber must be greater than or equal to 1 when provided.");
    }
}
