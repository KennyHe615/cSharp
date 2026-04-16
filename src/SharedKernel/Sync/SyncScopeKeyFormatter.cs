namespace SharedKernel.Sync;

/// <summary>
/// Canonical formatter for sync scope/cursor identity tokens.
/// </summary>
public static class SyncScopeKeyFormatter
{
    public static string Format(string category,
                                string modeToken,
                                string? interval,
                                int? pageNumber,
                                string? genesysJobId)
    {
        if (string.IsNullOrWhiteSpace(category))
        {
            throw new ArgumentException("Category is required.", nameof(category));
        }

        if (string.IsNullOrWhiteSpace(modeToken))
        {
            throw new ArgumentException("Mode token is required.", nameof(modeToken));
        }

        string c = category.Trim();
        string m = modeToken.Trim();
        string i = string.IsNullOrWhiteSpace(interval) ? "-" : interval.Trim();
        string p = pageNumber.HasValue ? pageNumber.Value.ToString() : "-";
        string g = string.IsNullOrWhiteSpace(genesysJobId) ? "-" : genesysJobId.Trim();

        return $"{c}|{m}|{i}|{p}|{g}";
    }
}
