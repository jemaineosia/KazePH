using KazePH.Core.Enums;

namespace KazePH.Core.Models;

/// <summary>
/// Represents a registered platform user.
/// </summary>
public class User
{
    /// <summary>Primary key (maps to ASP.NET Core Identity user ID).</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Full legal name of the user.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Unique display name chosen by the user.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Mobile phone number used for OTP verification.</summary>
    public string PhoneNumber { get; set; } = string.Empty;

    /// <summary>Indicates whether the phone number has been verified via OTP.</summary>
    public bool PhoneVerified { get; set; }

    /// <summary>URL of the user's profile avatar stored in Supabase Storage.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Current ranking tier based on completed events and conduct.</summary>
    public RankTier RankTier { get; set; } = RankTier.Rookie;

    /// <summary>Number of conduct strikes issued by admin (3 = permanent ban).</summary>
    public int Strikes { get; set; }

    /// <summary>Date and time the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
