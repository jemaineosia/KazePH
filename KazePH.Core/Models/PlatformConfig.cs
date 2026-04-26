namespace KazePH.Core.Models;

/// <summary>
/// A key-value configuration entry that can be managed by admin from the platform settings panel.
/// </summary>
public class PlatformConfig
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; }

    /// <summary>Unique identifier for the config setting (e.g., "WithdrawalFeePercent").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Serialised value of the config setting.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Human-readable description of what this setting controls.</summary>
    public string? Description { get; set; }

    /// <summary>Date and time the config was last updated (UTC).</summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
