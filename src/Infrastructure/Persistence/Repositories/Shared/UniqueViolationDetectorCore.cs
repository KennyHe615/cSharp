using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


namespace Infrastructure.Persistence.Repositories.Shared;

/// <summary>
/// Shared unique-violation detection primitives for repository-specific conflict detectors.
/// </summary>
internal static class UniqueViolationDetectorCore
{
    /// <summary>
    /// Determines whether the exception is a SQL unique-key violation or contains one of the supplied constraint tokens.
    /// </summary>
    /// <param name="ex">Persistence exception raised while saving or updating data.</param>
    /// <param name="tokens">Constraint or index name fragments that identify a repository-specific unique conflict.</param>
    /// <returns><c>true</c> when the exception represents the expected unique conflict; otherwise <c>false</c>.</returns>
    internal static bool IsUniqueViolation(Exception ex, IReadOnlyCollection<string> tokens)
    {
        return IsSqlUniqueViolation(ex) || ContainsAnyToken(ex, tokens);
    }

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
}
