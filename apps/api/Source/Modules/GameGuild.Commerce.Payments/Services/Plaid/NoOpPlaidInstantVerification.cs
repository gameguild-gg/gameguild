using GameGuild.CQRS;
using Microsoft.Extensions.Options;

namespace GameGuild.Commerce.Payments;

/// <summary>
///     Fail-closed default <see cref="IPlaidInstantVerification"/> (issue #414): served
///     while <c>Payments:Plaid:IsEnabled</c> is false or no real adapter is registered.
///     Reports itself unavailable and fails every call with
///     <c>PaymentMethods.PlaidNotConfigured</c> so callers deterministically fall back to
///     the micro-deposit verification flow — a runtime misconfiguration can never surface
///     as a silent success.
/// </summary>
public sealed class NoOpPlaidInstantVerification(
    IOptions<PlaidInstantVerificationOptions>? options = null) : IPlaidInstantVerification
{
    /// <summary>Shared instance for hosts that construct it without options.</summary>
    public static readonly NoOpPlaidInstantVerification Instance = new();

    private readonly PlaidInstantVerificationOptions _options = options?.Value ?? new PlaidInstantVerificationOptions();

    /// <inheritdoc />
    public bool IsConfigured => false;

    /// <inheritdoc />
    public Task<Result<PlaidLinkToken>> CreateLinkTokenAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<PlaidLinkToken>(NotConfigured()));

    /// <inheritdoc />
    public Task<Result<PlaidInstantBankAccount>> ExchangePublicTokenAsync(string publicToken, CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Failure<PlaidInstantBankAccount>(NotConfigured()));

    private Error NotConfigured() => Error.Problem(
        "PaymentMethods.PlaidNotConfigured",
        $"Plaid instant bank-account verification is not configured (Payments:Plaid). US bank accounts verify through the micro-deposit challenge instead. Current environment: {_options.EnvironmentLabel}.");
}
