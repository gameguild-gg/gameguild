using GameGuild.Economy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Payouts;

/// <summary>
/// Uses database-owned procedures so application roles cannot insert or mutate payout requests
/// outside their constrained state transitions.
/// </summary>
public sealed class PostgreSqlPayoutRequestStore : IPayoutRequestStore
{
    private readonly DbContext _db;

    public PostgreSqlPayoutRequestStore(IApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "PostgreSQL payout request persistence requires the application's relational DbContext.");
    }

    public PayoutRequest? FindReplay(Guid payeeId, string idempotencyKey, string requestHash)
    {
        if (payeeId == Guid.Empty)
            throw new ArgumentException("Payee ID is required.", nameof(payeeId));
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(requestHash);

        var row = Read($"""
            SELECT * FROM economy_private.read_payout_request_by_idempotency_v1({payeeId}, {idempotencyKey.Trim()})
            """).SingleOrDefault();
        if (row is null)
            return null;
        if (!string.Equals(row.RequestHash, requestHash, StringComparison.Ordinal))
            throw new PayoutRequestReplayConflictException(
                "Payout request idempotency key was reused with different inputs.");

        return ToContract(row);
    }

    public void Add(PayoutRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Execute($"""
            SELECT economy_private.create_payout_request_v1(
                {request.Id},
                {request.IdempotencyKey.Value},
                {request.RequestHash},
                {request.PayeeId},
                {request.WalletId.Value},
                {request.Amount.Units},
                {(int)request.State},
                {request.Version},
                {request.CreatedAt},
                {request.UpdatedAt});
            """);
    }

    public PayoutRequest GetForPayee(Guid requestId, Guid payeeId)
    {
        if (requestId == Guid.Empty)
            throw new ArgumentException("Payout request ID is required.", nameof(requestId));
        if (payeeId == Guid.Empty)
            throw new ArgumentException("Payee ID is required.", nameof(payeeId));

        var row = Read($"""
            SELECT * FROM economy_private.read_payout_request_by_id_for_payee_v1({requestId}, {payeeId})
            """).SingleOrDefault();
        return row is null
            ? throw new KeyNotFoundException($"Payout request {requestId:N} was not found.")
            : ToContract(row);
    }

    public IReadOnlyList<PayoutRequest> ListForPayee(Guid payeeId, int take)
    {
        if (payeeId == Guid.Empty)
            throw new ArgumentException("Payee ID is required.", nameof(payeeId));
        if (take is < 1 or > 100)
            throw new ArgumentOutOfRangeException(nameof(take), "Take must be between 1 and 100.");

        return Read($"""
            SELECT * FROM economy_private.read_payout_requests_by_payee_v1({payeeId}, {take})
            """)
            .OrderByDescending(row => row.CreatedAt)
            .ThenByDescending(row => row.Id)
            .Select(ToContract)
            .ToArray();
    }

    public PayoutRequest Update(PayoutRequest request, long expectedVersion)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (expectedVersion <= 0)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion));

        Execute($"""
            SELECT economy_private.transition_payout_request_v1(
                {request.Id},
                {request.PayeeId},
                {expectedVersion},
                {(int)request.State},
                {request.UpdatedAt});
            """);
        return request;
    }

    private IQueryable<PayoutRequestRow> Read(FormattableString sql) =>
        _db.Database.SqlQuery<PayoutRequestRow>(sql).AsNoTracking();

    private void Execute(FormattableString sql)
    {
        try
        {
            _db.Database.ExecuteSqlInterpolated(sql);
        }
        catch (Exception exception) when (exception.Message.Contains("payout request", StringComparison.OrdinalIgnoreCase))
        {
            throw Translate(exception);
        }
    }

    private static Exception Translate(Exception exception) =>
        exception.Message.Contains("idempotency", StringComparison.OrdinalIgnoreCase)
            ? new PayoutRequestReplayConflictException(exception.Message)
            : new PayoutRequestStaleCommandException(exception.Message);

    private static PayoutRequest ToContract(PayoutRequestRow row) => new(
        row.Id,
        new IdempotencyKey(row.IdempotencyKey),
        row.RequestHash,
        row.PayeeId,
        new WalletId(row.WalletId),
        new CoinAmount(CurrencyCode.HardCoin, row.AmountUnits),
        row.State,
        row.Version,
        row.CreatedAt,
        row.UpdatedAt);
}

public sealed class PayoutRequestRow
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid PayeeId { get; set; }
    public Guid WalletId { get; set; }
    public long AmountUnits { get; set; }
    public PayoutRequestState State { get; set; }
    public long Version { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
