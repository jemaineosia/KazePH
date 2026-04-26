using KazePH.Application.Interfaces;
using KazePH.Core.Models;
using KazePH.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace KazePH.Application.Services;

/// <summary>
/// Implements <see cref="IPlatformConfigService"/> using <see cref="KazeDbContext"/>.
/// </summary>
public class PlatformConfigService : IPlatformConfigService
{
    private readonly KazeDbContext _db;

    /// <summary>Initializes a new instance of <see cref="PlatformConfigService"/>.</summary>
    public PlatformConfigService(KazeDbContext db) => _db = db;

    /// <inheritdoc />
    public async Task<string?> GetConfigAsync(string key, CancellationToken cancellationToken = default)
    {
        var config = await _db.PlatformConfigs.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        return config?.Value;
    }

    /// <inheritdoc />
    public async Task SetConfigAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
    {
        var config = await _db.PlatformConfigs.FirstOrDefaultAsync(c => c.Key == key, cancellationToken);
        if (config is null)
        {
            config = new PlatformConfig { Id = Guid.NewGuid(), Key = key };
            _db.PlatformConfigs.Add(config);
        }

        config.Value = value;
        config.Description = description ?? config.Description;
        config.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<decimal> GetDecimalConfigAsync(string key, decimal defaultValue = 0, CancellationToken cancellationToken = default)
    {
        var raw = await GetConfigAsync(key, cancellationToken);
        return decimal.TryParse(raw, out var result) ? result : defaultValue;
    }
}
