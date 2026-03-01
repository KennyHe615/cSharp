using Application.Abstractions.Persistence;
using Application.DTOs.JobTracking;
using Application.Enums;

using Infrastructure.Persistence.DbContext;
using Infrastructure.Persistence.Entities.JobTracking;

using Microsoft.EntityFrameworkCore;

using SharedKernel.Time;


namespace Infrastructure.Persistence.Repositories.JobTracking;

/// <summary>
/// Entity Framework-based implementation of <see cref="IJobTrackingRepository"/>.
/// </summary>
public sealed class JobTrackingRepository(IUnitOfWork uow,
                                          AppDbContext dbContext) : IJobTrackingRepository
{
    private readonly IUnitOfWork _uow = uow              ?? throw new ArgumentNullException(nameof(uow));
    private readonly AppDbContext _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));

    /// <inheritdoc />
    public async Task<long> CreateAsync(SyncDataType category,
                                        UtcInterval? interval,
                                        string? jobId,
                                        CancellationToken ct)
    {
        JobTrackingEntity entity = new JobTrackingEntity
                                   {
                                       DataType = category,
                                       Interval = interval?.ToString(),
                                       JobId = jobId,
                                       IsIncrementalCompleted = false,
                                       IsRecoveryCompleted = false
                                   };

        await _uow.UpsertAsync(entity, ct: ct)
                  .ConfigureAwait(false);
        await _uow.SaveChangesAsync(ct)
                  .ConfigureAwait(false);

        return entity.Id;
    }

    /// <inheritdoc />
    public async Task<JobTrackingDto?> GetByIdAsync(long id, CancellationToken ct)
    {
        JobTrackingEntity? entity = await _dbContext.Set<JobTrackingEntity>()
                                                    .AsNoTracking()
                                                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                                                    .ConfigureAwait(false);

        return entity is not null
            ? new JobTrackingDto
              {
                  Id = entity.Id,
                  Category = entity.DataType,
                  Interval = entity.Interval,
                  JobId = entity.JobId,
                  IsIncrementalCompleted = entity.IsIncrementalCompleted,
                  IsRecoveryCompleted = entity.IsRecoveryCompleted
              }
            : null;
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    /// Thrown when the target job-tracking record does not exist.
    /// </exception>
    public async Task UpdateRecoveryCompletedAsync(long id, bool isCompleted, CancellationToken ct)
    {
        JobTrackingEntity? entity = await _dbContext.Set<JobTrackingEntity>()
                                                    .FirstOrDefaultAsync(x => x.Id == id, ct)
                                                    .ConfigureAwait(false);

        if (entity is null)
        {
            throw new InvalidOperationException($"JobTracking record '{id}' was not found.");
        }

        entity.IsRecoveryCompleted = isCompleted;

        await _uow.SaveChangesAsync(ct)
                  .ConfigureAwait(false);
    }
}
