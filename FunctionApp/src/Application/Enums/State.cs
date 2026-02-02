using System.Text.Json.Serialization;


namespace Application.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum State
{
    Active,
    Inactive,
    Deleted
}
