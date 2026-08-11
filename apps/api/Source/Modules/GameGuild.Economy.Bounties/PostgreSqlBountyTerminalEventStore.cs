using System.Text.Json;
using GameGuild.Economy.Bounties.Persistence;
using GameGuild.Economy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Bounties;

/// <summary>
/// Immutable terminal evidence for a bounty. The terminal writer stores this only after the
/// corresponding registered posting has been accepted in the same database transaction.
/// </summary>
public sealed record PersistedBountyTerminalEvent(
    Guid Id,
    BountyId BountyId,
    BountyStatus Status,
    Guid ActorId,
    WalletId DestinationWalletId,
    IdempotencyKey IdempotencyKey,
    Guid? RiskDecisionId,
    SourceStampId? ProceedsSourceStampId,
    CreditLotId? ProceedsLotId,
    long ReturnedUnits,
    long FeeUnits,
    long FirstJournalSequence,
    IReadOnlyList<BountyTerminalOutputLot> OutputLots,
    DateTimeOffset OccurredAt);

/// <summary>
/// Describes a terminal output before the specialized claim/reclaim writer materializes it.
/// The stored JSON is evidence only; balance authority remains the immutable journal and lots.
/// </summary>
public sealed record BountyTerminalOutputLot(
    CreditLotId LotId,
    WalletId WalletId,
    CoinAmount Amount,
    ProvenanceKind Provenance,
    SourceStampId RootSourceStampId,
    DateTimeOffset ConfirmedAt,
    DateTimeOffset OriginalMaturesAt,
    bool CashOutEligible);

public sealed record CompleteBountyTerminalEventCommand(
    Guid Id,
    BountyId BountyId,
    BountyStatus Status,
    Guid ActorId,
    WalletId DestinationWalletId,
    IdempotencyKey IdempotencyKey,
    Guid? RiskDecisionId,
    SourceStampId? ProceedsSourceStampId,
    CreditLotId? ProceedsLotId,
    long ReturnedUnits,
    long FeeUnits,
    long FirstJournalSequence,
    IReadOnlyList<BountyTerminalOutputLot> OutputLots,
    DateTimeOffset OccurredAt)
{
    public void Validate()
    {
        if (Id == Guid.Empty)
            throw new ArgumentException("Terminal event ID is required.", nameof(Id));
        if (Status is not (BountyStatus.Claimed or BountyStatus.Reclaimed))
            throw new ArgumentOutOfRangeException(nameof(Status), "Only terminal bounty statuses can be persisted.");
        if (ActorId == Guid.Empty)
            throw new ArgumentException("Terminal actor ID is required.", nameof(ActorId));
        ArgumentOutOfRangeException.ThrowIfNegative(ReturnedUnits);
        ArgumentOutOfRangeException.ThrowIfNegative(FeeUnits);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(FirstJournalSequence);
        ArgumentNullException.ThrowIfNull(OutputLots);

        if (Status == BountyStatus.Claimed &&
            (!RiskDecisionId.HasValue || !ProceedsSourceStampId.HasValue || !ProceedsLotId.HasValue))
        {
            throw new ArgumentException(
                "A bounty claim requires the consumed risk decision and immutable proceeds identifiers.",
                nameof(RiskDecisionId));
        }

        if (Status == BountyStatus.Reclaimed &&
            (RiskDecisionId.HasValue || ProceedsSourceStampId.HasValue || ProceedsLotId.HasValue))
        {
            throw new ArgumentException(
                "A bounty reclaim cannot carry claim-only risk or proceeds identifiers.",
                nameof(RiskDecisionId));
        }

        if (OutputLots.GroupBy(lot => lot.LotId).Any(group => group.Count() > 1))
            throw new ArgumentException("Terminal output lots must have unique identities.", nameof(OutputLots));
        if (OutputLots.Any(lot => lot.OriginalMaturesAt < lot.ConfirmedAt))
            throw new ArgumentException("Terminal output lot maturity must not precede confirmation.", nameof(OutputLots));
    }
}

public interface IBountyTerminalEventStore
{
    PersistedBountyTerminalEvent? FindByBounty(BountyId bountyId);

    PersistedBountyTerminalEvent? FindByIdempotency(IdempotencyKey idempotencyKey);

    PersistedBountyTerminalEvent Complete(CompleteBountyTerminalEventCommand command);
}

/// <summary>
/// Persists terminal bounty outcomes through private SECURITY DEFINER procedures. It never
/// writes bounty state directly, which keeps terminal transitions auditable and race-safe.
/// </summary>
public sealed class PostgreSqlBountyTerminalEventStore : IBountyTerminalEventStore
{
    private readonly DbContext _db;

    public PostgreSqlBountyTerminalEventStore(IApplicationDbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        _db = context as DbContext
            ?? throw new InvalidOperationException(
                "PostgreSQL bounty terminal persistence requires the application's relational DbContext.");
    }

    public PersistedBountyTerminalEvent? FindByBounty(BountyId bountyId) =>
        Read("""
            SELECT * FROM economy_private.read_bounty_terminal_by_bounty_v1({0})
            """, bountyId.Value).SingleOrDefault() is { } row
            ? ToContract(row)
            : null;

    public PersistedBountyTerminalEvent? FindByIdempotency(IdempotencyKey idempotencyKey) =>
        Read("""
            SELECT * FROM economy_private.read_bounty_terminal_by_idempotency_v1({0})
            """, idempotencyKey.Value).SingleOrDefault() is { } row
            ? ToContract(row)
            : null;

    public PersistedBountyTerminalEvent Complete(CompleteBountyTerminalEventCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        command.Validate();
        var payload = JsonSerializer.Serialize(command.OutputLots.Select(BountyTerminalOutputLotPayload.From));

        _db.Database.ExecuteSqlInterpolated($"""
            SELECT economy_private.complete_bounty_terminal_v1(
                {command.Id},
                {command.BountyId.Value},
                {(int)command.Status},
                {command.ActorId},
                {command.DestinationWalletId.Value},
                {command.IdempotencyKey.Value},
                {command.RiskDecisionId},
                {command.ProceedsSourceStampId?.Value},
                {command.ProceedsLotId?.Value},
                {command.ReturnedUnits},
                {command.FeeUnits},
                {command.FirstJournalSequence},
                {payload}::jsonb,
                {command.OccurredAt});
            """);

        return FindByBounty(command.BountyId)
            ?? throw new InvalidOperationException("The bounty terminal writer did not persist an outcome.");
    }

    private IQueryable<BountyTerminalEventRow> Read(string sql, params object[] parameters) =>
        _db.Set<BountyTerminalEventRow>().FromSqlRaw(sql, parameters).AsNoTracking();

    private static PersistedBountyTerminalEvent ToContract(BountyTerminalEventRow row) => new(
        row.Id,
        new BountyId(row.BountyId),
        row.Status,
        row.ActorId,
        new WalletId(row.DestinationWalletId),
        new IdempotencyKey(row.IdempotencyKey),
        row.RiskDecisionId,
        row.ProceedsSourceStampId is { } sourceStampId ? new SourceStampId(sourceStampId) : null,
        row.ProceedsLotId is { } proceedsLotId ? new CreditLotId(proceedsLotId) : null,
        row.ReturnedUnits,
        row.FeeUnits,
        row.FirstJournalSequence,
        JsonSerializer.Deserialize<BountyTerminalOutputLotPayload[]>(row.OutputLots)?.Select(item => item.ToContract()).ToArray()
            ?? throw new InvalidOperationException("Persisted bounty terminal output evidence is missing."),
        row.OccurredAt);

    private sealed record BountyTerminalOutputLotPayload(
        Guid LotId,
        Guid WalletId,
        int Currency,
        long AmountUnits,
        int Provenance,
        Guid RootSourceStampId,
        DateTimeOffset ConfirmedAt,
        DateTimeOffset OriginalMaturesAt,
        bool CashOutEligible)
    {
        public static BountyTerminalOutputLotPayload From(BountyTerminalOutputLot lot) => new(
            lot.LotId.Value,
            lot.WalletId.Value,
            (int)lot.Amount.Currency,
            lot.Amount.Units,
            (int)lot.Provenance,
            lot.RootSourceStampId.Value,
            lot.ConfirmedAt,
            lot.OriginalMaturesAt,
            lot.CashOutEligible);

        public BountyTerminalOutputLot ToContract() => new(
            new CreditLotId(LotId),
            new WalletId(WalletId),
            new CoinAmount((CurrencyCode)Currency, AmountUnits),
            (ProvenanceKind)Provenance,
            new SourceStampId(RootSourceStampId),
            ConfirmedAt,
            OriginalMaturesAt,
            CashOutEligible);
    }
}
