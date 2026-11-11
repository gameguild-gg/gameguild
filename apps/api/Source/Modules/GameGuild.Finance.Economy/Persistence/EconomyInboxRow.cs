namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy provider-event inbox
///     (#1092 Tier 1.2).
/// </summary>
public enum EconomyInboxKind
{
    /// <summary>Compliance/KYC provider-event dedupe
    /// (formerly <c>economy_compliance_inbox</c>).</summary>
    Compliance = 1,

    /// <summary>Ad-network provider completion-proof dedupe
    /// (formerly <c>economy_ad_reward_provider_proof_inbox</c>).</summary>
    AdRewardProviderProof = 2,
}

/// <summary>
///     Unified Economy inbox row (#1092 Tier 1.2: the compliance inbox and the
///     ad-reward provider-proof inbox merged behind the
///     <see cref="EconomyInboxKind"/> discriminator). Both dedupe inbound
///     provider events by provider identity and payload hash and track
///     processing outcome through <c>ProcessedAt</c>/<c>ProcessingError</c>.
///     The table stays FK-free on purpose: it must keep accepting events for
///     aggregates that are being rebuilt (#1092 §3.3 guardrail).
/// </summary>
public sealed class EconomyInboxRow
{
    public Guid Id { get; set; }
    public EconomyInboxKind InboxKind { get; set; }
    public Guid? TenantId { get; set; }
    public Guid? SessionId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? Environment { get; set; }
    public string ProviderEventId { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string? EvidenceHash { get; set; }
    public bool? SignatureVerified { get; set; }
    public string? RawObjectReference { get; set; }
    public DateTimeOffset ReceivedAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public string? ProcessingError { get; set; }
}
