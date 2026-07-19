using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;
using GameGuild.Economy.Policy;
using GameGuild.Economy.Reserves;
using GameGuild.Economy.Risk;

namespace GameGuild.Economy.Persistence;

internal sealed class EconomyWalletRow
{
    public Guid Id { get; set; }
    public Guid OwnerId { get; set; }
    public Guid TenantId { get; set; }
    public WalletLifecycleState State { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class EconomyAccountRow
{
    public Guid Id { get; set; }
    public Guid? WalletId { get; set; }
    public EconomyAccountCode Code { get; set; }
    public CurrencyCode Currency { get; set; }
    public ProvenanceKind? Provenance { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class EconomySourceStampRow
{
    public Guid Id { get; set; }
    public string SourceKind { get; set; } = string.Empty;
    public string InternalSourceId { get; set; } = string.Empty;
    public string SourceLegId { get; set; } = string.Empty;
    public string? Provider { get; set; }
    public string? ProviderReference { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public ProvenanceKind Provenance { get; set; }
    public SourceConfirmationState State { get; set; }
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? PostingReferenceId { get; set; }
    public long PolicyVersion { get; set; }
    public long AuthoritativeUnits { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

internal sealed class EconomySourceStampEventRow
{
    public Guid Id { get; set; }
    public Guid SourceStampId { get; set; }
    public long Sequence { get; set; }
    public SourceConfirmationState State { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class EconomyPostingGroupRow
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public PostingTemplateKind TemplateKind { get; set; }
    public int TemplateVersion { get; set; }
    public PostingAuthority Authority { get; set; }
    public PostingStatus Status { get; set; }
    public Guid CapabilityId { get; set; }
    public Guid ActorId { get; set; }
    public Guid TenantId { get; set; }
    public Guid? RiskDecisionId { get; set; }
    public long PolicyVersion { get; set; }
    public long ReserveVersion { get; set; }
    public long ReserveAuthorizationEpoch { get; set; }
    public Guid? SourceStampId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

internal sealed class EconomyJournalEntryRow
{
    public Guid Id { get; set; }
    public Guid PostingGroupId { get; set; }
    public long Sequence { get; set; }
    public string PreviousHash { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
}

internal sealed class EconomyJournalLineRow
{
    public Guid Id { get; set; }
    public Guid JournalEntryId { get; set; }
    public Guid AccountId { get; set; }
    public Guid? WalletId { get; set; }
    public Guid? CreditLotId { get; set; }
    public int Sequence { get; set; }
    public EntrySide Side { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public ProvenanceKind? Provenance { get; set; }
}

internal sealed class EconomyCreditLotRow
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid RootSourceStampId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public ProvenanceKind Provenance { get; set; }
    public DateTimeOffset CreditedAt { get; set; }
    public DateTimeOffset ConfirmedAt { get; set; }
    public DateTimeOffset OriginalMaturesAt { get; set; }
    public bool CashOutEligible { get; set; }
    public long JournalSequence { get; set; }
    public CreditLotState State { get; set; }
    public long ReversalEpoch { get; set; }
}

internal sealed class EconomyEntryAllocationRow
{
    public Guid Id { get; set; }
    public Guid JournalLineId { get; set; }
    public Guid ParentLotId { get; set; }
    public long AmountUnits { get; set; }
}

internal sealed class EconomyLotLineageEdgeRow
{
    public Guid Id { get; set; }
    public Guid ParentLotId { get; set; }
    public Guid ChildLotId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
}

internal sealed class EconomyFragmentRootRangeRow
{
    public Guid Id { get; set; }
    public Guid RootSourceStampId { get; set; }
    public Guid? CreditLotId { get; set; }
    public Guid? EntryAllocationId { get; set; }
    public long StartInclusive { get; set; }
    public long EndExclusive { get; set; }
    public long ReversalEpoch { get; set; }
}

internal sealed class EconomyRootReversalStateRow
{
    public Guid RootSourceStampId { get; set; }
    public long Epoch { get; set; }
    public long CumulativeProviderUnits { get; set; }
    public long ReversedUnits { get; set; }
    public string State { get; set; } = string.Empty;
    public string TargetedRanges { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class EconomyProviderFactAllocationRow
{
    public Guid Id { get; set; }
    public Guid SourceStampId { get; set; }
    public Guid JournalLineId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ConnectedAccount { get; set; } = string.Empty;
    public string ProviderObject { get; set; } = string.Empty;
    public string ProviderMonetaryLeg { get; set; } = string.Empty;
    public CurrencyCode Currency { get; set; }
    public long AllocatedUnits { get; set; }
    public long CumulativeCreditedUnits { get; set; }
    public long AuthoritativeUnits { get; set; }
}

internal sealed class EconomyDispatchSnapshotRow
{
    public Guid Id { get; set; }
    public Guid PostingGroupId { get; set; }
    public string SnapshotHash { get; set; } = string.Empty;
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public string Destination { get; set; } = string.Empty;
    public string EligibilityPayload { get; set; } = string.Empty;
    public long ChainSequence { get; set; }
    public string ChainHash { get; set; } = string.Empty;
    public long ReserveVersion { get; set; }
    public long ReserveAuthorizationEpoch { get; set; }
    public long KillSwitchEpoch { get; set; }
    public long FencingToken { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class EconomyOutboxMessageRow
{
    public Guid Id { get; set; }
    public Guid PostingGroupId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class EconomyIdempotencyRecordRow
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid PostingGroupId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

internal sealed class EconomyChainHeadRow
{
    public short Id { get; set; }
    public long Sequence { get; set; }
    public string Hash { get; set; } = string.Empty;
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class EconomyExternalAnchorRow
{
    public Guid Id { get; set; }
    public long JournalSequence { get; set; }
    public string JournalHash { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public string WormReference { get; set; } = string.Empty;
    public string? DispatchSnapshotHash { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string ProviderReference { get; set; } = string.Empty;
    public DateTimeOffset AnchoredAt { get; set; }
}

internal sealed class EconomyReserveHeadRow
{
    public long Version { get; set; }
    public bool IsActive { get; set; }
    public long PolicyVersion { get; set; }
    public long AuthorizationEpoch { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public long HardFaceValueUsdMinor { get; set; }
    public long RequiredHardReserveUsdMinor { get; set; }
    public long SoftFaceValueUsdNanos { get; set; }
    public long StressedExpectedRedemptionCostUsdNanos { get; set; }
    public long RequiredSoftReserveUsdNanos { get; set; }
    public long HardBackingUsdNanos { get; set; }
    public long SoftBackingUsdNanos { get; set; }
    public ReserveCoverageState Coverage { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset ActivatedAt { get; set; }
}

internal sealed class EconomyReserveAssetAllocationRow
{
    public Guid Id { get; set; }
    public long ReserveVersion { get; set; }
    public string AssetKey { get; set; } = string.Empty;
    public ReserveBackingPurpose Purpose { get; set; }
    public long EligibleUsdNanos { get; set; }
}

internal sealed class EconomyRiskDecisionRow
{
    public Guid Id { get; set; }
    public RiskOutcome Outcome { get; set; }
    public string OperationFingerprint { get; set; } = string.Empty;
    public string ActorHash { get; set; } = string.Empty;
    public PostingTemplateKind TemplateKind { get; set; }
    public Guid SourceWalletId { get; set; }
    public Guid DestinationWalletId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public string CurrencyLegs { get; set; } = string.Empty;
    public string SourceRoots { get; set; } = string.Empty;
    public string ProviderReferenceHash { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public long ReserveVersion { get; set; }
    public long ReserveAuthorizationEpoch { get; set; }
    public long FeatureVersion { get; set; }
    public long KillSwitchEpoch { get; set; }
    public long CounterVersion { get; set; }
    public long EntityGraphVersion { get; set; }
    public string EntityGraphEvidenceHash { get; set; } = string.Empty;
    public string ReasonCodes { get; set; } = string.Empty;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}

internal sealed class EconomyRegisteredCapabilityRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AllowedTemplateKinds { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

internal sealed class EconomyRiskDecisionConsumptionRow
{
    public Guid Id { get; set; }
    public Guid RiskDecisionId { get; set; }
    public Guid PostingGroupId { get; set; }
    public string OperationFingerprint { get; set; } = string.Empty;
    public DateTimeOffset ConsumedAt { get; set; }
}

internal sealed class EconomyRiskCounterRow
{
    public Guid Id { get; set; }
    public RiskLimitDimension Dimension { get; set; }
    public string SubjectHash { get; set; } = string.Empty;
    public PostingTemplateKind Operation { get; set; }
    public CurrencyCode Currency { get; set; }
    public DateTimeOffset WindowStartedAt { get; set; }
    public DateTimeOffset WindowEndsAt { get; set; }
    public long CounterVersion { get; set; }
    public long MaxUnits { get; set; }
    public long UsedUnits { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class EconomyRiskCounterReservationRow
{
    public Guid Id { get; set; }
    public Guid RiskDecisionId { get; set; }
    public Guid RiskCounterId { get; set; }
    public long AmountUnits { get; set; }
    public DateTimeOffset ReservedAt { get; set; }
}

internal sealed class EconomyProtectedChangeCooldownRow
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public ProtectedChangeKind Kind { get; set; }
    public string ValueHash { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
}

internal sealed class EconomyHoldRow
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public HoldReason Reason { get; set; }
    public HoldStatus Status { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset? ReleasedAt { get; set; }
}

internal sealed class EconomyHoldEventRow
{
    public Guid Id { get; set; }
    public Guid HoldId { get; set; }
    public long Sequence { get; set; }
    public HoldEventKind Kind { get; set; }
    public Guid ActorId { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class EconomyRiskReviewCaseRow
{
    public Guid Id { get; set; }
    public Guid RiskDecisionId { get; set; }
    public Guid SubmittedBy { get; set; }
    public RiskReviewStatus Status { get; set; }
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
    public Guid? ResolvedBy { get; set; }
    public string? Resolution { get; set; }
    public int RequiredApprovals { get; set; }
    public Guid? AppealOf { get; set; }
}

internal sealed class EconomyRiskReviewEventRow
{
    public Guid Id { get; set; }
    public Guid RiskReviewCaseId { get; set; }
    public long Sequence { get; set; }
    public RiskReviewEventKind Kind { get; set; }
    public Guid ActorId { get; set; }
    public string EvidenceHashes { get; set; } = string.Empty;
    public string? Resolution { get; set; }
    public RiskManualDecisionCode? DecisionCode { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
}

internal sealed class EconomyRiskAuditEvidenceRow
{
    public Guid Id { get; set; }
    public Guid RiskDecisionId { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string OperationFingerprint { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
}
