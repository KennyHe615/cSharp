namespace Infrastructure.Persistence.Mappers.Shared;

public static class MappingExtensions
{
    public static string? GetValue(this System.Collections.IDictionary? dictionary, string path)
    {
        return dictionary.GetValue(path, null);
    }

    public static string? GetValue(this System.Collections.IDictionary? dictionary, string path, int? truncate)
    {
        if (dictionary is null) return null;

        if (string.IsNullOrWhiteSpace(path)) return null;

        object? current = dictionary;

        foreach (string key in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (current is not System.Collections.IDictionary dict) return null;

            if (!dict.Contains(key)) return null;

            current = dict[key];

            if (current is null) return null;
        }

        string? result = current.ToString();

        if (truncate is null or < 0) return result;

        if (result is null) return null;

        return result.Length > truncate.Value ? result[..truncate.Value] : result;
    }
}
