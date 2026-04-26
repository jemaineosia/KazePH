using KazePH.Application.Interfaces;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="ITopUpService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class TopUpService : ITopUpService
{
    private readonly KazeDbContext _db;
    private readonly IWalletService _walletService;

    /// <summary>Initializes a new instance of <see cref="TopUpService"/>.</summary>
    public TopUpService(KazeDbContext db, IWalletService walletService)
    {
        _db = db;
        _walletService = walletService;
    }

    /// <inheritdoc />
    public async Task<TopUpRequest> SubmitTopUpAsync(
        string userId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? receiptUrl,
        CancellationToken cancellationToken = default)
    {
        var request = new TopUpRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            PaymentMethod = paymentMethod,
            ReceiptUrl = receiptUrl,
            Status = TopUpStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.TopUpRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <inheritdoc />
    public async Task ApproveTopUpAsync(Guid requestId, string? adminNote = null, CancellationToken cancellationToken = default)
    {
        var request = await GetPendingRequest(requestId, cancellationToken);
        request.Status = TopUpStatus.Approved;
        request.AdminNote = adminNote;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        await _walletService.CreditFundsAsync(request.UserId, request.Amount, cancellationToken);
    }

    /// <inheritdoc />
    public async Task RejectTopUpAsync(Guid requestId, string adminNote, CancellationToken cancellationToken = default)
    {
        var request = await GetPendingRequest(requestId, cancellationToken);
        request.Status = TopUpStatus.Rejected;
        request.AdminNote = adminNote;
        request.ReviewedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TopUpRequest> GetPendingRequest(Guid requestId, CancellationToken ct)
    {
        var request = await _db.TopUpRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException($"TopUpRequest '{requestId}' not found.");

        if (request.Status != TopUpStatus.Pending)
            throw new InvalidOperationException($"TopUpRequest '{requestId}' is not in Pending status.");

        return request;
    }
}
