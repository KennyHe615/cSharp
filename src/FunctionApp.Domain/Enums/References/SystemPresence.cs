using System.Text.Json.Serialization;


namespace FunctionApp.Domain.Enums.References;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SystemPresence
{
    Available,
    Away,
    Busy,
    Offline,
    Idle,
    OnQueue,
    Meal,
    Training,
    Meeting,
    Break
}
