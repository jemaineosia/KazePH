namespace KazePH.Core.Models;

/// <summary>
/// Holds the monetary balances for a single user.
/// </summary>
public class Wallet
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the owning <see cref="User"/>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Funds freely available for placing bets or withdrawing.</summary>
    public decimal AvailableBalance { get; set; }

    /// <summary>Funds currently locked in an active bet escrow.</summary>
    public decimal LockedBalance { get; set; }

    /// <summary>Funds in a pending withdrawal request, not yet released.</summary>
    public decimal PendingWithdrawalBalance { get; set; }

    /// <summary>Last time any balance field was modified (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property to the owning user.</summary>
    public User? User { get; set; }
}
