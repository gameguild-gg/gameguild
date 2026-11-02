using GameGuild.CQRS;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Port for Plaid instant bank-account verification (issue #414, optional acceptance
///     box). When a real adapter is configured (<c>Payments:Plaid</c>), Link replaces the
///     multi-day micro-deposit challenge: the customer authenticates with their bank in the
///     Link flow, the returned public token is exchanged for the verified account details,
///     and the bank-account payment method is marked verified immediately. The default
///     implementation is the fail-closed <see cref="NoOpPlaidInstantVerification"/>, so
///     deployments without Plaid credentials keep the micro-deposit flow (issue #1030).
/// </summary>
public interface IPlaidInstantVerification
{
    /// <summary>
    ///     Whether a real Plaid adapter is configured. Callers use this to decide between
    ///     offering instant verification and falling back to micro-deposits.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    ///     Creates a Plaid Link token for the given owner (the client opens Link with it).
    /// </summary>
    /// <param name="tenantId">Owning tenant.</param>
    /// <param name="userId">Customer opening the Link flow.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The single-use Link token with its expiry, or a failure when unavailable.</returns>
    Task<Result<PlaidLinkToken>> CreateLinkTokenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Exchanges the public token returned by a completed Link flow for the verified
    ///     bank-account details. A success carries verified-on-connection semantics: no
    ///     micro-deposit challenge is needed for this account.
    /// </summary>
    /// <param name="publicToken">Public token from the Link flow success callback.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verified account details, or a failure when unavailable.</returns>
    Task<Result<PlaidInstantBankAccount>> ExchangePublicTokenAsync(string publicToken, CancellationToken cancellationToken = default);
}

/// <summary>Single-use Plaid Link token issued for one customer's verification flow.</summary>
/// <param name="LinkToken">The token the client passes to Plaid Link.</param>
/// <param name="ExpiresAtUtc">When the token expires (Plaid issues ~4-hour tokens).</param>
public sealed record PlaidLinkToken(string LinkToken, DateTime ExpiresAtUtc);

/// <summary>
///     Bank-account details returned by a completed Plaid Link flow — display-safe fields
///     only, mirroring what the micro-deposit flow stores.
/// </summary>
/// <param name="PlaidAccountId">Plaid's stable account identifier.</param>
/// <param name="BankName">Bank display name (e.g. "First Platypus Bank").</param>
/// <param name="AccountLast4">Last four digits of the account number (display only).</param>
/// <param name="AccountType">"checking" or "savings".</param>
public sealed record PlaidInstantBankAccount(
    string PlaidAccountId,
    string BankName,
    string AccountLast4,
    string AccountType);
