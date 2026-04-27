using System.Text.Json;
using KazePH.Application.Interfaces;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Stores payment accounts as a JSON array in PlatformConfig["PaymentAccounts"].
/// </summary>
public class PaymentAccountService : IPaymentAccountService
{
    private const string ConfigKey = "PaymentAccounts";
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    private readonly KazeDbContext _db;
    public PaymentAccountService(KazeDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<List<PaymentAccount>> GetAllAsync(CancellationToken ct = default)
    {
        var config = await _db.PlatformConfigs.FirstOrDefaultAsync(c => c.Key == ConfigKey, ct);
        if (config is null || string.IsNullOrWhiteSpace(config.Value)) return new();
        return JsonSerializer.Deserialize<List<PaymentAccount>>(config.Value, _json) ?? new();
    }

    /// <inheritdoc />
    public async Task<List<PaymentAccount>> GetActiveAsync(CancellationToken ct = default)
    {
        var all = await GetAllAsync(ct);
        return all.Where(a => a.IsActive).ToList();
    }

    /// <inheritdoc />
    public async Task SaveAllAsync(List<PaymentAccount> accounts, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(accounts);
        var config = await _db.PlatformConfigs.FirstOrDefaultAsync(c => c.Key == ConfigKey, ct);
        if (config is null)
        {
            _db.PlatformConfigs.Add(new PlatformConfig
            {
                Id = Guid.NewGuid(),
                Key = ConfigKey,
                Value = json,
                Description = "Platform payment accounts for Cash-In",
                UpdatedAt = DateTime.UtcNow
            });
        }
        else
        {
            config.Value = json;
            config.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync(ct);
    }
}
