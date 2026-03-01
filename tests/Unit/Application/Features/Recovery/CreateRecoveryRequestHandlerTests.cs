using System.Diagnostics.CodeAnalysis;

using Application.Abstractions.Persistence;
using Application.Contracts.InternalApis.Recovery;
using Application.DTOs.JobTracking;
using Application.Enums;
using Application.Features.Recovery;

using SharedKernel.Lobs;
using SharedKernel.Time;

using Xunit;


namespace tests.Unit.Application.Features.Recovery;

public sealed class CreateRecoveryRequestHandlerTests
{
    [Theory]
    [InlineData(RecoveryCategory.UsersDetails, SyncDataType.UsersDetailsRecovery)]
    [InlineData(RecoveryCategory.ConversationsDetails, SyncDataType.ConversationsDetailsRecovery)]
    [InlineData(RecoveryCategory.ConversationsAggregates, SyncDataType.ConversationsAggregatesRecovery)]
    public async Task Handle_MapsCategoryAndCreatesJob(RecoveryCategory category, SyncDataType expectedDataType)
    {
        StubJobTrackingRepository repository = new StubJobTrackingRepository { NextCreatedId = 101L };
        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository);

        UtcInterval interval = new UtcInterval(new DateTimeOffset(2025,
                                                                  1,
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero),
                                               new DateTimeOffset(2025,
                                                                  1,
                                                                  1,
                                                                  1,
                                                                  0,
                                                                  0,
                                                                  TimeSpan.Zero));

        CreateRecoveryRequestCommand command = new CreateRecoveryRequestCommand(new LobName("CRC"),
                                                                                category,
                                                                                interval,
                                                                                "JOB-123");

        CreateRecoveryRequestResponse response = await sut.Handle(command, CancellationToken.None);

        Assert.Equal(1, repository.CreateCalls);
        Assert.Equal(expectedDataType, repository.LastCategory);
        Assert.Equal(interval, repository.LastInterval);
        Assert.Equal("JOB-123", repository.LastJobId);

        Assert.True(response.Success);
        Assert.Equal("Recovery request created successfully.", response.Message);

        object detail = response.RequestedDetail;
        Assert.NotNull(detail);

        Type detailType = detail.GetType();
        Assert.Equal(101L, (long)detailType.GetProperty("Id")!.GetValue(detail)!);
        Assert.Equal("CRC", (string)detailType.GetProperty("Lob")!.GetValue(detail)!);
        Assert.Equal(category.ToString(), (string)detailType.GetProperty("Category")!.GetValue(detail)!);
        Assert.Equal(interval, (UtcInterval?)detailType.GetProperty("Interval")!.GetValue(detail)!);
        Assert.Equal("JOB-123", (string?)detailType.GetProperty("JobId")!.GetValue(detail)!);
    }

    [Fact]
    public async Task Handle_UnsupportedCategory_ThrowsInvalidOperationException()
    {
        StubJobTrackingRepository repository = new StubJobTrackingRepository();
        CreateRecoveryRequestHandler sut = new CreateRecoveryRequestHandler(repository);

        CreateRecoveryRequestCommand command =
            new CreateRecoveryRequestCommand(new LobName("CRC"),
                                             (RecoveryCategory)999,
                                             null,
                                             null);

        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.Handle(command, CancellationToken.None));

        Assert.Contains("Unsupported recovery category", ex.Message);
        Assert.Equal(0, repository.CreateCalls);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubJobTrackingRepository : IJobTrackingRepository
    {
        public int CreateCalls { get; private set; }

        public long NextCreatedId { get; set; } = 1L;

        public SyncDataType? LastCategory { get; private set; }

        public UtcInterval? LastInterval { get; private set; }

        public string? LastJobId { get; private set; }

        public Task<long> CreateAsync(SyncDataType category, UtcInterval? interval, string? jobId, CancellationToken ct)
        {
            CreateCalls++;
            LastCategory = category;
            LastInterval = interval;
            LastJobId = jobId;

            return Task.FromResult(NextCreatedId);
        }

        public Task<JobTrackingDto?> GetByIdAsync(long id, CancellationToken ct)
        {
            return Task.FromResult<JobTrackingDto?>(null);
        }

        public Task UpdateRecoveryCompletedAsync(long id, bool isCompleted, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
