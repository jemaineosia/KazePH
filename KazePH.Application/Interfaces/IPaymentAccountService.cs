using KazePH.Core.Models;

namespace KazePH.Application.Interfaces;

/// <summary>
/// Manages the list of platform payment accounts stored in PlatformConfig.
/// </summary>
public interface IPaymentAccountService
{
    /// <summary>Returns all payment accounts (active and inactive).</summary>
    Task<List<PaymentAccount>> GetAllAsync(CancellationToken ct = default);

    /// <summary>Returns only accounts with IsActive = true.</summary>
    Task<List<PaymentAccount>> GetActiveAsync(CancellationToken ct = default);

    /// <summary>Persists the full list of payment accounts, replacing any previous data.</summary>
    Task SaveAllAsync(List<PaymentAccount> accounts, CancellationToken ct = default);
}
