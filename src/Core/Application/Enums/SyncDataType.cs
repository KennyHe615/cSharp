namespace Application.Enums;

public enum SyncDataType
{
    #region References

    Skill,
    PresenceDefinition,
    Group,
    WrapUpCode,

    #endregion

    #region Analytics

    UserDetailsIncremental,
    UserDetailsRecovery,
    ConversationDetails

    #endregion
}
