using KazePH.Core.Enums;
using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages the full lifecycle of user withdrawal requests, including fee calculation.
/// Fee structure:
/// <list type="bullet">
///   <item><description>Standard fee: configured via <c>WithdrawalFeePercent</c> + <c>WithdrawalFeeFixed</c> platform config.</description></item>
///   <item><description>PayPal extra fee: added on top of the standard fee when the method is <see cref="PaymentMethod.PayPal"/>.</description></item>
/// </list>
/// </summary>
public interface IWithdrawalService
{
    /// <summary>
    /// Submits a withdrawal request. Moves the requested amount from <c>AvailableBalance</c>
    /// to <c>PendingWithdrawalBalance</c>.
    /// </summary>
    /// <param name="userId">Requesting user's identity ID.</param>
    /// <param name="amount">Gross amount the user wants to withdraw.</param>
    /// <param name="paymentMethod">Selected payout channel.</param>
    /// <param name="destinationDetails">JSON-serialised destination info (GCash number, bank account, or PayPal email).</param>
    /// <param name="agentTip">Optional voluntary tip the user adds for the processing agent. Deducted separately from wallet.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created <see cref="WithdrawalRequest"/> with fee and net amount pre-calculated.</returns>
    Task<WithdrawalRequest> RequestWithdrawalAsync(
        string userId,
        decimal amount,
        PaymentMethod paymentMethod,
        string destinationDetails,
        decimal agentTip = 0,
        CancellationToken cancellationToken = default);

    /// <summary>Admin marks the withdrawal as being actively processed.</summary>
    Task ProcessWithdrawalAsync(Guid requestId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Admin confirms the payout has been sent. Moves funds out of <c>PendingWithdrawalBalance</c>
    /// and attaches the admin's receipt URL.
    /// </summary>
    Task CompleteWithdrawalAsync(Guid requestId, string? receiptUrl = null, CancellationToken cancellationToken = default);

    /// <summary>Admin rejects the withdrawal request; returns funds to <c>AvailableBalance</c>.</summary>
    Task RejectWithdrawalAsync(Guid requestId, string adminNote, CancellationToken cancellationToken = default);
}
