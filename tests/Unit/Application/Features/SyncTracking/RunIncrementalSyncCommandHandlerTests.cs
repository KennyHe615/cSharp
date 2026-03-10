using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class RunIncrementalSyncCommandHandlerTests
{
    [Fact]
    public async Task Handle_Success_ReturnsIncrementalRequestId_AndDoesNotCreateRecovery()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 101L
                                                      };
        StubSyncRequestRunner runner = new StubSyncRequestRunner();

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.UsersDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", 1);

        long result = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(101L, result);
        Assert.Single(requestRepository.Calls);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
        Assert.Equal(1, runner.ExecuteCalls);
        Assert.Equal(101L, runner.LastRequestId);
    }

    [Fact]
    public async Task Handle_CallerCanceled_RethrowsOperationCanceledException_WithoutRecovery()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 201L
                                                      };
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute =
                                               new
                                                   OperationCanceledException("caller canceled")
                                       };

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.UsersDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", null);

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Handle(command, cts.Token));

        Assert.Single(requestRepository.Calls);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
    }

    [Fact]
    public async Task Handle_OrchestrationCanceled_RethrowsOperationCanceledException_WithoutRecovery()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 301L
                                                      };
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute =
                                               new
                                                   OperationCanceledException("orchestration canceled")
                                       };

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.UsersDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", null);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Single(requestRepository.Calls);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
    }

    [Fact]
    public async Task Handle_AnalyticsFailure_CreatesRecoveryScope_AndRethrowsOriginalException()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 401L,
                                                          RecoveryRequestId = 402L
                                                      };
        InvalidOperationException original = new InvalidOperationException("incremental failed");
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute = original
                                       };

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.ConversationsDetails,
                                                                          "2026-01-01T00:00Z/2026-01-01T00:30Z",
                                                                          2);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);
        Assert.Equal(2, requestRepository.Calls.Count);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
        Assert.Equal(SyncMode.Recovery, requestRepository.Calls[1].Mode);
        Assert.Equal(command.Category, requestRepository.Calls[1].Category);
        Assert.Equal(command.Interval, requestRepository.Calls[1].Interval);
        Assert.Equal(command.PageNumber, requestRepository.Calls[1].PageNumber);
    }

    [Fact]
    public async Task Handle_AnalyticsFailure_RecoveryCreateFails_StillRethrowsOriginalException()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 501L,
                                                          ThrowOnRecoveryCreate =
                                                              new Exception("recovery create failed")
                                                      };
        InvalidOperationException original = new InvalidOperationException("incremental failed");
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute = original
                                       };

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command =
            new RunIncrementalSyncCommand(SyncCategory.UsersDetails, "2026-01-01T00:00Z/2026-01-01T00:30Z", null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);
        Assert.Equal(2, requestRepository.Calls.Count);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
        Assert.Equal(SyncMode.Recovery, requestRepository.Calls[1].Mode);
    }

    [Fact]
    public async Task Handle_ReferencesFailure_DoesNotCreateRecoveryScope_AndRethrows()
    {
        StubSyncRequestRepository requestRepository = new StubSyncRequestRepository
                                                      {
                                                          IncrementalRequestId = 601L
                                                      };
        InvalidOperationException original = new InvalidOperationException("references failed");
        StubSyncRequestRunner runner = new StubSyncRequestRunner
                                       {
                                           ThrowOnExecute = original
                                       };

        RunIncrementalSyncCommandHandler sut = new RunIncrementalSyncCommandHandler(requestRepository, runner);
        RunIncrementalSyncCommand command = new RunIncrementalSyncCommand(SyncCategory.Queue, null, null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Same(original, ex);
        Assert.Single(requestRepository.Calls);
        Assert.Equal(SyncMode.Incremental, requestRepository.Calls[0].Mode);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRequestRunner : ISyncRequestRunner
    {
        public int ExecuteCalls { get; private set; }

        public long LastRequestId { get; private set; }

        public Exception? ThrowOnExecute { get; init; }

        public Task ExecuteAsync(long requestId, CancellationToken ct)
        {
            ExecuteCalls++;
            LastRequestId = requestId;

            if (ThrowOnExecute is not null) throw ThrowOnExecute;

            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubSyncRequestRepository : ISyncRequestRepository
    {
        public long IncrementalRequestId { get; init; } = 1L;

        public long RecoveryRequestId { get; init; } = 2L;

        public Exception? ThrowOnRecoveryCreate { get; init; }

        public List<CreateCall> Calls { get; } = [];

        public Task<long> CreateOrGetByScopeAsync(SyncCategory category,
                                                  SyncMode mode,
                                                  string? interval,
                                                  int? pageNumber,
                                                  string? genesysJobId,
                                                  CancellationToken ct)
        {
            Calls.Add(new CreateCall(category,
                                     mode,
                                     interval,
                                     pageNumber,
                                     genesysJobId));

            if (mode == SyncMode.Recovery && ThrowOnRecoveryCreate is not null)
            {
                throw ThrowOnRecoveryCreate;
            }

            return Task.FromResult(mode == SyncMode.Incremental ? IncrementalRequestId : RecoveryRequestId);
        }

        public Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct)
        {
            throw new NotSupportedException();
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed record CreateCall(SyncCategory Category,
                                     SyncMode Mode,
                                     string? Interval,
                                     int? PageNumber,
                                     string? GenesysJobId);

    #endregion
}
