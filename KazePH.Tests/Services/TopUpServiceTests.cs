using KazePH.Application.Interfaces;
using KazePH.Application.Services;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using KazePH.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace KazePH.Tests.Services;

public class TopUpServiceTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    private static (TopUpService svc, Mock<IWalletService> walletMock, KazeDbContext db) CreateSut()
    {
        var db          = DbContextHelper.Create();
        var walletMock  = new Mock<IWalletService>();
        var svc         = new TopUpService(db, walletMock.Object);
        return (svc, walletMock, db);
    }

    // ── SubmitTopUpAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitTopUpAsync_CreatesPendingRequest()
    {
        var (svc, _, db) = CreateSut();

        var request = await svc.SubmitTopUpAsync(
            "user-1", 500m, PaymentMethod.GCash, "https://receipt.url/img.jpg");

        Assert.NotEqual(Guid.Empty, request.Id);
        Assert.Equal("user-1",            request.UserId);
        Assert.Equal(500m,                request.Amount);
        Assert.Equal(PaymentMethod.GCash, request.PaymentMethod);
        Assert.Equal(TopUpStatus.Pending, request.Status);

        var persisted = await db.TopUpRequests.FindAsync(request.Id);
        Assert.NotNull(persisted);
    }

    [Fact]
    public async Task SubmitTopUpAsync_StoresReceiptUrl()
    {
        var (svc, _, _) = CreateSut();

        var request = await svc.SubmitTopUpAsync(
            "user-1", 200m, PaymentMethod.Bank, "https://storage.supabase.co/receipt.png");

        Assert.Equal("https://storage.supabase.co/receipt.png", request.ReceiptUrl);
    }

    // ── ApproveTopUpAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task ApproveTopUpAsync_SetsApprovedStatus_CreditsFunds()
    {
        var (svc, walletMock, db) = CreateSut();
        var request = await svc.SubmitTopUpAsync("user-1", 500m, PaymentMethod.GCash, null);

        await svc.ApproveTopUpAsync(request.Id, "Verified receipt");

        var updated = await db.TopUpRequests.FindAsync(request.Id);
        Assert.Equal(TopUpStatus.Approved, updated!.Status);
        Assert.Equal("Verified receipt",   updated.AdminNote);
        Assert.NotNull(updated.ReviewedAt);

        walletMock.Verify(w => w.CreditFundsAsync("user-1", 500m, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveTopUpAsync_Throws_WhenRequestNotFound()
    {
        var (svc, _, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ApproveTopUpAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ApproveTopUpAsync_Throws_WhenRequestAlreadyApproved()
    {
        var (svc, _, _) = CreateSut();
        var request = await svc.SubmitTopUpAsync("user-1", 500m, PaymentMethod.GCash, null);
        await svc.ApproveTopUpAsync(request.Id);

        // Approving twice should throw since it's no longer Pending
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ApproveTopUpAsync(request.Id));
    }

    // ── RejectTopUpAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RejectTopUpAsync_SetsRejectedStatus()
    {
        var (svc, _, db) = CreateSut();
        var request = await svc.SubmitTopUpAsync("user-1", 300m, PaymentMethod.GCash, null);

        await svc.RejectTopUpAsync(request.Id, "Receipt is blurry");

        var updated = await db.TopUpRequests.FindAsync(request.Id);
        Assert.Equal(TopUpStatus.Rejected,   updated!.Status);
        Assert.Equal("Receipt is blurry",    updated.AdminNote);
        Assert.NotNull(updated.ReviewedAt);
    }

    [Fact]
    public async Task RejectTopUpAsync_DoesNotCreditFunds()
    {
        var (svc, walletMock, _) = CreateSut();
        var request = await svc.SubmitTopUpAsync("user-1", 300m, PaymentMethod.GCash, null);

        await svc.RejectTopUpAsync(request.Id, "Invalid receipt");

        walletMock.Verify(w => w.CreditFundsAsync(It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RejectTopUpAsync_Throws_WhenRequestNotPending()
    {
        var (svc, _, _) = CreateSut();
        var request = await svc.SubmitTopUpAsync("user-1", 300m, PaymentMethod.GCash, null);
        await svc.RejectTopUpAsync(request.Id, "Bad receipt");

        // Rejecting again should throw
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectTopUpAsync(request.Id, "Again"));
    }
}
