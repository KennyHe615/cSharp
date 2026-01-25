namespace FunctionApp.Domain.Entities;

public abstract class AuditEntity
{
    public DateTimeOffset AppCreatedAt { get; set; }

    public DateTimeOffset AppUpdatedAt { get; set; }
}
