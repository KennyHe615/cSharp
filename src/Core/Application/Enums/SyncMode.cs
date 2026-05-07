namespace Application.Enums;

/// <summary>
/// Execution mode for a logical sync request.
/// References uses <see cref="Full"/>.
/// Analytics uses <see cref="Incremental"/> and <see cref="Recovery"/>.
/// </summary>
public enum SyncMode
{
    Full = 1,
    Incremental = 2,
    Recovery = 3
}
