using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Provides access to admin-managed platform configuration key-value pairs.
/// All config values are stored as strings; typed helpers are provided for common types.
/// </summary>
public interface IPlatformConfigService
{
    /// <summary>Retrieves the raw string value of a config entry by its key.</summary>
    /// <param name="key">The unique config key (e.g., "WithdrawalFeePercent").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw string value, or <c>null</c> if the key does not exist.</returns>
    Task<string?> GetConfigAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or updates a config entry with the provided value.
    /// </summary>
    /// <param name="key">The unique config key.</param>
    /// <param name="value">New value to store.</param>
    /// <param name="description">Optional description of what this config controls.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetConfigAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a config entry and parses it as a <see cref="decimal"/>.
    /// Returns <paramref name="defaultValue"/> if the key is missing or not parseable.
    /// </summary>
    Task<decimal> GetDecimalConfigAsync(string key, decimal defaultValue = 0, CancellationToken cancellationToken = default);
}
