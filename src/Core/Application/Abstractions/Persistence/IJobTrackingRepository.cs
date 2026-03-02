using Application.DTOs.JobTracking;
using Application.Enums;

using SharedKernel.Time;


namespace Application.Abstractions.Persistence;

/// <summary>
/// Provides persistence operations for job-tracking records used by sync and recovery workflows.
/// </summary>
public interface IJobTrackingRepository
{
    /// <summary>
    /// Creates a new job-tracking record.
    /// </summary>
    /// <param name="category">Sync/recovery data type for the tracking record.</param>
    /// <param name="interval">Optional UTC interval associated with the job.</param>
    /// <param name="jobId">Optional external job identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The created job-tracking record identifier.</returns>
    Task<long> CreateAsync(SyncDataType category, UtcInterval? interval, string? jobId, CancellationToken ct);

    /// <summary>
    /// Retrieves a job-tracking record by identifier.
    /// </summary>
    /// <param name="id">Job-tracking record identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// The matching <see cref="JobTrackingDto"/> when found; otherwise <c>null</c>.
    /// </returns>
    Task<JobTrackingDto?> GetByIdAsync(long id, CancellationToken ct);

    /// <summary>
    /// Updates the recovery-completed flag for an existing job-tracking record.
    /// </summary>
    /// <param name="id">Job-tracking record identifier.</param>
    /// <param name="isCompleted">Recovery completion state to persist.</param>
    /// <param name="ct">Cancellation token.</param>
    Task UpdateRecoveryCompletedAsync(long id, bool isCompleted, CancellationToken ct);
}
