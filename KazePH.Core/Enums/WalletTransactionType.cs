namespace KazePH.Core.Enums;

/// <summary>
/// Describes the nature of a wallet transaction log entry.
/// </summary>
public enum WalletTransactionType
{
    /// <summary>Funds moved from available to locked escrow when a bet is placed.</summary>
    StakeLocked = 0,

    /// <summary>Locked stake returned to available (event cancelled or winner receiving stake back).</summary>
    StakeReleased = 1,

    /// <summary>Opponent's stake credited to the winner's available balance.</summary>
    WinningsReceived = 2,

    /// <summary>Losing stake removed from locked balance (forfeited to the winner).</summary>
    StakeForfeited = 3,

    /// <summary>Stake returned to both players when an event ends in a draw.</summary>
    DrawRefund = 4,

    /// <summary>Funds added from an approved top-up / deposit.</summary>
    TopUp = 5,

    /// <summary>Funds deducted for a processed withdrawal.</summary>
    Withdrawal = 6,
}
