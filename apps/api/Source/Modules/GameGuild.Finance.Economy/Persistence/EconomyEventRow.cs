namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy event ledger
///     (#1092 Tier 2.1 and 2.5). The kind selects which subject
///     column carries the parent aggregate, which partial unique
///     indexes apply, and which payload columns the kind-shape
///     check requires.
/// </summary>
public enum EconomyEventStreamKind
{
    /// <summary>Source-stamp confirmation event (formerly
    ///     <c>economy_source_stamp_events</c>) — appended by the
    ///     SECURITY DEFINER funding and reversal writers.</summary>
    SourceStamp = 1,

    /// <summary>Provider dispute lifecycle event (formerly
    ///     <c>economy_provider_dispute_events</c>) — keyed by the
    ///     provider's event id.</summary>
    ProviderDispute = 2,

    /// <summary>Wallet debt movement (formerly
    ///     <c>economy_wallet_debt_events</c>) — written inside the
    ///     provider reversal writers.</summary>
    WalletDebt = 3,

    /// <summary>Funding hold event (formerly
    ///     <c>economy_hold_events</c>) — appended inside the
    ///     marketplace settlement and refund writers.</summary>
    FundingHold = 4,

    /// <summary>Risk review case event (formerly
    ///     <c>economy_risk_review_events</c>).</summary>
    RiskReview = 5,

    /// <summary>Compliance hold event (formerly
    ///     <c>economy_compliance_hold_events</c>).</summary>
    ComplianceHold = 6,

    /// <summary>Legacy cutover audit entry (formerly
    ///     <c>economy_legacy_cutover_audit</c>).</summary>
    LegacyCutover = 7,

    /// <summary>Marketplace settlement event (formerly
    ///     <c>economy_marketplace_events</c>).</summary>
    MarketplaceSettlement = 8,

    /// <summary>Ad-reward session lifecycle event (formerly the
    ///     <c>TimelineKind</c> = 1 half of
    ///     <c>economy_ad_reward_session_timeline</c>).</summary>
    AdRewardSessionEvent = 9,

    /// <summary>Ad-reward playback milestone (formerly the
    ///     <c>TimelineKind</c> = 2 half of
    ///     <c>economy_ad_reward_session_timeline</c>). Kinds 9 and
    ///     10 keep independent sequence streams per session.</summary>
    AdRewardPlaybackMilestone = 10,

    /// <summary>Admin withdrawal run audit entry (formerly
    ///     <c>economy_admin_withdrawal_audit_events</c>) — a hash
    ///     chain computed by the Treasury append function.</summary>
    AdminWithdrawalAudit = 11,

    /// <summary>Bounty expiration record (formerly
    ///     <c>economy_bounty_expiration_events</c>, Tier 2.5) —
    ///     one row per bounty.</summary>
    BountyExpiration = 12,
}

/// <summary>
///     Unified Economy event ledger (#1092 Tier 2.1 and 2.5: eleven
///     event and audit streams merged behind
///     <see cref="EconomyEventStreamKind"/>). Every stream keeps its
///     former ordering and replay guarantees as kind-filtered partial
///     unique indexes. Reads must carry the kind predicate together
///     with the kind-exclusive subject column; the kind-shape check
///     constraint keeps each kind bound to its own columns.
/// </summary>
public sealed class EconomyEventRow
{
    public Guid Id { get; set; }
    public EconomyEventStreamKind StreamKind { get; set; }

    /// <summary>Order of the event inside its parent's stream.</summary>
    public long Sequence { get; set; }

    /// <summary>Tenant (kinds 8 and 11 only).</summary>
    public Guid? TenantId { get; set; }

    /// <summary>Integer kind or state (kinds 1, 4, 5, 7 and 9 only).</summary>
    public int? Kind { get; set; }

    /// <summary>Text kind (kinds 6, 8 and 11 only).</summary>
    public string? KindText { get; set; }

    public Guid? ActorId { get; set; }

    public string? EvidenceHash { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    /// <summary>Source stamp an event belongs to (kinds 1, 2 and 3).</summary>
    public Guid? SourceStampId { get; set; }

    /// <summary>Provider event id (kind 2 only) — the provider-side
    ///     replay key.</summary>
    public string? ProviderEventId { get; set; }

    /// <summary>Provider dispute an event belongs to (kind 2 only).</summary>
    public string? ProviderDisputeReference { get; set; }

    /// <summary>Dispute status (kind 2 only).</summary>
    public int? Status { get; set; }

    /// <summary>Cumulative disputed hard units (kind 2 only).</summary>
    public long? CumulativeDisputedHardUnits { get; set; }

    /// <summary>Provider request hash (kind 2 only).</summary>
    public string? RequestHash { get; set; }

    /// <summary>Wallet debt an event belongs to, by the debt's wallet
    ///     (kind 3 only).</summary>
    public Guid? DebtWalletId { get; set; }

    /// <summary>Signed debt movement (kind 3 only).</summary>
    public long? DeltaHardUnits { get; set; }

    /// <summary>Debt outstanding after the movement (kind 3 only).</summary>
    public long? OutstandingHardUnits { get; set; }

    /// <summary>Funding hold an event belongs to (kind 4 only).</summary>
    public Guid? HoldId { get; set; }

    /// <summary>Risk review case an event belongs to (kind 5 only).</summary>
    public Guid? RiskReviewCaseId { get; set; }

    /// <summary>Hashes of the evidences behind a review decision
    ///     (kind 5 only) — a JSON array.</summary>
    public string? EvidenceHashes { get; set; }

    /// <summary>Review resolution (kind 5 only).</summary>
    public string? Resolution { get; set; }

    /// <summary>Manual decision code (kind 5 only).</summary>
    public int? DecisionCode { get; set; }

    /// <summary>Compliance hold an event belongs to (kind 6 only).</summary>
    public Guid? ComplianceHoldId { get; set; }

    /// <summary>Legacy cutover, by tenant, an audit entry belongs to
    ///     (kind 7 only).</summary>
    public Guid? LegacyCutoverTenantId { get; set; }

    /// <summary>Legacy shadow batch an audit entry belongs to
    ///     (kind 7 only).</summary>
    public Guid? BatchId { get; set; }

    /// <summary>Cutover audit reason (kind 7 only).</summary>
    public string? Reason { get; set; }

    /// <summary>Hash of the reauthentication behind a cutover step
    ///     (kind 7 only).</summary>
    public string? ReauthenticationHash { get; set; }

    /// <summary>Marketplace settlement an event belongs to
    ///     (kind 8 only).</summary>
    public Guid? SettlementId { get; set; }

    /// <summary>Ad-reward session an event belongs to (kinds 9 and
    ///     10 only).</summary>
    public Guid? SessionId { get; set; }

    /// <summary>Playback milestone percentage (kind 10 only).</summary>
    public int? Percentage { get; set; }

    /// <summary>Admin withdrawal run an audit entry belongs to
    ///     (kind 11 only).</summary>
    public Guid? RunId { get; set; }

    /// <summary>Audit evidence (kind 11 only).</summary>
    public string? Evidence { get; set; }

    /// <summary>Hash of the previous audit entry in the chain
    ///     (kind 11 only).</summary>
    public string? PreviousHash { get; set; }

    /// <summary>Chain hash of this audit entry (kind 11 only).</summary>
    public string? Hash { get; set; }

    /// <summary>Expired bounty (kind 12 only).</summary>
    public Guid? BountyId { get; set; }

    /// <summary>Expiry that triggered the record (kind 12 only).</summary>
    public DateTimeOffset? ExpiresAt { get; set; }

    /// <summary>Bounty version the expiration was recorded against
    ///     (kind 12 only).</summary>
    public long? BountyVersion { get; set; }
}
