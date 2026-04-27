using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a platform payment account (GCash, Bank, PayPal) managed by admin.
/// Stored as a JSON array in PlatformConfig under the key "PaymentAccounts".
/// </summary>
public class PaymentAccount
{
    /// <summary>Unique identifier for this account entry.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Payment channel type.</summary>
    public PaymentMethod Type { get; set; }

    /// <summary>Admin-facing label (e.g. "GCash Main", "BDO Savings").</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Account/phone number shown to users.</summary>
    public string AccountNumber { get; set; } = string.Empty;

    /// <summary>Account holder name shown to users.</summary>
    public string AccountName { get; set; } = string.Empty;

    /// <summary>Bank name — only relevant when Type is Bank.</summary>
    public string? BankName { get; set; }

    /// <summary>Whether this account is currently visible to users on the Cash-In page.</summary>
    public bool IsActive { get; set; } = true;
}
