using KazePH.Application.Services;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using KazePH.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Tests.Services;

public class EventServiceTests
{
    // ── Factory ───────────────────────────────────────────────────────────────

    private static (EventService svc, WalletService walletSvc, KazeDbContext db) CreateSut()
    {
        var db          = DbContextHelper.Create();
        var walletSvc   = new WalletService(db);
        var eventSvc    = new EventService(db, walletSvc);
        return (eventSvc, walletSvc, db);
    }

    private static async Task SeedWalletAsync(KazeDbContext db, string userId, decimal available = 1000m)
    {
        db.Wallets.Add(new Wallet
        {
            Id               = Guid.NewGuid(),
            UserId           = userId,
            AvailableBalance = available,
            LockedBalance    = 0m
        });
        await db.SaveChangesAsync();
    }

    // ── CreateEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CreateEventAsync_OneVsOne_CreatesEventAndBetEntry()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "NBA Finals", "Game 7", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "Lakers", "Celtics",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);

        Assert.NotEqual(Guid.Empty, ev.Id);
        Assert.Equal(EventStatus.Open, ev.Status);
        var entry = await db.BetEntries.FirstOrDefaultAsync(b => b.EventId == ev.Id && b.UserId == creatorId);
        Assert.NotNull(entry);
        Assert.Equal("A", entry.Side);
        Assert.Equal(500m, entry.Amount);
    }

    [Fact]
    public async Task CreateEventAsync_OneVsOne_LocksCreatorStakeInWallet()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        await svc.CreateEventAsync(
            creatorId, "NBA Finals", "Game 7", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "Lakers", "Celtics",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);

        var wallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(500m, wallet.AvailableBalance);
        Assert.Equal(500m, wallet.LockedBalance);
    }

    [Fact]
    public async Task CreateEventAsync_OneVsOne_LogsStakeLockedTransaction()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "NBA Finals", "Game 7", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "Lakers", "Celtics",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);

        var txn = await db.WalletTransactions
            .FirstOrDefaultAsync(t => t.UserId == creatorId && t.EventId == ev.Id);
        Assert.NotNull(txn);
        Assert.Equal(WalletTransactionType.StakeLocked, txn.Type);
        Assert.Equal(500m, txn.Amount);
    }

    [Fact]
    public async Task CreateEventAsync_OneVsOne_Throws_WhenMissingCreatorSide()
    {
        var (svc, _, db) = CreateSut();
        await SeedWalletAsync(db, "creator-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateEventAsync(
                "creator-1", "Event", "Desc", DateTime.UtcNow.AddDays(1),
                EventType.OneVsOne, "Side A", "Side B",
                creatorSide: null, creatorStake: 500m, opponentStake: 300m));
    }

    [Fact]
    public async Task CreateEventAsync_Pool_CreatesEventWithNoLock()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "PBA Finals", "Game 5", DateTime.UtcNow.AddDays(2),
            EventType.Pool, "TNT", "Magnolia");

        Assert.Equal(EventStatus.Open, ev.Status);
        var wallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(1000m, wallet.AvailableBalance); // nothing locked for pool events
        Assert.Equal(0m, wallet.LockedBalance);
    }

    // ── AcceptChallengeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task AcceptChallengeAsync_LocksOpponentStake_SetsEventToActive()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId   = "creator-1";
        const string opponentId  = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "NBA Finals", "Game 7", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "Lakers", "Celtics",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);

        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        var opponentWallet = await db.Wallets.FirstAsync(w => w.UserId == opponentId);
        Assert.Equal(200m, opponentWallet.AvailableBalance);
        Assert.Equal(300m, opponentWallet.LockedBalance);

        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Active, updatedEvent!.Status);
    }

    [Fact]
    public async Task AcceptChallengeAsync_Throws_WhenCreatorAcceptsOwnChallenge()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Self Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 200m, opponentStake: 200m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AcceptChallengeAsync(ev.Id, creatorId));
    }

    [Fact]
    public async Task AcceptChallengeAsync_Throws_WhenDirectChallengeAcceptedByWrongUser()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string targetId   = "target-1";
        const string intruderId = "intruder-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, intruderId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Direct", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 200m, opponentStake: 200m,
            challengedUserId: targetId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.AcceptChallengeAsync(ev.Id, intruderId));
    }

    // ── RejectChallengeAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task RejectChallengeAsync_RefundsCreator_CancelsEvent()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId     = "creator-1";
        const string challengedId  = "challenged-1";
        await SeedWalletAsync(db, creatorId,    available: 1000m);
        await SeedWalletAsync(db, challengedId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Direct Challenge", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m,
            challengedUserId: challengedId);

        await svc.RejectChallengeAsync(ev.Id, challengedId);

        // Event cancelled
        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Cancelled, updatedEvent!.Status);

        // Creator's locked stake fully returned
        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(1000m, cWallet.AvailableBalance);
        Assert.Equal(0m,    cWallet.LockedBalance);
    }

    [Fact]
    public async Task RejectChallengeAsync_Throws_WhenCreatorTriesToRejectOwnChallenge()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId = "creator-1";
        await SeedWalletAsync(db, creatorId, available: 1000m);

        var ev = await svc.CreateEventAsync(
            creatorId, "My Challenge", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 200m, opponentStake: 200m);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectChallengeAsync(ev.Id, creatorId));
    }

    [Fact]
    public async Task RejectChallengeAsync_Throws_WhenWrongUserTriesToRejectDirectChallenge()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId    = "creator-1";
        const string targetId     = "target-1";
        const string intruderId   = "intruder-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, intruderId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Direct", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 200m, opponentStake: 200m,
            challengedUserId: targetId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectChallengeAsync(ev.Id, intruderId));
    }

    [Fact]
    public async Task RejectChallengeAsync_Throws_WhenEventNotOpen()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId    = "creator-1";
        const string challengedId = "challenged-1";
        await SeedWalletAsync(db, creatorId,    available: 1000m);
        await SeedWalletAsync(db, challengedId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Challenge", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 200m, opponentStake: 200m,
            challengedUserId: challengedId);

        // Accept first so it's Active, then try to reject
        await svc.AcceptChallengeAsync(ev.Id, challengedId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.RejectChallengeAsync(ev.Id, challengedId));
    }

    // ── CancelEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task CancelEventAsync_RefundsAllParticipants_SetsEventToCancelled()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        await svc.CancelEventAsync(ev.Id);

        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        var oWallet = await db.Wallets.FirstAsync(w => w.UserId == opponentId);
        Assert.Equal(1000m, cWallet.AvailableBalance);
        Assert.Equal(0m,    cWallet.LockedBalance);
        Assert.Equal(500m,  oWallet.AvailableBalance);
        Assert.Equal(0m,    oWallet.LockedBalance);

        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Cancelled, updatedEvent!.Status);
    }

    [Fact]
    public async Task CancelEventAsync_Throws_WhenEventAlreadyCompleted()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);
        await svc.DeclareResultAsync(ev.Id, creatorId, "A");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CancelEventAsync(ev.Id));
    }

    // ── SubmitDeclarationAsync ────────────────────────────────────────────────

    private async Task<(Guid eventId, string creatorId, string opponentId)>
        SetupActiveEventAsync(KazeDbContext db, EventService svc)
    {
        const string creatorId  = "creator-decl";
        const string opponentId = "opponent-decl";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Decl Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "Lakers", "Celtics",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        return (ev.Id, creatorId, opponentId);
    }

    [Fact]
    public async Task SubmitDeclaration_BothAgreeOnWinner_SettlesEventAndPaysOut()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, creatorId, opponentId) = await SetupActiveEventAsync(db, svc);

        // Both declare Side A wins
        await svc.SubmitDeclarationAsync(eventId, creatorId,  "A");
        await svc.SubmitDeclarationAsync(eventId, opponentId, "A");

        var updatedEvent = await db.Events.FindAsync(eventId);
        Assert.Equal(EventStatus.Completed, updatedEvent!.Status);

        var result = await db.EventResults.FirstOrDefaultAsync(r => r.EventId == eventId);
        Assert.NotNull(result);
        Assert.Equal("A", result.DeclaredWinningSide);

        // Creator (winner): 500 remaining avail + 500 stake released + 300 from loser pot = 1300
        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(1300m, cWallet.AvailableBalance);
        Assert.Equal(0m,    cWallet.LockedBalance);

        // Opponent (loser): locked funds forfeited
        var oWallet = await db.Wallets.FirstAsync(w => w.UserId == opponentId);
        Assert.Equal(0m, oWallet.LockedBalance);
    }

    [Fact]
    public async Task SubmitDeclaration_BothClaimVictory_EscalatesToDispute()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, creatorId, opponentId) = await SetupActiveEventAsync(db, svc);

        // Creator declares A wins, opponent declares B wins (both claim victory)
        await svc.SubmitDeclarationAsync(eventId, creatorId,  "A");
        await svc.SubmitDeclarationAsync(eventId, opponentId, "B");

        var updatedEvent = await db.Events.FindAsync(eventId);
        Assert.Equal(EventStatus.Disputed, updatedEvent!.Status);

        var dispute = await db.Disputes.FirstOrDefaultAsync(d => d.EventId == eventId);
        Assert.NotNull(dispute);
        Assert.Equal(DisputeStatus.Open, dispute.Status);
    }

    [Fact]
    public async Task SubmitDeclaration_DifferentResults_EscalatesToDispute()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, creatorId, opponentId) = await SetupActiveEventAsync(db, svc);

        // Creator says A won, opponent says Draw — any disagreement = dispute
        await svc.SubmitDeclarationAsync(eventId, creatorId,  "A");
        await svc.SubmitDeclarationAsync(eventId, opponentId, "Draw");

        var updatedEvent = await db.Events.FindAsync(eventId);
        Assert.Equal(EventStatus.Disputed, updatedEvent!.Status);

        var dispute = await db.Disputes.FirstOrDefaultAsync(d => d.EventId == eventId);
        Assert.NotNull(dispute);
    }

    [Fact]
    public async Task SubmitDeclaration_BothDeclareDraw_SettlesDraw()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, creatorId, opponentId) = await SetupActiveEventAsync(db, svc);

        // Both click the Draw button
        await svc.SubmitDeclarationAsync(eventId, creatorId,  "Draw");
        await svc.SubmitDeclarationAsync(eventId, opponentId, "Draw");

        var updatedEvent = await db.Events.FindAsync(eventId);
        Assert.Equal(EventStatus.Completed, updatedEvent!.Status);

        var result = await db.EventResults.FirstOrDefaultAsync(r => r.EventId == eventId);
        Assert.NotNull(result);
        Assert.Equal("Draw", result.DeclaredWinningSide);

        // Both get stakes returned — creator: 500 remaining + 500 released = 1000
        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        var oWallet = await db.Wallets.FirstAsync(w => w.UserId == opponentId);
        Assert.Equal(1000m, cWallet.AvailableBalance);
        Assert.Equal(0m,    cWallet.LockedBalance);
        Assert.Equal(500m,  oWallet.AvailableBalance); // 200 remaining + 300 refund
        Assert.Equal(0m,    oWallet.LockedBalance);
    }

    [Fact]
    public async Task SubmitDeclaration_Throws_WhenNotParticipant()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, _, _) = await SetupActiveEventAsync(db, svc);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SubmitDeclarationAsync(eventId, "random-user", "A"));
    }

    [Fact]
    public async Task SubmitDeclaration_Throws_WhenAlreadyDeclared()
    {
        var (svc, _, db) = CreateSut();
        var (eventId, creatorId, _) = await SetupActiveEventAsync(db, svc);

        await svc.SubmitDeclarationAsync(eventId, creatorId, "A");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.SubmitDeclarationAsync(eventId, creatorId, "A"));
    }

    // ── DeclareResultAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task DeclareResultAsync_SettlesWinner_CompletesEvent()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        await svc.DeclareResultAsync(ev.Id, creatorId, "A");

        var updatedEvent = await db.Events.FindAsync(ev.Id);
        Assert.Equal(EventStatus.Completed, updatedEvent!.Status);

        var result = await db.EventResults.FirstOrDefaultAsync(r => r.EventId == ev.Id);
        Assert.NotNull(result);
        Assert.Equal("A", result.DeclaredWinningSide);

        var cWallet = await db.Wallets.FirstAsync(w => w.UserId == creatorId);
        Assert.Equal(1300m, cWallet.AvailableBalance); // 500 remaining + 500 released + 300 from loser
    }

    [Fact]
    public async Task DeclareResultAsync_Throws_WhenNotCreator()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeclareResultAsync(ev.Id, opponentId, "B"));
    }

    [Fact]
    public async Task DeclareResultAsync_Throws_WhenInvalidWinningSide()
    {
        var (svc, _, db) = CreateSut();
        const string creatorId  = "creator-1";
        const string opponentId = "opponent-1";
        await SeedWalletAsync(db, creatorId,  available: 1000m);
        await SeedWalletAsync(db, opponentId, available: 500m);

        var ev = await svc.CreateEventAsync(
            creatorId, "Event", "Desc", DateTime.UtcNow.AddDays(1),
            EventType.OneVsOne, "A", "B",
            creatorSide: "A", creatorStake: 500m, opponentStake: 300m);
        await svc.AcceptChallengeAsync(ev.Id, opponentId);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.DeclareResultAsync(ev.Id, creatorId, "C"));
    }
}
