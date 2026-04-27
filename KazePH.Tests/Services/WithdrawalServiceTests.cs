using KazePH.Application.Interfaces;
using KazePH.Application.Services;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using KazePH.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;

namespace KazePH.Tests.Services;

public class WithdrawalServiceTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    private static (WithdrawalService svc, Mock<IWalletService> walletMock, KazeDbContext db)
        CreateSut(decimal feePercent = 0m)
    {
        var db         = DbContextHelper.Create();
        var walletMock = new Mock<IWalletService>();

        // Platform config returns 0 fees by default
        var configMock = new Mock<IPlatformConfigService>();
        configMock
            .Setup(c => c.GetDecimalConfigAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0m);

        // App config with configurable fee percent
        var appConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Withdrawal:FeePercent"] = feePercent.ToString()
            })
            .Build();

        var svc = new WithdrawalService(db, walletMock.Object, configMock.Object, appConfig);
        return (svc, walletMock, db);
    }

    private static async Task<Wallet> SeedWalletAsync(
        KazeDbContext db, string userId, decimal available = 1000m, decimal pending = 0m)
    {
        var wallet = new Wallet
        {
            Id                       = Guid.NewGuid(),
            UserId                   = userId,
            AvailableBalance         = available,
            LockedBalance            = 0m,
            PendingWithdrawalBalance = pending
        };
        db.Wallets.Add(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    // ── RequestWithdrawalAsync ────────────────────────────────────────────────

    [Fact]
    public async Task RequestWithdrawalAsync_CreatesPendingRequest()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);

        var request = await svc.RequestWithdrawalAsync(
            "user-1", 500m, PaymentMethod.GCash, "09xxxxxxxxx");

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("user-1",                 request.UserId);
        Assert.Equal(500m,                     request.Amount);
        Assert.Equal(WithdrawalStatus.Pending, request.Status);

        var persisted = await db.WithdrawalRequests.FindAsync(request.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task RequestWithdrawalAsync_IncreasesWalletPendingBalance()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);

        await svc.RequestWithdrawalAsync("user-1", 500m, PaymentMethod.GCash, "09xxxxxxxxx");

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(500m, wallet.PendingWithdrawalBalance);
    }

    [Fact]
    public async Task RequestWithdrawalAsync_DebitsWalletForAmountPlusTip()
    {
        var (svc, walletMock, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);

        await svc.RequestWithdrawalAsync("user-1", 500m, PaymentMethod.GCash, "09xxxxxxxxx", agentTip: 50m);

        // total debit = amount + tip = 550
        walletMock.Verify(w => w.DebitFundsAsync("user-1", 550m, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RequestWithdrawalAsync_Throws_WhenFeeExceedsAmount()
    {
        // 200% fee → fee = 2 × amount → netAmount = amount - fee < 0
        var (svc, _, db) = CreateSut(feePercent: 200m);
        await SeedWalletAsync(db, "user-1", available: 1000m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RequestWithdrawalAsync("user-1", 100m, PaymentMethod.GCash, "09xxxxxxxxx"));
    }

    // ── ProcessWithdrawalAsync ────────────────────────────────────────────────

    [Fact]
    public async Task ProcessWithdrawalAsync_SetsProcessingStatus()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");

        await svc.ProcessWithdrawalAsync(request.Id);

        var updated = await db.WithdrawalRequests.FindAsync(request.Id);
        Assert.Equal(WithdrawalStatus.Processing, updated!.Status);
    }

    [Fact]
    public async Task ProcessWithdrawalAsync_Throws_WhenNotPending()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");
        await svc.ProcessWithdrawalAsync(request.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ProcessWithdrawalAsync(request.Id));
    }

    // ── CompleteWithdrawalAsync ───────────────────────────────────────────────

    [Fact]
    public async Task CompleteWithdrawalAsync_SetsCompletedStatus_ReducesPendingBalance()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");
        await svc.ProcessWithdrawalAsync(request.Id);

        await svc.CompleteWithdrawalAsync(request.Id, "https://receipt.url", "admin01");

        var updated = await db.WithdrawalRequests.FindAsync(request.Id);
        Assert.Equal(WithdrawalStatus.Completed, updated!.Status);
        Assert.Equal("https://receipt.url",      updated.ReceiptUrl);
        Assert.Equal("admin01",                  updated.ProcessedByUsername);
        Assert.NotNull(updated.ProcessedAt);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(0m, wallet.PendingWithdrawalBalance);
    }

    [Fact]
    public async Task CompleteWithdrawalAsync_Throws_WhenNotProcessing()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");

        // Still Pending, not Processing
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CompleteWithdrawalAsync(request.Id));
    }

    // ── RejectWithdrawalAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task RejectWithdrawalAsync_RefundsAvailableBalance_SetsRejected()
    {
        var (svc, walletMock, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);

        // DebitFunds is mocked, so AvailableBalance won't actually decrease.
        // We still verify PendingWithdrawalBalance is returned to 0.
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");

        await svc.RejectWithdrawalAsync(request.Id, "Cannot process at this time", "admin01");

        var updated = await db.WithdrawalRequests.FindAsync(request.Id);
        Assert.Equal(WithdrawalStatus.Rejected,              updated!.Status);
        Assert.Equal("Cannot process at this time",          updated.AdminNote);
        Assert.Equal("admin01",                              updated.ProcessedByUsername);

        // PendingWithdrawalBalance should be zeroed
        var wallet = await db.Wallets.FirstAsync(w => w.UserId == "user-1");
        Assert.Equal(0m, wallet.PendingWithdrawalBalance);
    }

    [Fact]
    public async Task RejectWithdrawalAsync_Throws_WhenAlreadyRejected()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "user-1", available: 1000m);
        var request = await svc.RequestWithdrawalAsync("user-1", 300m, PaymentMethod.GCash, "09xxxxxxxxx");
        await svc.RejectWithdrawalAsync(request.Id, "Reason");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectWithdrawalAsync(request.Id, "Again"));
    }
}
