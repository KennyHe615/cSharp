using Application.Shared.Context;

using Configuration.Options;

using Microsoft.Extensions.Options;


namespace Infrastructure.Shared.Context;

public sealed class LobContext : ILobContext
{
    private readonly LobContextOptions _options;
    private readonly LobSettings _settings;

    public LobContext(IOptions<LobContextOptions> options, ILobContextAccessor accessor)
    {
        string? lobName = accessor.LobName;

        if (string.IsNullOrWhiteSpace(lobName))
        {
            throw new InvalidOperationException("LOB Context has not been initialized with a LOB name.");
        }

        LobName = lobName;
        _options = options.Value;

        if (!TryGet(lobName, out _settings))
        {
            throw new KeyNotFoundException($"No LobSettings configured for LOB: {lobName}");
        }
    }

    public string LobName { get; }

    public string GenesysClientId => _settings.GenesysClientId;

    public string GenesysClientSecret => _settings.GenesysClientSecret;

    public string DatabaseConnectionString => _settings.DatabaseConnectionString;

    #region ========== *** Private Methods *** ==========

    private bool TryGet(string lobName, out LobSettings settings)
    {
        settings = null!;

        // If LobContextOptions is a dictionary-like type
        if (_options.TryGetValue(lobName, out LobSettings? direct))
        {
            settings = direct;

            return true;
        }

        // Case-insensitive fallback
        KeyValuePair<string, LobSettings> match =
            _options.FirstOrDefault(kvp => string.Equals(kvp.Key, lobName, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrEmpty(match.Key)) return false;

        settings = match.Value;

        return true;
    }

    #endregion
}

public sealed class LobContextAccessor : ILobContextAccessor
{
    public string? LobName { get; set; }
}
