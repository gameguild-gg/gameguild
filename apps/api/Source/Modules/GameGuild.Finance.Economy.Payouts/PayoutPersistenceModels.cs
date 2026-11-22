using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Finance.Economy.Payouts;

// #1092 Tier 2.4: PayoutOperationRow merged into the shared
// EconomyOperationRow (OperationKind = Payout). The unified registration
// lives in the Economy module; this configuration re-asserts it and
// re-points the operation bindings (provider events, dispatch outbox,
// authorization evidence) at the merged table.

internal sealed class PayoutConnectAccountRow
{
    public Guid PayeeId { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    public string ProviderAccountId { get; set; } = string.Empty;
    public string DestinationHash { get; set; } = string.Empty;
    public ConnectAccountState State { get; set; }
    public bool ChargesEnabled { get; set; }
    public bool PayoutsEnabled { get; set; }
    public long Version { get; set; }
    public DateTimeOffset ObservedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
}

internal sealed class PayoutAuthorizationEvidenceRow
{
    public Guid OperationId { get; set; }
    public PayoutAuthorizationPhase Phase { get; set; }
    public Guid TenantId { get; set; }
    public Guid ActorId { get; set; }
    public Guid RiskDecisionId { get; set; }
    public string ReauthenticationEvidenceHash { get; set; } = string.Empty;
    public string OperationFingerprintHash { get; set; } = string.Empty;
    public Guid CapabilityReceiptId { get; set; }
    public string CapabilityReceiptHash { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class PayoutsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        // #1092 Tier 2.4: economy_payout_operations merged into the shared
        // economy_operations table (OperationKind = 1 = Payout). The module
        // re-asserts the unified table so contexts composing only this
        // configuration address the merged table; the payout kind keeps its
        // idempotency dedupe and state scans through the indexes declared
        // with the unified registration in the Economy module.
        modelBuilder.Entity<EconomyOperationRow>(builder =>
        {
            builder.ToTable("economy_operations");
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.OperationKind, row.TenantId, row.IdempotencyKey })
                .IsUnique()
                .HasDatabaseName("ux_economy_operations_kind_tenant_idempotency");
            builder.HasIndex(row => new { row.TenantId, row.OperationKind, row.State, row.UpdatedAt })
                .HasDatabaseName("ix_economy_operations_tenant_kind_state_updated");
        });

        // #1092 Tier 1.4: economy_payout_provider_events merged into the
        // shared economy_provider_events table (ProviderEventKind = 1 =
        // Payout). The Payouts module keeps contributing the operation
        // binding of its replay-dedupe rows.
        modelBuilder.Entity<EconomyProviderEventRow>(builder =>
        {
            builder.HasOne<EconomyOperationRow>()
                .WithMany()
                .HasForeignKey(row => row.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayoutConnectAccountRow>(builder =>
        {
            builder.ToTable("economy_payout_connect_accounts", table =>
            {
                table.HasCheckConstraint("ck_economy_payout_connect_accounts_state", "\"State\" BETWEEN 1 AND 4");
                table.HasCheckConstraint("ck_economy_payout_connect_accounts_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_payout_connect_accounts_window", "\"ExpiresAt\" > \"ObservedAt\"");
            });
            builder.HasKey(row => row.PayeeId);
            builder.Property(row => row.Provider).HasMaxLength(50);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ProviderAccountId).HasMaxLength(256);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.Provider, row.Environment, row.ProviderAccountId }).IsUnique();
            builder.HasIndex(row => new { row.State, row.ExpiresAt });
        });

        // #1092 Tier 1.1: economy_payout_dispatch_outbox merged into the
        // shared economy_outbox table (OutboxKind = 3 = PayoutDispatch). The
        // Payouts module keeps contributing the operation binding of its
        // queue rows.
        modelBuilder.Entity<EconomyOutboxRow>(builder =>
        {
            builder.HasOne<EconomyOperationRow>()
                .WithMany()
                .HasForeignKey(row => row.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PayoutAuthorizationEvidenceRow>(builder =>
        {
            builder.ToTable("economy_payout_authorization_evidence", table =>
                table.HasCheckConstraint(
                    "ck_economy_payout_authorization_evidence_phase",
                    "\"Phase\" BETWEEN 1 AND 2"));
            builder.HasKey(row => new { row.OperationId, row.Phase });
            builder.Property(row => row.ReauthenticationEvidenceHash).HasMaxLength(128);
            builder.Property(row => row.OperationFingerprintHash).HasMaxLength(128);
            builder.Property(row => row.CapabilityReceiptHash).HasMaxLength(128);
            builder.HasIndex(row => row.CapabilityReceiptId).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.RecordedAt });
            builder.HasOne<EconomyOperationRow>().WithMany().HasForeignKey(row => row.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
