using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Provides operations for reading and modifying user wallet balances.
/// All monetary operations use PHP (Philippine Peso) as the currency unit.
/// </summary>
public interface IWalletService
{
    /// <summary>Retrieves the wallet for the specified user.</summary>
    /// <param name="userId">The identity user ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's wallet, or <c>null</c> if not found.</returns>
    Task<Wallet?> GetBalanceAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves <paramref name="amount"/> from <c>AvailableBalance</c> to <c>LockedBalance</c>
    /// to escrow funds for an active bet.
    /// </summary>
    Task LockFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// Moves <paramref name="amount"/> from <c>LockedBalance</c> back to <c>AvailableBalance</c>
    /// when a bet is cancelled.
    /// </summary>
    Task ReleaseFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Adds <paramref name="amount"/> to <c>AvailableBalance</c> (e.g., on approved deposit or won bet).</summary>
    Task CreditFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>Deducts <paramref name="amount"/> from <c>AvailableBalance</c> (e.g., on withdrawal).</summary>
    Task DebitFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default);
}
