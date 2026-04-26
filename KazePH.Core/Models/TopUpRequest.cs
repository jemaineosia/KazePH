using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a user's request to deposit funds into their wallet.
/// The user sends money to a platform account and uploads a receipt for admin review.
/// </summary>
public class TopUpRequest
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Foreign key to the requesting <see cref="User"/>.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Declared amount the user claims to have sent.</summary>
    public decimal Amount { get; set; }

    /// <summary>Payment method used for this top-up.</summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>URL of the uploaded payment receipt in Supabase Storage.</summary>
    public string? ReceiptUrl { get; set; }

    /// <summary>Current review status of the top-up request.</summary>
    public TopUpStatus Status { get; set; } = TopUpStatus.Pending;

    /// <summary>Optional note from the admin explaining an approval or rejection.</summary>
    public string? AdminNote { get; set; }

    /// <summary>Date and time the request was submitted (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Date and time the admin reviewed the request (UTC); null if still pending.</summary>
    public DateTime? ReviewedAt { get; set; }

    /// <summary>Navigation property to the requesting user.</summary>
    public User? User { get; set; }
}
