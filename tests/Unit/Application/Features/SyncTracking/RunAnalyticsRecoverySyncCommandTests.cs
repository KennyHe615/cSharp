using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Xunit;

namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsRecoverySyncCommandTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        RunAnalyticsRecoverySyncCommand command =
            new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                2,
                                                "JOB-123");

        Assert.Equal(SyncAnalyticsCategory.UsersDetails, command.Category);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", command.Interval);
        Assert.Equal(2, command.PageNumber);
        Assert.Equal("JOB-123", command.GenesysJobId);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        RunAnalyticsRecoverySyncCommand left =
            new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                null,
                                                null,
                                                "JOB-999");
        RunAnalyticsRecoverySyncCommand right =
            new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                null,
                                                null,
                                                "JOB-999");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ValueEquality_DifferentValues_ShouldNotBeEqual()
    {
        RunAnalyticsRecoverySyncCommand left =
            new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                null,
                                                null,
                                                "JOB-999");
        RunAnalyticsRecoverySyncCommand right = new RunAnalyticsRecoverySyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                                    null,
                                                                                    null);

        Assert.NotEqual(left, right);
    }
}
