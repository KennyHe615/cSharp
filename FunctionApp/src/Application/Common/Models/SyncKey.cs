using Application.Common.Enums;


namespace Application.Common.Models;

public readonly record struct SyncKey(string LobName,
                                      SyncCategory Category);
