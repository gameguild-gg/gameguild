namespace GameGuild.Finance.Economy.Persistence;

/// <summary>
///     Discriminator for the unified Economy operation spine (#1092 Tier 2.4).
/// </summary>
public enum EconomyOperationKind
{
    /// <summary>Payout operation (formerly
    ///     <c>economy_payout_operations</c>).</summary>
    Payout = 1,

    /// <summary>Treasury admin-withdrawal run (formerly
    ///     <c>economy_admin_withdrawal_runs</c>).</summary>
    AdminWithdrawal = 2,
}

/// <summary>
///     Unified Economy operation spine (#1092 Tier 2.4: payout operations and
///     admin-withdrawal runs merged behind <see cref="EconomyOperationKind"/>).
///     The fencing discipline is preserved: each kind keeps its own monotonic
///     token allocator (<c>economy_private.payout_fencing_sequence</c> and
///     <c>economy_private.admin_withdrawal_fencing_sequence</c>), and dispatch
///     still verifies exact version + token + epoch match. Treasury's
///     <c>ExecutionEpoch</c> has no payout twin — the merged table keeps both
///     epoch columns, nullable and guarded by per-kind check constraints.
///     <see cref="State"/> is stored as the raw kind-specific code; stores
///     cast it to <c>PayoutOperationState</c> or
///     <c>AdminWithdrawalRunState</c>.
/// </summary>
public sealed class EconomyOperationRow
{
    public Guid Id { get; set; }
    public EconomyOperationKind OperationKind { get; set; }
    public Guid TenantId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;

    /// <summary>Payout actor / withdrawal requester.</summary>
    public Guid ActorId { get; set; }

    public long AmountUnits { get; set; }
    public string DestinationHash { get; set; } = string.Empty;
    public string? DispatchSnapshotHash { get; set; }
    public int State { get; set; }
    public long Version { get; set; }
    public long FencingToken { get; set; }
    public long ReserveVersion { get; set; }
    public long ReserveAuthorizationEpoch { get; set; }
    public long PolicyVersion { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }

    // Payout-only columns (OperationKind = 1).
    public Guid? PayeeId { get; set; }
    public Guid? WalletId { get; set; }
    public string? ProviderAccountId { get; set; }
    public string? ProviderBindingHash { get; set; }
    public string? EligibilityHash { get; set; }
    public string? ProviderPayoutId { get; set; }
    public long? KillSwitchEpoch { get; set; }

    // Admin-withdrawal-only columns (OperationKind = 2).
    public DateOnly? PeriodStart { get; set; }
    public Guid? ApprovedBy { get; set; }
    public Guid? PlatformFeeWalletId { get; set; }
    public string? SourceAssetKey { get; set; }
    public string? ProviderTransferId { get; set; }
    public long? ExecutionEpoch { get; set; }
}
