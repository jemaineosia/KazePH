using KazePH.Application.Services;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Tests.Services;

public class WalletServiceTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (WalletService svc, KazePH.Infrastructure.Data.KazeDbContext db) CreateSut()
    {
        var db = DbContextHelper.Create();
        return (new WalletService(db), db);
    }

    private static async Task<Wallet> SeedWalletAsync(
        KazePH.Infrastructure.Data.KazeDbContext db,
        string userId,
        decimal available = 0,
        decimal locked = 0)
    {
        var wallet = new Wallet
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            AvailableBalance = available,
            LockedBalance    = locked
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    // ── GetBalanceAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetBalanceAsync_ReturnsNull_WhenWalletDoesNotExist()
    {
        var (svc, _) = CreateSut();

        var result = await svc.GetBalanceAsync("nonexistent-user");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetBalanceAsync_ReturnsWallet_WhenExists()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 500m);

        var result = await svc.GetBalanceAsync("user-1");

        Assert.NotNull(result);
        Assert.Equal(500m, result.AvailableBalance);
    }

    // ── LockFundsAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task LockFundsAsync_MovesAmountFromAvailableToLocked()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m, locked: 0m);

        await svc.LockFundsAsync("user-1", 400m);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(600m, wallet.AvailableBalance);
        Assert.Equal(400m, wallet.LockedBalance);
    }

    [Fact]
    public async Task LockFundsAsync_Throws_WhenInsufficientAvailableBalance()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.LockFundsAsync("user-1", 500m));
    }

    [Fact]
    public async Task LockFundsAsync_Throws_WhenWalletNotFound()
    {
        var (svc, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.LockFundsAsync("ghost-user", 100m));
    }

    // ── ReleaseFundsAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ReleaseFundsAsync_MovesAmountFromLockedToAvailable()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 0m, locked: 500m);

        await svc.ReleaseFundsAsync("user-1", 300m);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(300m, wallet.AvailableBalance);
        Assert.Equal(200m, wallet.LockedBalance);
    }

    [Fact]
    public async Task ReleaseFundsAsync_Throws_WhenInsufficientLockedBalance()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 0m, locked: 100m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ReleaseFundsAsync("user-1", 500m));
    }

    // ── CreditFundsAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreditFundsAsync_IncreasesAvailableBalance()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 200m);

        await svc.CreditFundsAsync("user-1", 350m);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(550m, wallet.AvailableBalance);
    }

    // ── DebitFundsAsync ───────────────────────────────────────────────────────

    [Fact]
    public async Task DebitFundsAsync_DecreasesAvailableBalance()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);

        await svc.DebitFundsAsync("user-1", 250m);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(750m, wallet.AvailableBalance);
    }

    [Fact]
    public async Task DebitFundsAsync_Throws_WhenInsufficientAvailableBalance()
    {
        var (svc, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 50m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DebitFundsAsync("user-1", 500m));
    }

    // ── Log ───────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Log_AddsWalletTransactionWithCorrectFields()
    {
        var (svc, db) = CreateSut();
        var eventId = Guid.NewGuid();

        svc.Log("user-1", WalletTransactionType.StakeLocked, 500m, "Stake locked: Test Event", eventId);
        await db.SaveChangesAsync();

        var txn = await db.WalletTransactions.FirstAsync(t => t.UserId == "user-1");
        Assert.Equal("user-1", txn.UserId);
        Assert.Equal(WalletTransactionType.StakeLocked, txn.Type);
        Assert.Equal(500m, txn.Amount);
        Assert.Equal("Stake locked: Test Event", txn.Description);
        Assert.Equal(eventId, txn.EventId);
    }

    [Fact]
    public async Task Log_WithNullEventId_StillPersists()
    {
        var (svc, db) = CreateSut();

        svc.Log("user-1", WalletTransactionType.TopUp, 1000m, "Cash in", null);
        await db.SaveChangesAsync();

        var txn = await db.WalletTransactions.FirstAsync(t => t.UserId == "user-1");
        Assert.Null(txn.EventId);
        Assert.Equal(WalletTransactionType.TopUp, txn.Type);
    }
}
