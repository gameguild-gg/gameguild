using GameGuild.Economy.Contracts;

namespace GameGuild.Economy.Payouts;

/// <summary>
/// A user-submitted intent to withdraw earned value. It is deliberately separate from a
/// <see cref="PayoutOperation"/>: no funds are reserved or sent until the request has passed
/// the later KYC, risk, provider, and FIFO reservation steps.
/// </summary>
public enum PayoutRequestState
{
    Submitted = 1,
    Cancelled = 2,
    Approved = 3,
    Rejected = 4
}

public sealed record PayoutRequest(
    Guid Id,
    IdempotencyKey IdempotencyKey,
    string RequestHash,
    Guid PayeeId,
    WalletId WalletId,
    CoinAmount Amount,
    PayoutRequestState State,
    long Version,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public PayoutRequest Cancel(DateTimeOffset occurredAt)
    {
        if (State != PayoutRequestState.Submitted)
        {
            throw new PayoutRequestTransitionException("Only a submitted payout request can be cancelled.");
        }
        if (occurredAt < UpdatedAt)
        {
            throw new ArgumentOutOfRangeException(nameof(occurredAt), "Payout request timestamps cannot move backwards.");
        }

        return this with
        {
            State = PayoutRequestState.Cancelled,
            Version = checked(Version + 1),
            UpdatedAt = occurredAt
        };
    }
}

public interface IPayoutRequestStore
{
    PayoutRequest? FindReplay(Guid payeeId, string idempotencyKey, string requestHash);

    void Add(PayoutRequest request);

    PayoutRequest GetForPayee(Guid requestId, Guid payeeId);

    IReadOnlyList<PayoutRequest> ListForPayee(Guid payeeId, int take);

    PayoutRequest Update(PayoutRequest request, long expectedVersion);
}

public sealed class PayoutRequestReplayConflictException(string message) : InvalidOperationException(message);
public sealed class PayoutRequestStaleCommandException(string message) : InvalidOperationException(message);
public sealed class PayoutRequestWalletUnavailableException(string message) : InvalidOperationException(message);
public sealed class PayoutRequestInsufficientWithdrawableFundsException(string message) : InvalidOperationException(message);
public sealed class PayoutRequestTransitionException(string message) : InvalidOperationException(message);
