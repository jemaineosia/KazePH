namespace KazePH.Core.Enums;

/// <summary>
/// Status of a withdrawal request.
/// </summary>
public enum WithdrawalStatus
{
    /// <summary>Request submitted, awaiting admin action.</summary>
    Pending,

    /// <summary>Admin is actively processing the payout.</summary>
    Processing,

    /// <summary>Payout has been sent and confirmed.</summary>
    Completed,

    /// <summary>Request was rejected by admin.</summary>
    Rejected
}
