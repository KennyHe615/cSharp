using System.Text.Json.Serialization;


namespace Application.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupVisibility
{
    Public,
    Owners,
    Members
}
