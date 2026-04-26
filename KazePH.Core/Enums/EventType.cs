namespace KazePH.Core.Enums;

/// <summary>
/// Type of betting event.
/// </summary>
public enum EventType
{
    /// <summary>Head-to-head bet between exactly two participants.</summary>
    OneVsOne,

    /// <summary>Parimutuel/pool bet where multiple participants bet on one of two sides.</summary>
    Pool
}
