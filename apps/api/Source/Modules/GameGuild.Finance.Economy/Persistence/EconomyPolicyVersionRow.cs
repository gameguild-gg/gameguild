namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified signed dual-control policy-version table
///     (#1092 Tier 2.3). The domain selects the subject key shape: Marketplace
///     policies key on <c>SubjectId</c> (the product), ad-network policies key
///     on <c>SubjectKey</c> (the network name).
/// </summary>
public enum EconomyPolicyDomain
{
    /// <summary>Marketplace currency policy (formerly
    ///     <c>economy_marketplace_currency_policy_versions</c>).</summary>
    MarketplaceCurrency = 1,

    /// <summary>Ad-network reward policy (formerly
    ///     <c>economy_ad_network_policy_versions</c>).</summary>
    AdNetwork = 2,
}

/// <summary>
///     Unified signed dual-control policy version (#1092 Tier 2.3: the two
///     policy-version tables merged behind <see cref="EconomyPolicyDomain"/>).
///     Both domains share the signing spine (CanonicalPayload/PayloadHash/
///     KeyId/Signature) and the dual-control contract (ProposedBy &lt;&gt;
///     ApprovedBy); domain-specific pricing columns are nullable and guarded by
///     per-domain check constraints. Historical-version reads are
///     correctness-critical: a refund must price with the policy that priced
///     the settlement, so the (TenantId, subject, Version) uniqueness is
///     preserved per domain through partial unique indexes.
/// </summary>
public sealed class EconomyPolicyVersionRow
{
    public Guid Id { get; set; }
    public EconomyPolicyDomain PolicyDomain { get; set; }
    public Guid TenantId { get; set; }
    public long Version { get; set; }

    /// <summary>Marketplace product id (PolicyDomain = 1 only).</summary>
    public Guid? SubjectId { get; set; }

    /// <summary>Ad-network name (PolicyDomain = 2 only).</summary>
    public string? SubjectKey { get; set; }

    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }

    // Marketplace currency-policy payload (PolicyDomain = 1 only).
    public int? Mode { get; set; }
    public Guid? SellerId { get; set; }
    public long? HardPriceUnits { get; set; }
    public long? SoftPriceUnits { get; set; }
    public int? PlatformFeePpm { get; set; }
    public Guid? PlatformFeeWalletId { get; set; }
    public long? RefundHoldTicks { get; set; }

    // Ad-network reward-policy payload (PolicyDomain = 2 only).
    public int? IssuanceMode { get; set; }
    public int? YieldState { get; set; }
    public long? EstimatedNetEcpmUsdNanos { get; set; }
    public int? ContractedRevenueSharePpm { get; set; }
    public int? SafetyBufferPpm { get; set; }
    public int? MinimumVisiblePpm { get; set; }
    public long? MaximumFocusLossTicks { get; set; }
    public long? MaximumRewardSoftUnits { get; set; }
    public DateTimeOffset? ReportsCurrentThrough { get; set; }
    public long? ReportStaleAfterTicks { get; set; }
    public int? Ranking { get; set; }
    public long? MaximumUserSoftUnits { get; set; }
    public long? MaximumDeviceSoftUnits { get; set; }
    public long? MaximumIpSoftUnits { get; set; }
    public long? MaximumAsnSoftUnits { get; set; }
    public long? MaximumNetworkSoftUnits { get; set; }
    public long? MaximumGlobalSoftUnits { get; set; }
    public long? FundedLossBudgetUsdNanos { get; set; }
    public long? BudgetWindowTicks { get; set; }
    public string? ProviderHash { get; set; }
    public bool? ProviderCertified { get; set; }

    // Shared signed dual-control spine.
    public string CanonicalPayload { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public Guid ProposedBy { get; set; }
    public Guid ApprovedBy { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}
