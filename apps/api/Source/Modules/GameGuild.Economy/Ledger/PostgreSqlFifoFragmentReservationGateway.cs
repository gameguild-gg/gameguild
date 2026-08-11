using System.Data.Common;
using GameGuild.Economy.Contracts;
using GameGuild.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Ledger;

public enum PersistedFragmentReservationPurpose
{
    Payout = 1,
    AdminWithdrawal = 2,
    HardToSoftConversion = 3,
    Spend = 4,
    ProviderReversal = 5,
    BountyEscrow = 6
}

public enum PersistedFragmentReservationStatus
{
    Reserved = 1,
    Released = 2,
    Consumed = 3
}

public sealed record FifoFragmentReservationRequest(
    Guid OperationId,
    WalletId WalletId,
    CurrencyCode Currency,
    ProvenanceKind Provenance,
    CoinAmount Amount,
    PersistedFragmentReservationPurpose Purpose,
    DateTimeOffset ReservedAt);

public sealed record PersistedFragmentReservation(
    Guid Id,
    Guid OperationId,
    CreditLotId ParentLotId,
    SourceStampId RootSourceStampId,
    long ReversalEpoch,
    RootTraceRange Range,
    CoinAmount Amount);

public interface IFifoFragmentReservationGateway
{
    IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request);

    long Transition(
        Guid operationId,
        PersistedFragmentReservationStatus expected,
        PersistedFragmentReservationStatus next,
        DateTimeOffset terminalAt);
}

public sealed class PostgreSqlFifoFragmentReservationGateway : IFifoFragmentReservationGateway
{
    private readonly DbContext _db;

    public PostgreSqlFifoFragmentReservationGateway(IApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "Persistent FIFO reservations require the application's relational DbContext.");
    }

    public IReadOnlyList<PersistedFragmentReservation> Reserve(FifoFragmentReservationRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.OperationId == Guid.Empty) throw new ArgumentException("Operation ID is required.", nameof(request));
        if (!Enum.IsDefined(request.Purpose)) throw new ArgumentOutOfRangeException(nameof(request));
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(request.Amount.Units);
        if (request.Amount.Currency != request.Currency)
            throw new ArgumentException("Reservation currency must match its amount.", nameof(request));

        try
        {
            return ReservationReceipts(request)
                .AsNoTracking()
                .AsEnumerable()
                .Select(row => new PersistedFragmentReservation(
                    row.ReservationId,
                    request.OperationId,
                    new CreditLotId(row.ParentLotId),
                    new SourceStampId(row.RootSourceStampId),
                    row.ReversalEpoch,
                    new RootTraceRange(
                        new SourceStampId(row.RootSourceStampId),
                        row.StartInclusive,
                        checked(row.EndExclusive - row.StartInclusive),
                        row.ReversalEpoch),
                    new CoinAmount(request.Currency, row.AmountUnits)))
                .ToArray();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new RegisteredPostingRejectedException(
                "The persistent Economy FIFO reservation writer rejected the request.", exception);
        }
    }

    private IQueryable<FifoFragmentReservationReceiptRow> ReservationReceipts(
        FifoFragmentReservationRequest request) =>
        request.Purpose == PersistedFragmentReservationPurpose.BountyEscrow
            ? _db.Set<FifoFragmentReservationReceiptRow>()
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM economy_private.reserve_bounty_fifo_fragments_v1(
                        {request.OperationId},
                        {request.WalletId.Value},
                        {(int)request.Currency},
                        {(int)request.Provenance},
                        {request.Amount.Units},
                        {(int)request.Purpose},
                        {request.ReservedAt})
                    """)
            : _db.Set<FifoFragmentReservationReceiptRow>()
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM economy_private.reserve_fifo_fragments_v1(
                        {request.OperationId},
                        {request.WalletId.Value},
                        {(int)request.Currency},
                        {(int)request.Provenance},
                        {request.Amount.Units},
                        {(int)request.Purpose},
                        {request.ReservedAt})
                    """);

    public long Transition(
        Guid operationId,
        PersistedFragmentReservationStatus expected,
        PersistedFragmentReservationStatus next,
        DateTimeOffset terminalAt)
    {
        if (operationId == Guid.Empty) throw new ArgumentException("Operation ID is required.", nameof(operationId));
        if (!Enum.IsDefined(expected) || !Enum.IsDefined(next)) throw new ArgumentOutOfRangeException(nameof(expected));

        try
        {
            return _db.Database.SqlQuery<long>($"""
                    SELECT economy_private.transition_fifo_fragment_reservations_v1(
                        {operationId}, {(int)expected}, {(int)next}, {terminalAt}) AS "Value"
                    """)
                .Single();
        }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            throw new RegisteredPostingRejectedException(
                "The persistent Economy FIFO reservation transition was rejected.", exception);
        }
    }

    private static bool IsDatabaseFailure(Exception exception) =>
        exception is DbException or DbUpdateException or InvalidOperationException ||
        exception.GetBaseException() is DbException;
}
