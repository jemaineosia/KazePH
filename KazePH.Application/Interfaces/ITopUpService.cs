using KazePH.Core.Enums;
using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages the lifecycle of top-up (deposit) requests submitted by users.
/// </summary>
public interface ITopUpService
{
    /// <summary>
    /// Creates a new top-up request after the user has sent money to the platform account
    /// and uploaded a receipt.
    /// </summary>
    /// <param name="userId">The requesting user's identity ID.</param>
    /// <param name="amount">Declared amount sent by the user.</param>
    /// <param name="paymentMethod">Payment channel used.</param>
    /// <param name="receiptUrl">URL of the uploaded receipt in Supabase Storage.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created <see cref="TopUpRequest"/>.</returns>
    Task<TopUpRequest> SubmitTopUpAsync(
        string userId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? receiptUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin approves a pending top-up request; credits the wallet with the declared amount.
    /// </summary>
    /// <param name="requestId">The top-up request ID to approve.</param>
    /// <param name="adminNote">Optional admin note attached to the record.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task ApproveTopUpAsync(Guid requestId, string? adminNote = null, CancellationToken cancellationToken = default);

    /// <summary>Admin rejects a pending top-up request with an explanatory note.</summary>
    /// <param name="requestId">The top-up request ID to reject.</param>
    /// <param name="adminNote">Reason for rejection (shown to user).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RejectTopUpAsync(Guid requestId, string adminNote, CancellationToken cancellationToken = default);
}
