namespace Infrastructure.Persistence.Repositories.SyncTracking;

/// <summary>
/// Extension helpers for validating and normalizing sync run-item repository inputs.
/// These helpers keep repository methods focused on persistence behavior.
/// </summary>
internal static class SyncRunItemInputExtensions
{
    /// <summary>
    /// Normalizes and validates a run-item step name.
    /// </summary>
    /// <param name="step">Raw step name.</param>
    /// <returns>The trimmed step name.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="step"/> is empty or whitespace.
    /// </exception>
    public static string NormalizeStep(this string step)
    {
        return string.IsNullOrWhiteSpace(step)
                       ? throw new ArgumentException("Step is required.", nameof(step))
                       : step.Trim();
    }

    /// <summary>
    /// Normalizes and validates a generic cursor token.
    /// </summary>
    /// <param name="cursor">Raw cursor token.</param>
    /// <returns>The trimmed cursor token.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="cursor"/> is empty or whitespace.
    /// </exception>
    public static string NormalizeCursor(this string cursor)
    {
        return string.IsNullOrWhiteSpace(cursor)
                       ? throw new ArgumentException("Cursor is required.", nameof(cursor))
                       : cursor.Trim();
    }

    /// <summary>
    /// Normalizes and validates a worker identifier.
    /// Values longer than the persistence limit are truncated to 200 characters.
    /// </summary>
    /// <param name="claimedBy">Raw worker identifier.</param>
    /// <returns>The normalized worker identifier.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimedBy"/> is empty or whitespace.
    /// </exception>
    public static string NormalizeClaimedBy(this string claimedBy)
    {
        if (string.IsNullOrWhiteSpace(claimedBy))
        {
            throw new ArgumentException("ClaimedBy is required.", nameof(claimedBy));
        }

        string trimmed = claimedBy.Trim();

        return trimmed.Length <= 200 ? trimmed : trimmed[..200];
    }

    /// <summary>
    /// Validates a lease ownership token.
    /// </summary>
    /// <param name="leaseToken">Lease ownership token.</param>
    /// <returns>The validated lease token.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="leaseToken"/> is empty.
    /// </exception>
    public static Guid NormalizeLeaseToken(this Guid leaseToken)
    {
        return leaseToken == Guid.Empty
                       ? throw new ArgumentException("LeaseToken is required.", nameof(leaseToken))
                       : leaseToken;
    }

    /// <summary>
    /// Normalizes an optional failure reason for persistence.
    /// Values longer than the persistence limit are truncated to 1000 characters.
    /// </summary>
    /// <param name="failureReason">Raw failure reason.</param>
    /// <returns>The normalized failure reason, or null when no reason is supplied.</returns>
    public static string? NormalizeFailureReason(this string? failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason)) return null;

        string trimmed = failureReason.Trim();

        return trimmed.Length <= 1000 ? trimmed : trimmed[..1000];
    }

    /// <summary>
    /// Normalizes a required failure reason for terminal failed transitions.
    /// </summary>
    /// <param name="failureReason">Raw failure reason.</param>
    /// <returns>The normalized failure reason.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="failureReason"/> is empty or whitespace.
    /// </exception>
    public static string NormalizeRequiredFailureReason(this string failureReason)
    {
        return failureReason.NormalizeFailureReason()
               ?? throw new ArgumentException("Failure reason is required.", nameof(failureReason));
    }

    /// <summary>
    /// Normalizes and de-duplicates page numbers while preserving first-seen order.
    /// </summary>
    /// <param name="pageNumbers">Incoming page numbers.</param>
    /// <returns>The normalized distinct page-number list.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="pageNumbers"/> is null.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when any page number is less than 1.
    /// </exception>
    public static IReadOnlyList<int> NormalizeDistinctPageNumbers(this IReadOnlyCollection<int> pageNumbers)
    {
        ArgumentNullException.ThrowIfNull(pageNumbers);

        List<int> normalized = new List<int>(pageNumbers.Count);
        HashSet<int> seen = [];

        foreach (int pageNumber in pageNumbers)
        {
            if (pageNumber < 1)
            {
                throw new ArgumentException("PageNumber must be greater than or equal to 1.", nameof(pageNumbers));
            }

            if (seen.Add(pageNumber)) normalized.Add(pageNumber);
        }

        return normalized;
    }

    /// <summary>
    /// Validates that a lease expiry is later than the lease start timestamp.
    /// </summary>
    /// <param name="claimedAtEastern">Eastern application timestamp when the lease is acquired.</param>
    /// <param name="claimExpiresAtEastern">Eastern application timestamp when the lease expires.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimExpiresAtEastern"/> is not greater than <paramref name="claimedAtEastern"/>.
    /// </exception>
    public static void ValidateClaimWindow(this DateTimeOffset claimedAtEastern, DateTimeOffset claimExpiresAtEastern)
    {
        if (claimExpiresAtEastern <= claimedAtEastern)
        {
            throw new ArgumentException("Claim expiry must be greater than claim start.",
                                        nameof(claimExpiresAtEastern));
        }
    }

    /// <summary>
    /// Validates that a heartbeat lease expiry is later than the heartbeat timestamp.
    /// </summary>
    /// <param name="heartbeatAtEastern">Eastern application timestamp of the heartbeat.</param>
    /// <param name="claimExpiresAtEastern">Eastern application timestamp when the lease expires.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="claimExpiresAtEastern"/> is not greater than <paramref name="heartbeatAtEastern"/>.
    /// </exception>
    public static void ValidateHeartbeatWindow(this DateTimeOffset heartbeatAtEastern,
                                               DateTimeOffset claimExpiresAtEastern)
    {
        if (claimExpiresAtEastern <= heartbeatAtEastern)
        {
            throw new ArgumentException("Claim expiry must be greater than heartbeat time.",
                                        nameof(claimExpiresAtEastern));
        }
    }
}
