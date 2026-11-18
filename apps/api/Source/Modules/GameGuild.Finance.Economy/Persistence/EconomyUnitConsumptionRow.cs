namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy unit-consumption table
///     (#1092 Tier 2.2). The kind selects which subject column carries the
///     consumed aggregate and which partial unique indexes apply.
/// </summary>
public enum EconomyUnitConsumptionKind
{
    /// <summary>Capability receipt burn (formerly
    ///     <c>economy_capability_receipt_consumptions</c>) — one row per
    ///     receipt, SQL-joined by the posting writers to authorize postings.</summary>
    CapabilityReceipt = 1,

    /// <summary>Risk decision burn (formerly
    ///     <c>economy_risk_decision_consumptions</c>) — one row per risk
    ///     decision and per posting group, written inside
    ///     <c>post_registered_posting_v1</c>.</summary>
    RiskDecision = 2,

    /// <summary>Ad-reward cap consumption (formerly
    ///     <c>economy_ad_reward_cap_consumptions</c>) — one row per
    ///     (session, cap scope).</summary>
    AdRewardCap = 3,

    /// <summary>Ad-reward budget consumption (formerly
    ///     <c>economy_ad_reward_budget_consumptions</c>) — one row per
    ///     session.</summary>
    AdRewardBudget = 4,
}

/// <summary>
///     Unified Economy unit consumption (#1092 Tier 2.2: four consumption
///     tables merged behind <see cref="EconomyUnitConsumptionKind"/>). Every
///     kind keeps its former dedupe guarantees as partial unique indexes
///     (receipt id, risk decision id, posting group id, session+scope, and
///     budget session). The SECURITY DEFINER posting writers SQL-join the
///     capability-receipt and risk-decision kinds to authorize postings; the
///     kind predicate must ride along on every join.
/// </summary>
public sealed class EconomyUnitConsumptionRow
{
    public Guid Id { get; set; }
    public EconomyUnitConsumptionKind ConsumptionKind { get; set; }
    public Guid? TenantId { get; set; }

    /// <summary>Consumed capability receipt (kind 1 only).</summary>
    public Guid? ReceiptId { get; set; }

    /// <summary>Consumed risk decision (kind 2 only).</summary>
    public Guid? RiskDecisionId { get; set; }

    /// <summary>Posting group that burned the risk decision (kind 2 only).</summary>
    public Guid? PostingGroupId { get; set; }

    /// <summary>Ad-reward session (kinds 3 and 4 only).</summary>
    public Guid? SessionId { get; set; }

    public Guid? ActorId { get; set; }
    public string? OperationFingerprint { get; set; }
    public long? KillSwitchEpoch { get; set; }
    public int? Scope { get; set; }
    public string? SubjectHash { get; set; }
    public DateTimeOffset? WindowStartedAt { get; set; }
    public DateTimeOffset? WindowEndsAt { get; set; }
    public long? SoftUnits { get; set; }
    public long? LossBudgetUsdNanos { get; set; }
    public Guid? UserId { get; set; }
    public string? Network { get; set; }
    public string? DeviceRiskHash { get; set; }
    public DateTimeOffset ConsumedAt { get; set; }
}
