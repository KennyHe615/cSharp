namespace Infrastructure.Persistence.Entities;

/// <summary>
/// Base persistence audit fields applied to entities that require application-managed timestamps.
/// </summary>
public abstract class Audit
{
    /// <summary>
    /// Gets or sets the application-created timestamp converted to Eastern time.
    /// </summary>
    public DateTimeOffset AppCreatedAtEastern { get; set; }

    /// <summary>
    /// Gets or sets the application-updated timestamp converted to Eastern time.
    /// </summary>
    public DateTimeOffset AppUpdatedAtEastern { get; set; }
}
