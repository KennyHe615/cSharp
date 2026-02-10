namespace Application.Common.Enums;

public enum SyncCategory
{
    #region References

    References, // For logging use
    Skill,
    PresenceDefinition,
    Group,
    WrapupCode,

    #endregion

    #region Analytics

    UserDetails,
    ConversationDetails

    #endregion
}
