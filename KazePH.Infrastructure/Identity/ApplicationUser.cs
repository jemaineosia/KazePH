using Microsoft.AspNetCore.Identity;

namespace KazePH.Infrastructure.Identity;

/// <summary>
/// Extends <see cref="IdentityUser"/> with KazePH-specific profile fields.
/// Stored in the Supabase PostgreSQL database alongside standard Identity tables.
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Full legal name of the user.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>Unique display name chosen by the user.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>Whether the phone number has been verified via OTP.</summary>
    public bool PhoneVerified { get; set; }

    /// <summary>URL of the user's avatar image in Supabase Storage.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>Date and time the account was created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
