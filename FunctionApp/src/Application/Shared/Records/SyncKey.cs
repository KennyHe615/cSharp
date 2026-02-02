using Application.Shared.Enums;


namespace Application.Shared.Records;

public readonly record struct SyncKey(string LobName,
                                      SyncCategory Category);
