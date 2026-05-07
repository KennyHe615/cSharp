using Infrastructure.Persistence.Repositories.Shared;


namespace Infrastructure.Persistence.Repositories.Recovery;

/// <summary>
/// Detects unique-key violations for analytics recovery intake requests.
/// </summary>
internal static class AnalyticsRecoveryRequestUniqueViolationDetector
{
    private static readonly string[] ActiveScopeTokens =
    [
        "UX_analytics_recovery_request_scope_key_active",
        "UQ_analytics_recovery_request_scope_key_active",
        "analytics_recovery_request_scope_key_active",
        "analytics_recovery_request",
        "scope_key"
    ];

    /// <summary>
    /// Determines whether the exception represents a duplicate-key violation for active analytics recovery intake scope.
    /// </summary>
    public static bool IsActiveScopeUniqueViolation(Exception ex)
    {
        return UniqueViolationDetectorCore.IsUniqueViolation(ex, ActiveScopeTokens);
    }
}
