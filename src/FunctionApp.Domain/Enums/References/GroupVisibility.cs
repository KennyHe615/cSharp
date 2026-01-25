using System.Text.Json.Serialization;


namespace FunctionApp.Domain.Enums.References;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupVisibility
{
    Public,
    Owners,
    Members
}
