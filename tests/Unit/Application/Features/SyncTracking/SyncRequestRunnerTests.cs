using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Orchestration;
using Application.Abstractions.Persistence;
using Application.DTOs.SyncTracking;
using Application.Enums;
using Application.Features.SyncTracking;

using Xunit;


namespace tests.Unit.Application.Features.SyncTracking;

public sealed class SyncRequestRunnerTests
{
    [Fact]
    public async Task ExecuteAsync_Success_DispatchesAndMarksCompleted()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator
                                         {
                                             NextRunId = 10L,
                                             NextIsCurrent = true
                                         };
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = BuildRequest(100L)
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher();

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        await sut.ExecuteAsync(100L, CancellationToken.None);

        Assert.Equal(1, requestRepository.GetByIdCalls);
        Assert.Equal(1, coordinator.StartCalls);
        Assert.Equal(1, coordinator.IsCurrentCalls);
        Assert.Equal(1, dispatcher.ExecuteCalls);
        Assert.Equal(1, coordinator.MarkCompletedCalls);
        Assert.Equal(0, coordinator.MarkFailedCalls);
        Assert.Equal(0, coordinator.MarkCanceledCalls);

        Assert.Equal(10L, dispatcher.LastRunId);
        Assert.Equal(SyncCategory.UsersDetails, dispatcher.LastCategory);
        Assert.Equal(SyncMode.Incremental, dispatcher.LastMode);
        Assert.Equal("2026-01-01T00:00Z/2026-01-01T00:30Z", dispatcher.LastInterval);
        Assert.Null(dispatcher.LastPageNumber);
        Assert.Null(dispatcher.LastGenesysJobId);
    }

    [Fact]
    public async Task ExecuteAsync_NotCurrentRun_ReturnsWithoutDispatchOrFinalStatus()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator
                                         {
                                             NextRunId = 20L,
                                             NextIsCurrent = false
                                         };
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = BuildRequest(200L)
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher();

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        await sut.ExecuteAsync(200L, CancellationToken.None);

        Assert.Equal(1, coordinator.StartCalls);
        Assert.Equal(1, coordinator.IsCurrentCalls);
        Assert.Equal(0, dispatcher.ExecuteCalls);
        Assert.Equal(0, coordinator.MarkCompletedCalls);
        Assert.Equal(0, coordinator.MarkFailedCalls);
        Assert.Equal(0, coordinator.MarkCanceledCalls);
    }

    [Fact]
    public async Task ExecuteAsync_DispatchThrowsException_MarksFailedAndRethrows()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator
                                         {
                                             NextRunId = 30L,
                                             NextIsCurrent = true
                                         };
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = BuildRequest(300L)
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher
                                             {
                                                 ThrowOnExecute =
                                                     new
                                                         InvalidOperationException("dispatch failed")
                                             };

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(300L, CancellationToken.None));

        Assert.Equal("dispatch failed", ex.Message);
        Assert.Equal(1, dispatcher.ExecuteCalls);
        Assert.Equal(0, coordinator.MarkCompletedCalls);
        Assert.Equal(1, coordinator.MarkFailedCalls);
        Assert.Equal(0, coordinator.MarkCanceledCalls);
        Assert.Equal("dispatch failed", coordinator.LastFailedReason);
    }

    [Fact]
    public async Task ExecuteAsync_CallerCancellation_MarksCanceledAndRethrows()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator
                                         {
                                             NextRunId = 40L,
                                             NextIsCurrent = true
                                         };
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = BuildRequest(400L)
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher
                                             {
                                                 ThrowOnExecute =
                                                     new OperationCanceledException()
                                             };

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        using CancellationTokenSource cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(400L, cts.Token));

        Assert.Equal(1, coordinator.MarkCanceledCalls);
        Assert.Equal("Canceled by host/user request.", coordinator.LastCanceledReason);
        Assert.Equal(0, coordinator.MarkFailedCalls);
        Assert.Equal(0, coordinator.MarkCompletedCalls);
    }

    [Fact]
    public async Task ExecuteAsync_OrchestrationCancellation_MarksCanceledAndRethrows()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator
                                         {
                                             NextRunId = 50L,
                                             NextIsCurrent = true
                                         };
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = BuildRequest(500L)
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher
                                             {
                                                 ThrowOnExecute =
                                                     new OperationCanceledException()
                                             };

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        await Assert.ThrowsAsync<OperationCanceledException>(() => sut.ExecuteAsync(500L, CancellationToken.None));

        Assert.Equal(1, coordinator.MarkCanceledCalls);
        Assert.Equal("Canceled by orchestration signal.", coordinator.LastCanceledReason);
        Assert.Equal(0, coordinator.MarkFailedCalls);
        Assert.Equal(0, coordinator.MarkCompletedCalls);
    }

    [Fact]
    public async Task ExecuteAsync_RequestNotFound_ThrowsInvalidOperationException()
    {
        StubRunCoordinator coordinator = new StubRunCoordinator();
        StubRequestRepository requestRepository = new StubRequestRepository
                                                  {
                                                      NextRequest = null
                                                  };
        StubExecutionDispatcher dispatcher = new StubExecutionDispatcher();

        SyncRequestRunner sut = new SyncRequestRunner(coordinator, requestRepository, dispatcher);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ExecuteAsync(999L, CancellationToken.None));

        Assert.Contains("Sync request '999' was not found.", ex.Message);
        Assert.Equal(0, coordinator.StartCalls);
        Assert.Equal(0, dispatcher.ExecuteCalls);
    }

    #region ========== *** Private Section *** ==========

    [ExcludeFromCodeCoverage]
    private static SyncRequestDto BuildRequest(long id)
    {
        return new SyncRequestDto
               {
                   Id = id,
                   Category = SyncCategory.UsersDetails,
                   Mode = SyncMode.Incremental,
                   Interval = "2026-01-01T00:00Z/2026-01-01T00:30Z",
                   PageNumber = null,
                   GenesysJobId = null,
                   ScopeKey = "UsersDetails|Incremental|2026-01-01T00:00Z/2026-01-01T00:30Z|-|-",
                   CurrentRunId = null
               };
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRunCoordinator : ISyncRunCoordinator
    {
        public long NextRunId { get; init; } = 1L;

        public bool NextIsCurrent { get; init; } = true;

        public int StartCalls { get; private set; }

        public int IsCurrentCalls { get; private set; }

        public int MarkCompletedCalls { get; private set; }

        public int MarkFailedCalls { get; private set; }

        public int MarkCanceledCalls { get; private set; }

        public string? LastFailedReason { get; private set; }

        public string? LastCanceledReason { get; private set; }

        public Task<long> StartNewRunAsync(long requestId, CancellationToken ct)
        {
            StartCalls++;

            return Task.FromResult(NextRunId);
        }

        public Task<bool> IsCurrentRunAsync(long runId, CancellationToken ct)
        {
            IsCurrentCalls++;

            return Task.FromResult(NextIsCurrent);
        }

        public Task MarkCompletedAsync(long runId, CancellationToken ct)
        {
            MarkCompletedCalls++;

            return Task.CompletedTask;
        }

        public Task MarkFailedAsync(long runId, string reason, CancellationToken ct)
        {
            MarkFailedCalls++;
            LastFailedReason = reason;

            return Task.CompletedTask;
        }

        public Task MarkSupersededAsync(long runId, long supersededByRunId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }

        public Task MarkCanceledAsync(long runId, string? reason, CancellationToken ct)
        {
            MarkCanceledCalls++;
            LastCanceledReason = reason;

            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubRequestRepository : ISyncRequestRepository
    {
        public SyncRequestDto? NextRequest { get; init; }

        public int GetByIdCalls { get; private set; }

        public Task<long> CreateOrGetByScopeAsync(SyncCategory category,
                                                  SyncMode mode,
                                                  string? interval,
                                                  int? pageNumber,
                                                  string? genesysJobId,
                                                  CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        public Task<SyncRequestDto?> GetByIdAsync(long id, CancellationToken ct)
        {
            GetByIdCalls++;

            return Task.FromResult(NextRequest);
        }

        public Task SetCurrentRunAsync(long requestId, long runId, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubExecutionDispatcher : ISyncExecutionDispatcher
    {
        public int ExecuteCalls { get; private set; }

        public long LastRunId { get; private set; }

        public SyncCategory LastCategory { get; private set; }

        public SyncMode LastMode { get; private set; }

        public string? LastInterval { get; private set; }

        public int? LastPageNumber { get; private set; }

        public string? LastGenesysJobId { get; private set; }

        public Exception? ThrowOnExecute { get; init; }

        public Task ExecuteAsync(long runId,
                                 SyncCategory category,
                                 SyncMode mode,
                                 string? interval,
                                 int? pageNumber,
                                 string? genesysJobId,
                                 CancellationToken ct)
        {
            ExecuteCalls++;
            LastRunId = runId;
            LastCategory = category;
            LastMode = mode;
            LastInterval = interval;
            LastPageNumber = pageNumber;
            LastGenesysJobId = genesysJobId;

            return ThrowOnExecute is not null ? throw ThrowOnExecute : Task.CompletedTask;
        }
    }

    #endregion
}
