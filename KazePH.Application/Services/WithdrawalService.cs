using KazePH.Application.Interfaces;
using KazePH.Core.Enums;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="IWithdrawalService"/> with fee calculation logic.
/// Fee = (standard fee % × amount) + standard fixed fee [+ PayPal extra % × amount + PayPal extra fixed fee].
/// </summary>
public class WithdrawalService : IWithdrawalService
{
    private readonly KazeDbContext _db;
    private readonly IWalletService _walletService;
    private readonly IPlatformConfigService _config;
    private readonly IConfiguration _appConfig;

    /// <summary>Initializes a new instance of <see cref="WithdrawalService"/>.</summary>
    public WithdrawalService(KazeDbContext db, IWalletService walletService, IPlatformConfigService config, IConfiguration appConfig)
    {
        _db = db;
        _walletService = walletService;
        _config = config;
        _appConfig = appConfig;
    }

    /// <inheritdoc />
    public async Task<WithdrawalRequest> RequestWithdrawalAsync(
        string userId,
        decimal amount,
        PaymentMethod paymentMethod,
        string destinationDetails,
        decimal agentTip = 0,
        CancellationToken cancellationToken = default)
    {
        var fee = await CalculateFeeAsync(amount, paymentMethod, cancellationToken);
        var netAmount = amount - fee;

        if (netAmount <= 0)
            throw new InvalidOperationException("The withdrawal fee exceeds the requested amount.");

        var tip = Math.Max(0, agentTip);
        var totalDebit = amount + tip;

        // Debit withdrawal amount + tip from available balance
        await _walletService.DebitFundsAsync(userId, totalDebit, cancellationToken);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == userId, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet not found for user '{userId}'.");

        wallet.PendingWithdrawalBalance += amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        var request = new WithdrawalRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Amount = amount,
            Fee = fee,
            NetAmount = netAmount,
            AgentTip = tip,
            PaymentMethod = paymentMethod,
            DestinationDetails = destinationDetails,
            Status = WithdrawalStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        _db.WithdrawalRequests.Add(request);
        await _db.SaveChangesAsync(cancellationToken);
        return request;
    }

    /// <inheritdoc />
    public async Task ProcessWithdrawalAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestByStatus(requestId, WithdrawalStatus.Pending, cancellationToken);
        request.Status = WithdrawalStatus.Processing;
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task CompleteWithdrawalAsync(Guid requestId, string? receiptUrl = null, CancellationToken cancellationToken = default)
    {
        var request = await GetRequestByStatus(requestId, WithdrawalStatus.Processing, cancellationToken);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet not found for user '{request.UserId}'.");

        wallet.PendingWithdrawalBalance -= request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        request.Status = WithdrawalStatus.Completed;
        request.ReceiptUrl = receiptUrl;
        request.ProcessedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task RejectWithdrawalAsync(Guid requestId, string adminNote, CancellationToken cancellationToken = default)
    {
        var request = await _db.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId, cancellationToken)
            ?? throw new InvalidOperationException($"WithdrawalRequest '{requestId}' not found.");

        if (request.Status is WithdrawalStatus.Completed or WithdrawalStatus.Rejected)
            throw new InvalidOperationException("Cannot reject a completed or already rejected withdrawal.");

        // Return funds to available balance
        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken)
            ?? throw new InvalidOperationException($"Wallet not found for user '{request.UserId}'.");

        wallet.PendingWithdrawalBalance -= request.Amount;
        wallet.AvailableBalance += request.Amount;
        wallet.UpdatedAt = DateTime.UtcNow;

        request.Status = WithdrawalStatus.Rejected;
        request.AdminNote = adminNote;
        request.ProcessedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<decimal> CalculateFeeAsync(decimal amount, PaymentMethod paymentMethod, CancellationToken ct)
    {
        // Prefer appsettings-configured fee; fall back to DB platform config
        var feePercent = _appConfig.GetValue<decimal>("Withdrawal:FeePercent", 0);
        if (feePercent == 0)
            feePercent = await _config.GetDecimalConfigAsync("WithdrawalFeePercent", 0, ct);

        var feeFixed = await _config.GetDecimalConfigAsync("WithdrawalFeeFixed", 0, ct);

        var fee = (amount * feePercent / 100m) + feeFixed;

        if (paymentMethod == PaymentMethod.PayPal)
        {
            var payPalPercent = await _config.GetDecimalConfigAsync("PayPalExtraFeePercent", 0, ct);
            var payPalFixed = await _config.GetDecimalConfigAsync("PayPalExtraFeeFixed", 0, ct);
            fee += (amount * payPalPercent / 100m) + payPalFixed;
        }

        return Math.Round(fee, 4);
    }

    private async Task<WithdrawalRequest> GetRequestByStatus(Guid requestId, WithdrawalStatus expectedStatus, CancellationToken ct)
    {
        var request = await _db.WithdrawalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException($"WithdrawalRequest '{requestId}' not found.");

        if (request.Status != expectedStatus)
            throw new InvalidOperationException($"WithdrawalRequest '{requestId}' is not in '{expectedStatus}' status.");

        return request;
    }
}
