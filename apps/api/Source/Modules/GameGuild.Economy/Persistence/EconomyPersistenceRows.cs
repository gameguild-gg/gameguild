using GameGuild.Economy.Contracts;
using GameGuild.Economy.Ledger;

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
