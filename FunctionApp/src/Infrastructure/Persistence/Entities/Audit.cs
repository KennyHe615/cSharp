namespace Infrastructure.Persistence.Entities;

public abstract class Audit
{
    public DateTimeOffset AppCreatedAt { get; set; }

    public DateTimeOffset AppUpdatedAt { get; set; }
}
