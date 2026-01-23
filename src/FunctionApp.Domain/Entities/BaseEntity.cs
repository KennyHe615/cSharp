namespace FunctionApp.Domain.Entities;

public abstract class BaseEntity
{
    public DateTimeOffset AppCreatedAt { get; set; }

    public DateTimeOffset AppUpdatedAt { get; set; }
}
