using KazePH.Core.Models;
using KazePH.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Infrastructure.Data;

/// <summary>
/// Main EF Core database context for KazePH, backed by Supabase (PostgreSQL via Npgsql).
/// Extends <see cref="IdentityDbContext{TUser}"/> to include ASP.NET Core Identity tables.
/// </summary>
public class KazeDbContext : IdentityDbContext<ApplicationUser>
{
    /// <inheritdoc />
    public KazeDbContext(DbContextOptions<KazeDbContext> options) : base(options) { }

    // ── Domain DbSets ────────────────────────────────────────────────────────

    /// <summary>Platform users (domain model, mirrors ApplicationUser).</summary>
    public DbSet<User> KazeUsers { get; set; } = null!;

    /// <summary>User wallets holding available, locked, and pending balances.</summary>
    public DbSet<Wallet> Wallets { get; set; } = null!;

    /// <summary>Deposit requests submitted by users for admin review.</summary>
    public DbSet<TopUpRequest> TopUpRequests { get; set; } = null!;

    /// <summary>Withdrawal requests pending admin processing.</summary>
    public DbSet<WithdrawalRequest> WithdrawalRequests { get; set; } = null!;

    /// <summary>Betting events (1v1 or pool) created by users.</summary>
    public DbSet<Event> Events { get; set; } = null!;

    /// <summary>Individual bet entries placed by participants on events.</summary>
    public DbSet<BetEntry> BetEntries { get; set; } = null!;

    /// <summary>Declared results for completed events.</summary>
    public DbSet<EventResult> EventResults { get; set; } = null!;

    /// <summary>Disputes raised by participants against event outcomes.</summary>
    public DbSet<Dispute> Disputes { get; set; } = null!;

    /// <summary>Evidence files submitted by participants during dispute resolution.</summary>
    public DbSet<DisputeEvidence> DisputeEvidence { get; set; } = null!;

    /// <summary>Friendship requests and established friendships between users.</summary>
    public DbSet<Friendship> Friendships { get; set; } = null!;

    /// <summary>Direct and event group chat messages.</summary>
    public DbSet<ChatMessage> ChatMessages { get; set; } = null!;

    /// <summary>In-app notifications for users.</summary>
    public DbSet<Notification> Notifications { get; set; } = null!;

    /// <summary>Admin-managed platform configuration key-value pairs.</summary>
    public DbSet<PlatformConfig> PlatformConfigs { get; set; } = null!;

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ── Table name conventions ───────────────────────────────────────────
        builder.Entity<User>().ToTable("kaze_users");
        builder.Entity<Wallet>().ToTable("wallets");
        builder.Entity<TopUpRequest>().ToTable("top_up_requests");
        builder.Entity<WithdrawalRequest>().ToTable("withdrawal_requests");
        builder.Entity<Event>().ToTable("events");
        builder.Entity<BetEntry>().ToTable("bet_entries");
        builder.Entity<EventResult>().ToTable("event_results");
        builder.Entity<Dispute>().ToTable("disputes");
        builder.Entity<DisputeEvidence>().ToTable("dispute_evidence");
        builder.Entity<Friendship>().ToTable("friendships");
        builder.Entity<ChatMessage>().ToTable("chat_messages");
        builder.Entity<Notification>().ToTable("notifications");
        builder.Entity<PlatformConfig>().ToTable("platform_configs");

        // ── User ─────────────────────────────────────────────────────────────
        builder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.Property(u => u.Id).HasMaxLength(450);
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.PhoneNumber).HasMaxLength(20).IsRequired();
        });

        // ── Wallet ───────────────────────────────────────────────────────────
        builder.Entity<Wallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId).IsUnique();
            e.Property(w => w.AvailableBalance).HasPrecision(18, 4);
            e.Property(w => w.LockedBalance).HasPrecision(18, 4);
            e.Property(w => w.PendingWithdrawalBalance).HasPrecision(18, 4);
            e.HasOne(w => w.User)
             .WithMany()
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── TopUpRequest ─────────────────────────────────────────────────────
        builder.Entity<TopUpRequest>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Amount).HasPrecision(18, 4);
            e.HasIndex(t => t.UserId);
            e.HasIndex(t => t.Status);
            e.HasOne(t => t.User)
             .WithMany()
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── WithdrawalRequest ────────────────────────────────────────────────
        builder.Entity<WithdrawalRequest>(e =>
        {
            e.HasKey(w => w.Id);
            e.Property(w => w.Amount).HasPrecision(18, 4);
            e.Property(w => w.Fee).HasPrecision(18, 4);
            e.Property(w => w.NetAmount).HasPrecision(18, 4);
            e.HasIndex(w => w.UserId);
            e.HasIndex(w => w.Status);
            e.HasOne(w => w.User)
             .WithMany()
             .HasForeignKey(w => w.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Event ────────────────────────────────────────────────────────────
        builder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Title).HasMaxLength(200).IsRequired();
            e.HasIndex(ev => ev.CreatorId);
            e.HasIndex(ev => ev.Status);
            e.HasOne(ev => ev.Creator)
             .WithMany()
             .HasForeignKey(ev => ev.CreatorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── BetEntry ─────────────────────────────────────────────────────────
        builder.Entity<BetEntry>(e =>
        {
            e.HasKey(b => b.Id);
            e.Property(b => b.Amount).HasPrecision(18, 4);
            e.Property(b => b.Side).HasMaxLength(100).IsRequired();
            e.HasIndex(b => new { b.EventId, b.UserId });
            e.HasOne(b => b.Event)
             .WithMany(ev => ev.BetEntries)
             .HasForeignKey(b => b.EventId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(b => b.User)
             .WithMany()
             .HasForeignKey(b => b.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── EventResult ──────────────────────────────────────────────────────
        builder.Entity<EventResult>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.EventId).IsUnique();
            e.Property(r => r.DeclaredWinningSide).HasMaxLength(100).IsRequired();
            e.HasOne(r => r.Event)
             .WithMany()
             .HasForeignKey(r => r.EventId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Dispute ──────────────────────────────────────────────────────────
        builder.Entity<Dispute>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.EventId);
            e.HasIndex(d => d.Status);
            e.HasOne(d => d.Event)
             .WithMany()
             .HasForeignKey(d => d.EventId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(d => d.OpenedByUser)
             .WithMany()
             .HasForeignKey(d => d.OpenedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── DisputeEvidence ──────────────────────────────────────────────────
        builder.Entity<DisputeEvidence>(e =>
        {
            e.HasKey(de => de.Id);
            e.HasIndex(de => de.DisputeId);
            e.HasOne(de => de.Dispute)
             .WithMany(d => d.Evidence)
             .HasForeignKey(de => de.DisputeId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(de => de.SubmittedByUser)
             .WithMany()
             .HasForeignKey(de => de.SubmittedByUserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── Friendship ───────────────────────────────────────────────────────
        builder.Entity<Friendship>(e =>
        {
            e.HasKey(f => f.Id);
            e.HasIndex(f => new { f.RequesterId, f.AddresseeId }).IsUnique();
            e.HasOne(f => f.Requester)
             .WithMany()
             .HasForeignKey(f => f.RequesterId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(f => f.Addressee)
             .WithMany()
             .HasForeignKey(f => f.AddresseeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ChatMessage ──────────────────────────────────────────────────────
        builder.Entity<ChatMessage>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => c.SenderId);
            e.HasIndex(c => c.ReceiverId);
            e.HasIndex(c => c.EventId);
            e.HasOne(c => c.Sender)
             .WithMany()
             .HasForeignKey(c => c.SenderId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Receiver)
             .WithMany()
             .HasForeignKey(c => c.ReceiverId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Event)
             .WithMany()
             .HasForeignKey(c => c.EventId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Notification ─────────────────────────────────────────────────────
        builder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.UserId);
            e.HasIndex(n => new { n.UserId, n.IsRead });
            e.Property(n => n.Title).HasMaxLength(200).IsRequired();
            e.HasOne(n => n.User)
             .WithMany()
             .HasForeignKey(n => n.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── PlatformConfig ───────────────────────────────────────────────────
        builder.Entity<PlatformConfig>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.Key).IsUnique();
            e.Property(p => p.Key).HasMaxLength(100).IsRequired();
        });
    }
}
