namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified provider-event replay-dedupe anchor
///     (#1092 Tier 1.4).
/// </summary>
public enum EconomyProviderEventKind
{
    /// <summary>Payout provider events
    /// (formerly <c>economy_payout_provider_events</c>).</summary>
    Payout = 1,

    /// <summary>Treasury admin-withdrawal provider events
    /// (formerly <c>economy_admin_withdrawal_provider_events</c>).</summary>
    AdminWithdrawal = 2,
}

/// <summary>
///     Unified provider-event row (#1092 Tier 1.4: the payout and
///     admin-withdrawal provider-event tables merged behind the
///     <see cref="EconomyProviderEventKind"/> discriminator). These rows are
///     the replay-dedupe anchors for provider webhooks — every lookup
///     (including <c>economy_private.read_payout_provider_event_v1</c> and
///     the treasury store) must filter on the kind in addition to
///     <see cref="EventId"/>. Rows are append-only: the deny-mutation
///     trigger rejects UPDATE and DELETE.
/// </summary>
public sealed class EconomyProviderEventRow
{
    public Guid Id { get; set; }
    public EconomyProviderEventKind ProviderEventKind { get; set; }
    public Guid? TenantId { get; set; }
    public string EventId { get; set; } = string.Empty;
    public string EventHash { get; set; } = string.Empty;
    public Guid? OperationId { get; set; }
    public Guid? RunId { get; set; }
    public int? ResultingState { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}
