namespace KazePH.Core.Enums;

/// <summary>
/// Status of a top-up (deposit) request.
/// </summary>
public enum TopUpStatus
{
    /// <summary>Awaiting admin review.</summary>
    Pending,

    /// <summary>Admin approved the deposit; balance has been credited.</summary>
    Approved,

    /// <summary>Admin rejected the deposit request.</summary>
    Rejected
}
