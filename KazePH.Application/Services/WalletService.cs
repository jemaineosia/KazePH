using KazePH.Application.Interfaces;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="IWalletService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class WalletService : IWalletService
{
    private readonly KazeDbContext _db;

    /// <summary>Initializes a new instance of <see cref="WalletService"/>.</summary>
    public WalletService(KazeDbContext db) => _db = db;

    /// <inheritdoc />
    public Task<Wallet?> GetBalanceAsync(string userId, CancellationToken cancellationToken = default)
        => _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken);

    /// <inheritdoc />
    public async Task LockFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await GetRequiredWallet(userId, cancellationToken);
        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient available balance to lock.");

        wallet.AvailableBalance -= amount;
        wallet.LockedBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task ReleaseFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await GetRequiredWallet(userId, cancellationToken);
        if (wallet.LockedBalance < amount)
            throw new InvalidOperationException("Insufficient locked balance to release.");

        wallet.LockedBalance -= amount;
        wallet.AvailableBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CreditFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await GetRequiredWallet(userId, cancellationToken);
        wallet.AvailableBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DebitFundsAsync(string userId, decimal amount, CancellationToken cancellationToken = default)
    {
        var wallet = await GetRequiredWallet(userId, cancellationToken);
        if (wallet.AvailableBalance < amount)
            throw new InvalidOperationException("Insufficient available balance to debit.");

        wallet.AvailableBalance -= amount;
        wallet.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<Wallet> GetRequiredWallet(string userId, CancellationToken ct)
    {
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, ct);
        return wallet ?? throw new InvalidOperationException($"Wallet not found for user '{userId}'.");
    }

    /// <inheritdoc />
    public void Log(string userId, WalletTransactionType type, decimal amount, string description, Guid? eventId = null)
    {
        _db.WalletTransactions.Add(new WalletTransaction
        {
            Id          = Guid.NewGuid(),
            UserId      = userId,
            Type        = type,
            Amount      = amount,
            Description = description,
            EventId     = eventId,
            CreatedAt   = DateTime.UtcNow
        });
    }
}
