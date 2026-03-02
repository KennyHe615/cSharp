namespace SharedKernel.Lobs;

public readonly record struct LobName
{
    private static readonly HashSet<string> Allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                                                      {
                                                          "NTT", "CRC", "LCL"
                                                      };

    public string Value { get; }

    public LobName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("LOB name is required.", nameof(value));
        }

        if (!Allowed.Contains(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value),
                                                  value,
                                                  $"Unsupported LOB '{value}'. Allowed values: {string.Join(", ", Allowed)}.");
        }

        Value = value.ToUpperInvariant();
    }

    /// <summary>
    /// Gets the supported LOB values in normalized uppercase form.
    /// </summary>
    public static IReadOnlyCollection<string> AllowedValues =>
        Allowed.Select(v => v.ToUpperInvariant())
               .OrderBy(v => v, StringComparer.Ordinal)
               .ToArray();

    public static LobName Ntt => new LobName("NTT");

    public static LobName Crc => new LobName("CRC");

    public static LobName Lcl => new LobName("LCL");

    public override string ToString()
    {
        return Value;
    }

    public static implicit operator string(LobName lob)
    {
        return lob.Value;
    }
}
