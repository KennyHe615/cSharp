using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Centralized detector for unique-key violations used by sync-tracking repositories.
/// For sync_request, this covers both incremental scope uniqueness and active recovery scope uniqueness.
/// </summary>
public static class UniqueViolationDetector
{
    private static readonly string[] ScopeKeyTokens =
    [
        "UX_sync_request_scope_key_incremental",
        "UX_sync_request_scope_key_recovery_active",
        "UX_sync_request_scope_key",
        "UQ_sync_request_scope_key",
        "scope_key"
    ];

    private static readonly string[] CheckpointTokens =
    [
        "UX_sync_checkpoint_run_step_cursor",
        "UQ_sync_checkpoint_run_step_cursor",
        "run_step_cursor"
    ];

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for sync_request scope-key constraints.
    /// </summary>
    public static bool IsScopeKeyUniqueViolation(DbUpdateException ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, ScopeKeyTokens);
    }

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for sync_checkpoint (run, step, cursor).
    /// </summary>
    public static bool IsCheckpointUniqueViolation(DbUpdateException ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, CheckpointTokens);
    }

    #region ========== *** Private Section *** ==========

    private static bool IsSqlUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // 2601: Cannot insert duplicate key row with unique index
            // 2627: Violation of UNIQUE KEY constraint / PRIMARY KEY
            return sqlEx.Number is 2601 or 2627;
        }

        return false;
    }

    private static bool ContainsAnyToken(DbUpdateException ex, IReadOnlyCollection<string> tokens)
    {
        string message = ex.InnerException?.Message ?? ex.Message;

        return !string.IsNullOrWhiteSpace(message)
               && tokens.Any(token => message.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
