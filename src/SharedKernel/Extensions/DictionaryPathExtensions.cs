using System.Collections;


namespace SharedKernel.Extensions;

public static class DictionaryPathExtensions
{
    public static string? GetStringByPath(this IDictionary? dictionary, string path, int? truncate = null)
    {
        if (!dictionary.TryGetByPath(path, out object? value, false) || value is null)
        {
            return null;
        }

        string? result = value.ToString();

        if (truncate is null || truncate < 0 || result is null) return result;

        return result.Length > truncate.Value ? result[..truncate.Value] : result;
    }

    public static bool TryGetByPath(this IDictionary? dictionary,
                                    string path,
                                    out object? value,
                                    bool includeNullTerminalValue = true)
    {
        value = null;

        if (dictionary is null || string.IsNullOrWhiteSpace(path)) return false;

        object? current = dictionary;
        string[] keys = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (keys.Length == 0)
        {
            value = null;

            return false;
        }

        for (int i = 0; i < keys.Length; i++)
        {
            string key = keys[i];
            bool isLast = i == keys.Length - 1;

            if (current is not IDictionary currentDict || !TryGetDictionaryValue(currentDict, key, out current))
            {
                value = null;

                return false;
            }

            if (current is not null) continue;

            if (isLast && includeNullTerminalValue)
            {
                value = null;

                return true;
            }

            value = null;

            return false;
        }

        value = current;

        return true;
    }

    #region ========== *** Private Methods *** ==========

    private static bool TryGetDictionaryValue(IDictionary dictionary, string key, out object? value)
    {
        if (dictionary.Contains(key))
        {
            value = dictionary[key];

            return true;
        }

        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string entryKey || !string.Equals(entryKey, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            value = entry.Value;

            return true;
        }

        value = null;

        return false;
    }

    #endregion
}
