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

    UsersDetailsIncremental,
    UsersDetailsRecovery,
    ConversationsDetailsIncremental,
    ConversationsDetailsRecovery,
    ConversationsAggregatesIncremental,
    ConversationsAggregatesRecovery,

    #endregion
}
