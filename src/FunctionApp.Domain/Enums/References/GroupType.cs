using System.Text.Json.Serialization;


namespace FunctionApp.Domain.Enums.References;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupType
{
    Official,
    Social
}
