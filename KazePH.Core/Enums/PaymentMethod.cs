namespace KazePH.Core.Enums;

/// <summary>
/// Supported payment methods for top-up and withdrawal operations.
/// </summary>
public enum PaymentMethod
{
    /// <summary>GCash mobile wallet (standard fee applies).</summary>
    GCash,

    /// <summary>Bank transfer (standard fee applies).</summary>
    Bank,

    /// <summary>PayPal (standard fee + extra PayPal fee applies).</summary>
    PayPal
}
