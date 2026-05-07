using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Centralized detector for unique-key violations used by sync-tracking repositories.
/// For sync_request, this covers full-scope uniqueness, incremental-scope uniqueness, and active recovery scope uniqueness.
/// For sync_run_item, this covers duplicate natural-key conflicts on generic cursor items and page-number work items.
/// </summary>
public static class UniqueViolationDetector
{
    #region ========== *** Fields Section *** ==========

    private static readonly string[] ScopeKeyTokens =
    [
        "UX_sync_request_scope_key_full",
        "UX_sync_request_scope_key_incremental",
        "UX_sync_request_scope_key_recovery_active",
        "UX_sync_request_scope_key",
        "UQ_sync_request_scope_key",
        "scope_key"
    ];

    private static readonly string[] RunItemTokens =
    [
        "UX_sync_run_item_run_step_cursor",
        "UX_sync_run_item_run_step_page_number",
        "UQ_sync_run_item_run_step_cursor",
        "UQ_sync_run_item_run_step_page_number",
        "run_step_cursor",
        "run_step_page_number"
    ];

    private static readonly string[] IncrementalSyncWindowCategoryTokens =
    [
        "UX_incremental_sync_window_category",
        "UQ_incremental_sync_window_category",
        "incremental_sync_window",
        "category"
    ];

    private static readonly string[] ActiveRunTokens =
    [
        "UX_sync_run_request_active",
        "UQ_sync_run_request_active",
        "sync_run_request_active",
        "request_active"
    ];

    #endregion

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for sync_request scope-key constraints.
    /// </summary>
    public static bool IsScopeKeyUniqueViolation(Exception ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, ScopeKeyTokens);
    }

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for sync_run_item natural keys.
    /// This covers generic cursor uniqueness and page-number uniqueness.
    /// </summary>
    public static bool IsRunItemUniqueViolation(Exception ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, RunItemTokens);
    }

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for
    /// <c>incremental_sync_window.category</c>.
    /// </summary>
    public static bool IsIncrementalSyncWindowCategoryUniqueViolation(DbUpdateException ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, IncrementalSyncWindowCategoryTokens);
    }

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for the active sync_run request constraint.
    /// </summary>
    public static bool IsActiveRunUniqueViolation(Exception ex)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, ActiveRunTokens);
    }

    #region ========== *** Private Section *** ==========

    private static bool IsSqlUniqueViolation(Exception ex)
    {
        return ex switch
               {
                   DbConstraintViolationException { InnerException: DbUpdateException dbUpdateException } =>
                           IsSqlUniqueViolation(dbUpdateException),

                   DbUpdateException { InnerException: SqlException sqlEx } => sqlEx.Number is 2601 or 2627,

                   _ => false
               };
    }

    private static bool ContainsAnyToken(Exception ex, IReadOnlyCollection<string> tokens)
    {
        string message = ex.InnerException?.Message ?? ex.Message;

        if (ex is DbConstraintViolationException constraintException
            && !string.IsNullOrWhiteSpace(constraintException.ConstraintName))
        {
            message = $"{constraintException.ConstraintName} {message}";
        }

        return !string.IsNullOrWhiteSpace(message)
               && tokens.Any(token => message.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    #endregion
}
