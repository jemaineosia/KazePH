using KazePH.Application.Services;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using KazePH.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Tests.Services;

public class DisputeServiceTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    private static (DisputeService svc, WalletService walletSvc, KazeDbContext db) CreateSut()
    {
        var db        = DbContextHelper.Create();
        var walletSvc = new WalletService(db);
        var svc       = new DisputeService(db, walletSvc);
        return (svc, walletSvc, db);
    }

    private static async Task<Event> SeedActiveEventAsync(
        KazeDbContext db, string creatorId, string opponentId)
    {
        // Wallets for the participants
        db.Wallets.Add(new Wallet { Id = Guid.NewGuid(), UserId = creatorId,  AvailableBalance = 0m, LockedBalance = 500m });
        db.Wallets.Add(new Wallet { Id = Guid.NewGuid(), UserId = opponentId, AvailableBalance = 0m, LockedBalance = 300m });

        var ev = new Event
        {
            Id        = Guid.NewGuid(),
            CreatorId = creatorId,
            Title     = "Test Event",
            EventType = EventType.OneVsOne,
            Status    = EventStatus.Disputed,
            SideA     = "Side A",
            SideB     = "Side B",
            EventDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(ev);

        db.BetEntries.Add(new BetEntry { Id = Guid.NewGuid(), EventId = ev.Id, UserId = creatorId,  Side = "A", Amount = 500m, CreatedAt = DateTime.UtcNow });
        db.BetEntries.Add(new BetEntry { Id = Guid.NewGuid(), EventId = ev.Id, UserId = opponentId, Side = "B", Amount = 300m, CreatedAt = DateTime.UtcNow });

        await db.SaveChangesAsync();
        return ev;
    }

    // ── OpenDisputeAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task OpenDisputeAsync_CreatesDispute_SetsEventToDisputed()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        var ev = new Event
        {
            Id        = Guid.NewGuid(),
            CreatorId = creatorId,
            Title     = "Event",
            EventType = EventType.OneVsOne,
            Status    = EventStatus.Active,
            SideA     = "A",
            SideB     = "B",
            EventDate = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(ev);
        await db.SaveChangesAsync();

        var dispute = await svc.OpenDisputeAsync(ev.Id, creatorId);

        Assert.NotEqual(Guid.Empty, dispute.Id);
        Assert.Equal(ev.Id,               dispute.EventId);
        Assert.Equal(DisputeStatus.Open,  dispute.Status);

        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Disputed, updatedEvent!.Status);
    }

    [Fact]
    public async Task OpenDisputeAsync_Throws_WhenEventNotFound()
    {
        var (svc, _, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.OpenDisputeAsync(Guid.NewGuid(), "user-1"));
    }

    // ── SubmitEvidenceAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task SubmitEvidenceAsync_CreatesEvidence_EscalatesToUnderReview()
    {
        var (svc, _, db) = CreateSut();
        var dispute = new Dispute
        {
            Id             = Guid.NewGuid(),
            EventId        = Guid.NewGuid(),
            OpenedByUserId = "user-1",
            Status         = DisputeStatus.Open,
            CreatedAt      = DateTime.UtcNow
        };
        db.Disputes.Add(dispute);
        await db.SaveChangesAsync();

        var evidence = await svc.SubmitEvidenceAsync(
            dispute.Id, "user-1", "https://screenshot.url/img.png", "Here is my proof");

        Assert.NotEqual(Guid.Empty, evidence.Id);
        Assert.Equal("https://screenshot.url/img.png", evidence.EvidenceUrl);
        Assert.Equal("Here is my proof",               evidence.Description);

        var updatedDispute = await db.Disputes.FindAsync(dispute.Id);
        Assert.Equal(DisputeStatus.UnderReview, updatedDispute!.Status);
    }

    [Fact]
    public async Task SubmitEvidenceAsync_Throws_WhenDisputeAlreadyResolved()
    {
        var (svc, _, db) = CreateSut();
        var dispute = new Dispute
        {
            Id             = Guid.NewGuid(),
            EventId        = Guid.NewGuid(),
            OpenedByUserId = "user-1",
            Status         = DisputeStatus.Resolved,
            CreatedAt      = DateTime.UtcNow
        };
        db.Disputes.Add(dispute);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SubmitEvidenceAsync(dispute.Id, "user-1", "https://img.url", null));
    }

    [Fact]
    public async Task SubmitEvidenceAsync_Throws_WhenDisputeNotFound()
    {
        var (svc, _, _) = CreateSut();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SubmitEvidenceAsync(Guid.NewGuid(), "user-1", "https://img.url", null));
    }

    // ── ResolveDisputeAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task ResolveDisputeAsync_CreditsFundsToWinners_SetsCompleted()
    {
        var (svc, walletSvc, db) = CreateSut();
        const string creatorId  = "creator-disp";
        const string opponentId = "opponent-disp";
        var ev = await SeedActiveEventAsync(db, creatorId, opponentId);

        var dispute = new Dispute
        {
            Id             = Guid.NewGuid(),
            EventId        = ev.Id,
            OpenedByUserId = creatorId,
            Status         = DisputeStatus.UnderReview,
            CreatedAt      = DateTime.UtcNow
        };
        db.Disputes.Add(dispute);
        await db.SaveChangesAsync();

        // Admin resolves: Side A wins
        await svc.ResolveDisputeAsync(dispute.Id, "Admin reviewed and Side A won.", "A");

        var updatedDispute = await db.Disputes.FindAsync(dispute.Id);
        Assert.Equal(DisputeStatus.Resolved,             updatedDispute!.Status);
        Assert.Equal("Admin reviewed and Side A won.",   updatedDispute.AdminNote);
        Assert.NotNull(updatedDispute.ResolvedAt);

        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Completed, updatedEvent!.Status);

        // Winner (Side A = creator) receives share of total pot (500 + 300 = 800)
        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(800m, cWallet.AvailableBalance);
    }

    [Fact]
    public async Task ResolveDisputeAsync_Throws_WhenAlreadyResolved()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        var ev = await SeedActiveEventAsync(db, creatorId, opponentId);

        var dispute = new Dispute
        {
            Id             = Guid.NewGuid(),
            EventId        = ev.Id,
            OpenedByUserId = creatorId,
            Status         = DisputeStatus.Resolved,
            CreatedAt      = DateTime.UtcNow,
            AdminNote      = "Already resolved"
        };
        db.Disputes.Add(dispute);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ResolveDisputeAsync(dispute.Id, "Try again", "A"));
    }
}
