using GameGuild.Economy.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Marketplace.Persistence;

internal sealed class MarketplaceCurrencyPolicyVersionRow
{
    public Guid ProductId { get; set; }
    public long Version { get; set; }
    public Guid SellerId { get; set; }
    public ProductCurrencyMode Mode { get; set; }
    public long HardPriceUnits { get; set; }
    public long SoftPriceUnits { get; set; }
    public int PlatformFeePpm { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
}

internal sealed class MarketplaceSettlementRow
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
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
    public DateTimeOffset RefundHoldUntil { get; set; }
    public DateTimeOffset SettledAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

internal sealed class MarketplaceSettlementLegRow
{
    public Guid SettlementId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long Units { get; set; }
    public long SellerUnits { get; set; }
    public long PlatformFeeUnits { get; set; }
    public long RefundedUnits { get; set; }
}

internal sealed class MarketplaceFundingFragmentRow
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public Guid ParentLotId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long AmountUnits { get; set; }
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
    public Guid RefundHoldId { get; set; }
    public DateTimeOffset RefundHoldUntil { get; set; }
    public string ParentLineage { get; set; } = "[]";
}

internal sealed class MarketplaceRefundRow
{
    public Guid Id { get; set; }
    public Guid SettlementId { get; set; }
    public Guid BuyerId { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public bool IsFullRefund { get; set; }
    public bool EntitlementRevoked { get; set; }
    public long FirstJournalSequence { get; set; }
    public DateTimeOffset RefundedAt { get; set; }
}

internal sealed class MarketplaceRefundLegRow
{
    public Guid RefundId { get; set; }
    public Guid SettlementId { get; set; }
    public CurrencyCode Currency { get; set; }
    public long Units { get; set; }
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
            });
            builder.HasKey(row => new { row.ProductId, row.Version });
            builder.HasIndex(row => new { row.ProductId, row.EffectiveAt });
        });

        modelBuilder.Entity<MarketplaceSettlementRow>(builder =>
        {
            builder.ToTable("economy_marketplace_settlements", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_settlements_state", "\"Status\" BETWEEN 1 AND 3");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_hold", "\"RefundHoldUntil\" > \"SettledAt\"");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_wallets", "\"BuyerWalletId\" <> \"SellerWalletId\" AND \"BuyerWalletId\" <> \"PlatformFeeWalletId\" AND \"SellerWalletId\" <> \"PlatformFeeWalletId\"");
                table.HasCheckConstraint("ck_economy_marketplace_settlements_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.HasIndex(row => row.OrderId).IsUnique();
            builder.HasIndex(row => row.IdempotencyKey).IsUnique();
            builder.HasIndex(row => new { row.BuyerId, row.SettledAt });
            builder.HasIndex(row => new { row.SellerId, row.SettledAt });
        });

        modelBuilder.Entity<MarketplaceSettlementLegRow>(builder =>
        {
            builder.ToTable("economy_marketplace_settlement_legs", table =>
            {
                table.HasCheckConstraint("ck_economy_marketplace_settlement_legs_conservation", "\"Units\" > 0 AND \"SellerUnits\" >= 0 AND \"PlatformFeeUnits\" >= 0 AND \"SellerUnits\" + \"PlatformFeeUnits\" = \"Units\"");
                table.HasCheckConstraint("ck_economy_marketplace_settlement_legs_refund", "\"RefundedUnits\" BETWEEN 0 AND \"Units\"");
            });
            builder.HasKey(row => new { row.SettlementId, row.Currency });
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
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
            builder.HasIndex(row => new { row.SettlementId, row.ParentLotId, row.Currency }).IsUnique();
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
                table.HasCheckConstraint("ck_economy_marketplace_refunds_sequence", "\"FirstJournalSequence\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.HasIndex(row => row.IdempotencyKey).IsUnique();
            builder.HasIndex(row => new { row.SettlementId, row.RefundedAt });
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MarketplaceRefundLegRow>(builder =>
        {
            builder.ToTable("economy_marketplace_refund_legs", table =>
                table.HasCheckConstraint("ck_economy_marketplace_refund_legs_amount", "\"Units\" > 0"));
            builder.HasKey(row => new { row.RefundId, row.Currency });
            builder.HasIndex(row => new { row.SettlementId, row.Currency }).IsUnique();
            builder.HasOne<MarketplaceRefundRow>().WithMany().HasForeignKey(row => row.RefundId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<MarketplaceSettlementRow>().WithMany().HasForeignKey(row => row.SettlementId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
