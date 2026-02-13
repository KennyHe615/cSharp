namespace Shared.Constants;

public static class GenesysConstants
{
    #region Orgs

    public const string NttOrg = "NTT";
    public const string CrcOrg = "CRC";
    public const string LclOrg = "LCL";

    #endregion

    #region Apis

    public const string OAuthBaseUrl = "https://login.cac1.pure.cloud";
    public const string OAuthEndpoint = "oauth/token";
    public const string ApiBaseUrl = "https://api.cac1.pure.cloud";
    public const int DefaultPageSize = 100;
    public const string DefaultQueryOrder = "asc";
    public const int HistoricalDataLimitDays = 558;
    public const int MaxHitThreshold = 100_000;
    public const int MaxIntervalDays = 7;

    #endregion
}
