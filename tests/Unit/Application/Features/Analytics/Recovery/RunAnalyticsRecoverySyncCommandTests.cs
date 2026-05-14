using Application.Enums;
using Application.Features.Analytics.Recovery;

using Xunit;


namespace tests.Unit.Application.Features.Analytics.Recovery;

public sealed class RunAnalyticsRecoverySyncCommandTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        RunAnalyticsRecoverySyncCommand command =
                new RunAnalyticsRecoverySyncCommand(101L,
                                                    SyncAnalyticsCategory.UsersDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    2,
                                                    "JOB-123");

        Assert.Equal(101L, command.RequestId);
        Assert.Equal(SyncAnalyticsCategory.UsersDetails, command.Category);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", command.Interval);
        Assert.Equal(2, command.PageNumber);
        Assert.Equal("JOB-123", command.GenesysJobId);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        RunAnalyticsRecoverySyncCommand left =
                new RunAnalyticsRecoverySyncCommand(151L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-999");
        RunAnalyticsRecoverySyncCommand right =
                new RunAnalyticsRecoverySyncCommand(151L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-999");

        Assert.Equal(left, right);
    }

    [Fact]
    public void ValueEquality_DifferentValues_ShouldNotBeEqual()
    {
        RunAnalyticsRecoverySyncCommand left =
                new RunAnalyticsRecoverySyncCommand(151L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    null,
                                                    null,
                                                    "JOB-999");
        RunAnalyticsRecoverySyncCommand right =
                new RunAnalyticsRecoverySyncCommand(152L,
                                                    SyncAnalyticsCategory.ConversationsDetails,
                                                    "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                    null,
                                                    null);

        Assert.NotEqual(left, right);
    }
}
