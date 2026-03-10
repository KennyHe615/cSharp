namespace Application.Enums;

public enum SyncCategory
{
    #region References

    User,
    Queue,
    Flow,
    Group,
    Skill,
    PresenceDefinition,
    WrapUpCode,

    #endregion

    #region Analytics

    UsersDetails,
    ConversationsDetails,
    ConversationsAggregates,

    #endregion
}
