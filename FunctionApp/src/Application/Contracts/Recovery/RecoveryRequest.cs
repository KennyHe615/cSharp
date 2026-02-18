using System.ComponentModel.DataAnnotations;

using Application.Common.Enums;


namespace Application.Contracts.Recovery;

public class RecoveryRequest
{
    public RecoveryLob Lob { get; set; }

    public SyncCategory? Category { get; set; }

    public string? Interval { get; set; }

    public string? JobId { get; set; }
}

public enum RecoveryLob
{
    Ntt,
    Lcl,
    Crc
}
