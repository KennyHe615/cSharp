using Application.Enums;
using Application.Features.SyncTracking.Analytics;

using Xunit;

namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunAnalyticsIncrementalSyncCommandTests
{
    [Fact]
    public void Constructor_ShouldAssignAllProperties()
    {
        RunAnalyticsIncrementalSyncCommand command =
            new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.UsersDetails,
                                                   "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                   3);

        Assert.Equal(SyncAnalyticsCategory.UsersDetails, command.Category);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", command.Interval);
        Assert.Equal(3, command.PageNumber);
    }

    [Fact]
    public void ValueEquality_SameValues_ShouldBeEqual()
    {
        RunAnalyticsIncrementalSyncCommand left = new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                                                         "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                                         null);
        RunAnalyticsIncrementalSyncCommand right = new RunAnalyticsIncrementalSyncCommand(SyncAnalyticsCategory.ConversationsDetails,
                                                                                          "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                                          null);

        Assert.Equal(left, right);
    }
}
