using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a user's request to withdraw funds from their wallet to an external account.
/// </summary>
public class WithdrawalRequest
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the requesting <see cref="User"/>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Gross amount the user wants to withdraw before fees.</summary>
    public decimal Amount { get; set; }

    /// <summary>Platform fee deducted from the withdrawal amount.</summary>
    public decimal Fee { get; set; }

    /// <summary>Net amount the user actually receives after fee deduction.</summary>
    public decimal NetAmount { get; set; }

    /// <summary>Optional tip the user voluntarily adds for the agent processing their request.</summary>
    public decimal AgentTip { get; set; }

    /// <summary>Chosen payout channel.</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// JSON blob containing destination details (e.g., GCash number, bank account info,
    /// or PayPal email). Format depends on the selected payment method.
    /// </summary>
    public string DestinationDetails { get; set; } = "{}";

    /// <summary>Current processing status of the withdrawal.</summary>
    public WithdrawalStatus Status { get; set; } = WithdrawalStatus.Pending;

    /// <summary>Optional note from the admin explaining a rejection or status update.</summary>
    public string? AdminNote { get; set; }

    /// <summary>URL of the receipt uploaded by admin after sending the payout.</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Date and time the withdrawal was requested (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date and time the withdrawal was completed or rejected (UTC).</summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>Navigation property to the requesting user.</summary>
    public User? User { get; set; }
}
