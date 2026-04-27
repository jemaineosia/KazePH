using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Immutable audit record of every balance-affecting wallet operation.
/// </summary>
public class WalletTransaction
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Owner of the wallet this entry belongs to.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Category of this transaction.</summary>
    public WalletTransactionType Type { get; set; }

    /// <summary>Amount involved in this transaction (always positive).</summary>
    public decimal Amount { get; set; }

    /// <summary>Human-readable description, e.g. "Won (Lakers): NBA Finals".</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Optional reference to the related event.</summary>
    public Guid? EventId { get; set; }

    /// <summary>UTC timestamp of this transaction.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? User { get; set; }
    public Event? Event { get; set; }
}
