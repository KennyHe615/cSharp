namespace FunctionApp.Domain.ValueObjects;

public readonly record struct ConversationDuration(TimeSpan Value)
{
    public static ConversationDuration FromSeconds(int seconds)
    {
        return seconds < 0
                   ? throw new InvalidDataException("Conversation duration cannot be negative.")
                   : new ConversationDuration(TimeSpan.FromSeconds(seconds));
    }
}
