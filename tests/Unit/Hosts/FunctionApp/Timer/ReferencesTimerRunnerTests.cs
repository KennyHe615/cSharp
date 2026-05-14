using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Context;
using Application.Abstractions.Identity;
using Application.Enums;
using Application.Features.References;
using Application.Mediator;

using FunctionApps.Timer;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using Moq;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Hosts.FunctionApp.Timer;

public sealed class ReferencesTimerRunnerTests
{
    [Fact]
    public async Task RunAsync_DispatchesAllEnumCategories_AndPopulatesLobContext()
    {
        TestContext ctx = CreateContext();
        List<RunReferencesFullSyncCommand> dispatched = [];

        ctx.Mediator.Setup(x => x.Send(It.IsAny<RunReferencesFullSyncCommand>(), It.IsAny<CancellationToken>()))
           .Callback<IRequest<long>, CancellationToken>((request, _) =>
                                                        {
                                                            dispatched.Add((RunReferencesFullSyncCommand)request);
                                                        })
           .ReturnsAsync(1L);

        TimerInfo timer = CreateTimerInfo(isPastDue: false);

        await ctx.Sut.RunAsync(LobName.Crc, timer, CancellationToken.None);

        SyncReferenceCategory[] expected = Enum.GetValues<SyncReferenceCategory>();

        Assert.Equal(expected.Length, dispatched.Count);
        Assert.Equal(expected,
                     dispatched.Select(x => x.Category)
                               .ToArray());
        Assert.Equal(LobName.Crc.Value, ctx.Accessor.Object.LobName);

        ctx.CredentialProvider.Verify(x => x.PopulateAsync(ctx.Accessor.Object, It.IsAny<CancellationToken>()),
                                      Times.Once);
        ctx.Mediator.Verify(x => x.Send(It.IsAny<RunReferencesFullSyncCommand>(), It.IsAny<CancellationToken>()),
                            Times.Exactly(expected.Length));
    }

    [Fact]
    public async Task RunAsync_WhenOneCategoryThrowsNotSupported_ContinuesWithRemainingCategories()
    {
        TestContext ctx = CreateContext();

        SyncReferenceCategory[] all = Enum.GetValues<SyncReferenceCategory>();
        SyncReferenceCategory unsupported = all[0];

        int notSupportedCount = 0;
        int totalDispatchCalls = 0;

        ctx.Mediator.Setup(x => x.Send(It.IsAny<RunReferencesFullSyncCommand>(), It.IsAny<CancellationToken>()))
           .Returns<IRequest<long>, CancellationToken>((request, _) =>
                                                       {
                                                           totalDispatchCalls++;
                                                           RunReferencesFullSyncCommand cmd =
                                                                   (RunReferencesFullSyncCommand)request;

                                                           if (cmd.Category != unsupported) return Task.FromResult(1L);

                                                           notSupportedCount++;

                                                           throw new NotSupportedException("Category not wired.");
                                                       });

        TimerInfo timer = CreateTimerInfo(isPastDue: true);

        await ctx.Sut.RunAsync(LobName.Ntt, timer, CancellationToken.None);

        Assert.Equal(all.Length, totalDispatchCalls);
        Assert.Equal(1, notSupportedCount);

        ctx.CredentialProvider.Verify(x => x.PopulateAsync(ctx.Accessor.Object, It.IsAny<CancellationToken>()),
                                      Times.Once);
        ctx.Mediator.Verify(x => x.Send(It.IsAny<RunReferencesFullSyncCommand>(), It.IsAny<CancellationToken>()),
                            Times.Exactly(all.Length));
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static TestContext CreateContext()
    {
        Mock<ISimpleMediator> mediator = new Mock<ISimpleMediator>(MockBehavior.Strict);
        mediator.Setup(x => x.Send(It.IsAny<RunReferencesFullSyncCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(1L);

        Mock<ILobContextAccessor> accessor = new Mock<ILobContextAccessor>(MockBehavior.Strict);
        accessor.SetupAllProperties();

        Mock<ICredentialProvider> credentialProvider = new Mock<ICredentialProvider>(MockBehavior.Strict);
        credentialProvider.Setup(x => x.PopulateAsync(It.IsAny<ILobContextAccessor>(), It.IsAny<CancellationToken>()))
                          .Returns(Task.CompletedTask);

        Mock<IDateTimeProvider> dateTimeProvider = new Mock<IDateTimeProvider>(MockBehavior.Strict);
        dateTimeProvider.SetupGet(x => x.EstNow)
                        .Returns(new DateTime(2026,
                                              4,
                                              13,
                                              12,
                                              0,
                                              0));

        Mock<ILogger<ReferencesTimerRunner>> logger = new Mock<ILogger<ReferencesTimerRunner>>(MockBehavior.Loose);
        logger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>()))
              .Returns(true);

        ReferencesTimerRunner sut = new ReferencesTimerRunner(mediator.Object,
                                                              accessor.Object,
                                                              credentialProvider.Object,
                                                              dateTimeProvider.Object,
                                                              logger.Object);

        return new TestContext(sut,
                               mediator,
                               accessor,
                               credentialProvider);
    }

    [ExcludeFromCodeCoverage]
    private static TimerInfo CreateTimerInfo(bool isPastDue)
    {
        return new TimerInfo
               {
                   IsPastDue = isPastDue,
                   ScheduleStatus = new ScheduleStatus()
               };
    }

    [ExcludeFromCodeCoverage]
    private sealed record TestContext(ReferencesTimerRunner Sut,
                                      Mock<ISimpleMediator> Mediator,
                                      Mock<ILobContextAccessor> Accessor,
                                      Mock<ICredentialProvider> CredentialProvider);

    #endregion
}
