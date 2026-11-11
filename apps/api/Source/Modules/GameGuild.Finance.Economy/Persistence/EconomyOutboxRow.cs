namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy outbox queue (#1092 Tier 1.1).
///     The kind selects the claim discipline and the completion marker:
///     <see cref="Marketplace"/> completes on <c>PublishedAt</c>; the two
///     dispatch kinds complete on <c>CompletedAt</c> and retry via
///     <c>AvailableAt</c>; <see cref="Posting"/> rows are bridged to the
///     integration outbox by trigger; <see cref="Compliance"/> rows are
///     legacy migrated rows (the write side was retired with the merge —
///     compliance evidence is read from the evidence tables directly).
/// </summary>
public enum EconomyOutboxKind
{
    /// <summary>Core posting-accepted spool (formerly <c>economy_outbox_messages</c>).</summary>
    Posting = 1,

    /// <summary>Marketplace entitlement grant/revoke (formerly <c>economy_marketplace_outbox</c>).</summary>
    Marketplace = 2,

    /// <summary>Payout provider dispatch commands (formerly <c>economy_payout_dispatch_outbox</c>).</summary>
    PayoutDispatch = 3,

    /// <summary>Treasury admin-withdrawal dispatch commands
    /// (formerly <c>economy_admin_withdrawal_dispatch_outbox</c>).</summary>
    AdminWithdrawalDispatch = 4,

    /// <summary>Compliance-evidence publication rows
    /// (formerly <c>economy_compliance_outbox</c>); write side retired.</summary>
    Compliance = 5,
}

/// <summary>
///     Unified Economy outbox row (#1092 Tier 1.1: five lease-based outbox
///     tables merged behind the <see cref="EconomyOutboxKind"/> discriminator).
///     Enqueues stay inside the posting transaction; the payload hash and — for
///     the dispatch kinds — the fencing token embedded in the payload must
///     survive every move of these rows.
/// </summary>
public sealed class EconomyOutboxRow
{
    public Guid Id { get; set; }
    public EconomyOutboxKind OutboxKind { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? PostingGroupId { get; set; }
    public Guid? SettlementId { get; set; }
    public Guid? OperationId { get; set; }
    public Guid? RunId { get; set; }
    public Guid? EvidenceId { get; set; }
    public string MessageType { get; set; } = string.Empty;
    public string? IdempotencyKey { get; set; }
    public string Payload { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset? AvailableAt { get; set; }
    public string? LeaseOwner { get; set; }
    public DateTimeOffset? LeaseExpiresAt { get; set; }
    public int AttemptCount { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? DispatchedAt { get; set; }
    public string? LastError { get; set; }
}
