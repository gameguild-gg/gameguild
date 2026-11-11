using GameGuild.Finance.Economy.Contracts;
using GameGuild.Finance.Economy.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Finance.Economy.Marketplace.Persistence;

internal sealed class MarketplaceCurrencyPolicyVersionRow
{
    public Guid TenantId { get; set; }
    public Guid ProductId { get; set; }
    public long Version { get; set; }
    public Guid SellerId { get; set; }
    public ProductCurrencyMode Mode { get; set; }
    public long HardPriceUnits { get; set; }
    public long SoftPriceUnits { get; set; }
    public int PlatformFeePpm { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public Guid PlatformFeeWalletId { get; set; }
    public long RefundHoldTicks { get; set; }
    public string CanonicalPayload { get; set; } = string.Empty;
    public string PayloadHash { get; set; } = string.Empty;
    public string KeyId { get; set; } = string.Empty;
    public string Signature { get; set; } = string.Empty;
    public Guid ProposedBy { get; set; }
    public Guid ApprovedBy { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
}

internal sealed class MarketplaceSettlementRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid OrderLineItemId { get; set; }
    public Guid ProductId { get; set; }
    public Guid ProductPricingVersionId { get; set; }
    public int PriceVersionSnapshot { get; set; }
    public int Quantity { get; set; }
    public int RefundedQuantity { get; set; }
    public decimal UnitPriceSnapshot { get; set; }
    public string FiatCurrencySnapshot { get; set; } = string.Empty;
    public string OrderSnapshotHash { get; set; } = string.Empty;
    public Guid BuyerId { get; set; }
    public Guid BuyerWalletId { get; set; }
    public Guid SellerId { get; set; }
    public Guid SellerWalletId { get; set; }
    public Guid PlatformFeeWalletId { get; set; }
    public long PolicyVersion { get; set; }
    public ProductCurrencyMode CurrencyMode { get; set; }
    public MarketplaceSettlementStatus Status { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public Guid EntitlementId { get; set; }
    public MarketplaceEntitlementStatus EntitlementStatus { get; set; }
    public Guid PostingId { get; set; }
    public long JournalSequence { get; set; }
    public string JournalHash { get; set; } = string.Empty;
    public Guid CapabilityReceiptId { get; set; }
    public string CapabilityReceiptHash { get; set; } = string.Empty;
    public long ReserveVersion { get; set; }
    public Guid RiskDecisionId { get; set; }
    public long KillSwitchEpoch { get; set; }
    public string JurisdictionCode { get; set; } = string.Empty;
    public string EvidenceHashes { get; set; } = "[]";
    public DateTimeOffset RefundHoldUntil { get; set; }
    public DateTimeOffset SettledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

/// <summary>
///     Discriminator for the unified marketplace value legs
///     (#1092 Tier 1.6). <c>RefundedUnits</c> is settlement-side and stays
///     queryable per (SettlementId, Currency) through the kind-1 partial
///     unique index — the refund math reads those counters.
/// </summary>
internal enum MarketplaceValueLegKind
{
    /// <summary>Settlement legs (formerly <c>economy_marketplace_settlement_legs</c>).</summary>
    Settlement = 1,

    /// <summary>Refund legs (formerly <c>economy_marketplace_refund_legs</c>).</summary>
    Refund = 2,
}

internal sealed class MarketplaceValueLegRow
{
    public Guid Id { get; set; }
    public MarketplaceValueLegKind LegKind { get; set; }
    public Guid SettlementId { get; set; }
    public Guid? RefundId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long Units { get; set; }
    public long? SellerUnits { get; set; }
    public long? PlatformFeeUnits { get; set; }
    public long? RefundedUnits { get; set; }
}

internal sealed class MarketplaceFundingFragmentRow
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public Guid ParentLotId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public Guid ReservationId { get; set; }
    public long TraceUnitsPerCoinUnit { get; set; }
    public string SelectedRootRanges { get; set; } = "[]";
}

internal sealed class MarketplaceSettlementCreditRow
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public MarketplaceCreditPurpose Purpose { get; set; }
    public Guid WalletId { get; set; }
    public Guid CreditLotId { get; set; }
    public Guid? SourceStampId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public long RemainingUnits { get; set; }
    public Guid RefundHoldId { get; set; }
    public DateTimeOffset RefundHoldUntil { get; set; }
    public string ParentLineage { get; set; } = "[]";
}

internal sealed class MarketplaceRefundRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SettlementId { get; set; }
    public Guid BuyerId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsFullRefund { get; set; }
    public bool EntitlementRevoked { get; set; }
    public long FirstJournalSequence { get; set; }
    public Guid PostingId { get; set; }
    public string JournalHash { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string ReasonHash { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int RefundedQuantity { get; set; }
    public long MarketplacePolicyVersion { get; set; }
    public long PolicyVersion { get; set; }
    public Guid CapabilityReceiptId { get; set; }
    public string CapabilityReceiptHash { get; set; } = string.Empty;
    public long ReserveVersion { get; set; }
    public Guid RiskDecisionId { get; set; }
    public long KillSwitchEpoch { get; set; }
    public string JurisdictionCode { get; set; } = string.Empty;
    public string EvidenceHashes { get; set; } = "[]";
    public DateTimeOffset RefundedAt { get; set; }
}

internal sealed class MarketplaceRefundDebtRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid RefundId { get; set; }
    public Guid SettlementId { get; set; }
    public Guid ResponsibleWalletId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset RecordedAt { get; set; }
}

internal sealed class MarketplaceEventRow
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid SettlementId { get; set; }
    public long Sequence { get; set; }
    public string EventKind { get; set; } = string.Empty;
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset OccurredAt { get; set; }
}


public sealed class MarketplaceModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<MarketplaceCurrencyPolicyVersionRow>(builder =>
        {
            builder.ToTable("economy_marketplace_currency_policy_versions", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_currency_policy_versions_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_currency_policy_versions_fee", "\"PlatformFeePpm\" BETWEEN 0 AND 999999");
                table.HasCheckConstraint("ck_economy_marketplace_currency_policy_versions_prices", "(\"Mode\" = 1 AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" = 0) OR (\"Mode\" = 2 AND \"HardPriceUnits\" = 0 AND \"SoftPriceUnits\" > 0) OR (\"Mode\" IN (3, 4) AND \"HardPriceUnits\" > 0 AND \"SoftPriceUnits\" > 0)");
                table.HasCheckConstraint("ck_economy_marketplace_currency_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\" AND \"RefundHoldTicks\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_currency_policy_versions_dual_control", "\"ProposedBy\" <> \"ApprovedBy\"");
            });
            builder.HasKey(row => new { row.TenantId, row.ProductId, row.Version });
            builder.Property(row => row.CanonicalPayload).HasColumnType("text");
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.Property(row => row.KeyId).HasMaxLength(256);
            builder.Property(row => row.Signature).HasColumnType("text");
            builder.HasIndex(row => new { row.TenantId, row.ProductId, row.EffectiveAt, row.ExpiresAt });
        });

        modelBuilder.Entity<MarketplaceSettlementRow>(builder =>
        {
            builder.ToTable("economy_marketplace_settlements", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_settlements_state", "\"Status\" BETWEEN 1 AND 3");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_hold", "\"RefundHoldUntil\" > \"SettledAt\"");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_wallets", "\"BuyerWalletId\" <> \"SellerWalletId\" AND \"BuyerWalletId\" <> \"PlatformFeeWalletId\" AND \"SellerWalletId\" <> \"PlatformFeeWalletId\"");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_order_snapshot", "\"Quantity\" > 0 AND \"RefundedQuantity\" BETWEEN 0 AND \"Quantity\" AND \"UnitPriceSnapshot\" >= 0 AND \"PriceVersionSnapshot\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_receipt", "\"ReserveVersion\" > 0 AND \"JournalSequence\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.FiatCurrencySnapshot).HasMaxLength(3);
            builder.Property(row => row.OrderSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.CapabilityReceiptHash).HasMaxLength(128);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.EvidenceHashes).HasColumnType("jsonb");
            builder.HasIndex(row => new { row.TenantId, row.OrderId }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.IdempotencyKey }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.BuyerId, row.SettledAt });
            builder.HasIndex(row => new { row.TenantId, row.SellerId, row.SettledAt });
        });

        // #1092 Tier 1.6 — settlement legs and refund legs merged behind
        // LegKind. RefundedUnits is settlement-side and stays queryable per
        // (SettlementId, Currency) through the kind-1 partial unique index;
        // the refund math (DurableMarketplaceRefundService) reads those
        // counters and the writer function updates them with a LegKind = 1
        // filter.
        modelBuilder.Entity<MarketplaceValueLegRow>(builder =>
        {
            builder.ToTable("economy_marketplace_value_legs", table =>
            {
                table.HasCheckConstraint(
                    "ck_economy_marketplace_value_legs_conservation",
                    "(\"LegKind\" = 1 AND \"Units\" > 0 AND \"SellerUnits\" >= 0 AND \"PlatformFeeUnits\" >= 0 AND \"SellerUnits\" + \"PlatformFeeUnits\" = \"Units\" AND \"RefundedUnits\" BETWEEN 0 AND \"Units\" AND \"RefundId\" IS NULL) OR " +
                    "(\"LegKind\" = 2 AND \"Units\" > 0 AND \"SellerUnits\" IS NULL AND \"PlatformFeeUnits\" IS NULL AND \"RefundedUnits\" IS NULL AND \"RefundId\" IS NOT NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).HasDefaultValueSql("gen_random_uuid()");
            builder.HasIndex(row => new { row.SettlementId, row.Currency })
                .IsUnique()
                .HasFilter("\"LegKind\" = 1")
                .HasDatabaseName("ux_economy_marketplace_value_legs_settlement_currency");
            builder.HasIndex(row => new { row.RefundId, row.Currency })
                .IsUnique()
                .HasFilter("\"LegKind\" = 2")
                .HasDatabaseName("ux_economy_marketplace_value_legs_refund_currency");
            builder.HasIndex(row => new { row.SettlementId, row.Currency })
                .HasFilter("\"LegKind\" = 2")
                .HasDatabaseName("ix_economy_marketplace_value_legs_refund_settlement");
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<MarketplaceRefundRow>().WithMany().HasForeignKey(row => row.RefundId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceFundingFragmentRow>(builder =>
        {
            builder.ToTable("economy_marketplace_funding_fragments", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_funding_fragments_amount", "\"AmountUnits\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_funding_fragments_scale", "\"TraceUnitsPerCoinUnit\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.SelectedRootRanges).HasColumnType("jsonb");
            builder.HasIndex(row => row.ReservationId).IsUnique();
            builder.HasIndex(row => new { row.SettlementId, row.ParentLotId, row.Currency });
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceSettlementCreditRow>(builder =>
        {
            builder.ToTable("economy_marketplace_settlement_credits", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_settlement_credits_amount", "\"AmountUnits\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_settlement_credits_purpose", "\"Purpose\" BETWEEN 1 AND 2");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.ParentLineage).HasColumnType("jsonb");
            builder.HasIndex(row => row.CreditLotId).IsUnique();
            builder.HasIndex(row => row.SourceStampId).IsUnique().HasFilter("\"SourceStampId\" IS NOT NULL");
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceRefundRow>(builder =>
        {
            builder.ToTable("economy_marketplace_refunds", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_refunds_sequence", "\"FirstJournalSequence\" > 0");
                table.HasCheckConstraint("ck_economy_marketplace_refunds_quantity", "\"Quantity\" > 0 AND \"RefundedQuantity\" >= \"Quantity\"");
                table.HasCheckConstraint("ck_economy_marketplace_refunds_versions", "\"MarketplacePolicyVersion\" > 0 AND \"PolicyVersion\" > 0 AND \"ReserveVersion\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.ReasonCode).HasMaxLength(100);
            builder.Property(row => row.ReasonHash).HasMaxLength(128);
            builder.Property(row => row.CapabilityReceiptHash).HasMaxLength(128);
            builder.Property(row => row.JurisdictionCode).HasMaxLength(16);
            builder.Property(row => row.EvidenceHashes).HasColumnType("jsonb");
            builder.HasIndex(row => new { row.TenantId, row.IdempotencyKey }).IsUnique();
            builder.HasIndex(row => new { row.TenantId, row.SettlementId, row.RefundedAt });
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        // economy_marketplace_refund_legs merged into economy_marketplace_value_legs
        // (#1092 Tier 1.6) — see the MarketplaceValueLegRow configuration above.

        modelBuilder.Entity<MarketplaceRefundDebtRow>(builder =>
        {
            builder.ToTable("economy_marketplace_refund_debts", table =>
                table.HasCheckConstraint("ck_economy_marketplace_refund_debts_amount", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.TenantId, row.ResponsibleWalletId, row.RecordedAt });
            builder.HasOne<MarketplaceRefundRow>().WithMany().HasForeignKey(row => row.RefundId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceEventRow>(builder =>
        {
            builder.ToTable("economy_marketplace_events", table =>
                table.HasCheckConstraint("ck_economy_marketplace_events_sequence", "\"Sequence\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EventKind).HasMaxLength(100);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.SettlementId, row.Sequence }).IsUnique();
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        // #1092 Tier 1.1: economy_marketplace_outbox merged into the shared
        // economy_outbox table (OutboxKind = 2). The Marketplace module keeps
        // contributing the settlement binding of its queue rows.
        modelBuilder.Entity<EconomyOutboxRow>(builder =>
        {
            builder.HasOne<MarketplaceSettlementRow>()
                .WithMany()
                .HasForeignKey(row => row.SettlementId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
