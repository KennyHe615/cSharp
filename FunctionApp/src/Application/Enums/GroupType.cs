using System.Text.Json.Serialization;


namespace Application.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum GroupType
{
    Official,
    Social
}
