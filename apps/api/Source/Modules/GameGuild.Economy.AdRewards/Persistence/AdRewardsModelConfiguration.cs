using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.AdRewards.Persistence;

internal sealed class AdNetworkPolicyVersionRow
{
    public string Network { get; set; } = string.Empty;
    public long Version { get; set; }
    public DateTimeOffset EffectiveAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public AdRewardIssuanceMode IssuanceMode { get; set; }
    public AdNetworkYieldState YieldState { get; set; }
    public long EstimatedNetEcpmUsdNanos { get; set; }
    public int ContractedRevenueSharePpm { get; set; }
    public int SafetyBufferPpm { get; set; }
    public int MinimumVisiblePpm { get; set; }
    public long MaximumFocusLossTicks { get; set; }
    public long MaximumRewardSoftUnits { get; set; }
    public DateTimeOffset ReportsCurrentThrough { get; set; }
    public long ReportStaleAfterTicks { get; set; }
    public int Ranking { get; set; }
}

internal sealed class AdRewardCompletionRow
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public Guid WalletId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public AdRewardCompletionState State { get; set; }
    public long RewardSoftUnits { get; set; }
    public Guid? SourceStampId { get; set; }
    public Guid? PostingId { get; set; }
    public Guid? OutputLotId { get; set; }
    public string? ProviderEventId { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

internal sealed class AdRewardAccumulatorRow
{
    public Guid WalletId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string RemainderNumerator { get; set; } = "0";
    public string CanonicalDenominator { get; set; } = "1";
    public long Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class AdRewardBudgetConsumptionRow
{
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public string DeviceRiskHash { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public long SoftUnits { get; set; }
    public long LossBudgetUsdNanos { get; set; }
    public DateTimeOffset ConsumedAt { get; set; }
}

internal sealed class AdRewardAttributionRow
{
    public Guid SessionId { get; set; }
    public string Network { get; set; } = string.Empty;
    public long PolicyVersion { get; set; }
    public string ProviderBatchId { get; set; } = string.Empty;
    public long EstimatedRevenueUsdNanos { get; set; }
    public long RewardSoftUnits { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}

internal sealed class AdProviderReportRow
{
    public Guid Id { get; set; }
    public string Network { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public DateTimeOffset PeriodStart { get; set; }
    public DateTimeOffset PeriodEnd { get; set; }
    public long ActualRevenueUsdNanos { get; set; }
    public string VerifiedSessionIds { get; set; } = "[]";
    public string EvidenceHash { get; set; } = string.Empty;
    public DateTimeOffset ImportedAt { get; set; }
    public string Signature { get; set; } = string.Empty;
}

internal sealed class AdRewardReconciliationRow
{
    public Guid Id { get; set; }
    public Guid ProviderReportId { get; set; }
    public string Network { get; set; } = string.Empty;
    public string ReportId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string BatchId { get; set; } = string.Empty;
    public long EstimatedRevenueUsdNanos { get; set; }
    public long PreviousActualRevenueUsdNanos { get; set; }
    public long ActualRevenueUsdNanos { get; set; }
    public long ActualDeltaUsdNanos { get; set; }
    public long VarianceUsdNanos { get; set; }
    public long HistoricalRewardSoftUnits { get; set; }
    public DateTimeOffset ReconciledAt { get; set; }
}

public sealed class AdRewardsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<AdNetworkPolicyVersionRow>(builder =>
        {
            builder.ToTable("economy_ad_network_policy_versions", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_network_policy_versions_window", "\"ExpiresAt\" > \"EffectiveAt\"");
                table.HasCheckConstraint("ck_economy_ad_network_policy_versions_ppm", "\"ContractedRevenueSharePpm\" BETWEEN 0 AND 1000000 AND \"SafetyBufferPpm\" BETWEEN 0 AND 999999 AND \"MinimumVisiblePpm\" BETWEEN 0 AND 1000000");
                table.HasCheckConstraint("ck_economy_ad_network_policy_versions_values", "\"Version\" > 0 AND \"EstimatedNetEcpmUsdNanos\" > 0 AND \"MaximumRewardSoftUnits\" > 0 AND \"MaximumFocusLossTicks\" >= 0 AND \"ReportStaleAfterTicks\" > 0 AND \"Ranking\" >= 0");
            });
            builder.HasKey(row => new { row.Network, row.Version });
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.HasIndex(row => new { row.Network, row.EffectiveAt, row.ExpiresAt });
        });

        modelBuilder.Entity<AdRewardCompletionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_completions", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_completions_reward_nonnegative", "\"RewardSoftUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_ad_reward_completions_state", "\"State\" BETWEEN 1 AND 3");
                table.HasCheckConstraint("ck_economy_ad_reward_completions_issued_binding", "\"State\" <> 1 OR (\"RewardSoftUnits\" > 0 AND \"SourceStampId\" IS NOT NULL AND \"PostingId\" IS NOT NULL AND \"OutputLotId\" IS NOT NULL)");
            });
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.ProviderEventId).HasMaxLength(256);
            builder.HasIndex(row => row.IdempotencyKey).IsUnique();
            builder.HasIndex(row => row.ProviderEventId).IsUnique().HasFilter("\"ProviderEventId\" IS NOT NULL");
            builder.HasIndex(row => new { row.UserId, row.CompletedAt });
            builder.HasIndex(row => new { row.Network, row.PolicyVersion });
        });

        modelBuilder.Entity<AdRewardAccumulatorRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_accumulators", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_accumulators_numbers", "\"RemainderNumerator\" ~ '^[0-9]+$' AND \"CanonicalDenominator\" ~ '^[1-9][0-9]*$'");
                table.HasCheckConstraint("ck_economy_ad_reward_accumulators_version", "\"PolicyVersion\" > 0 AND \"Version\" > 0");
            });
            builder.HasKey(row => new { row.WalletId, row.Network });
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.RemainderNumerator).HasColumnType("text");
            builder.Property(row => row.CanonicalDenominator).HasColumnType("text");
        });

        modelBuilder.Entity<AdRewardBudgetConsumptionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_budget_consumptions", table =>
                table.HasCheckConstraint("ck_economy_ad_reward_budget_consumptions_positive", "\"SoftUnits\" > 0 AND \"LossBudgetUsdNanos\" > 0"));
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.DeviceRiskHash).HasMaxLength(128);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.HasIndex(row => new { row.UserId, row.ConsumedAt });
            builder.HasIndex(row => new { row.DeviceRiskHash, row.ConsumedAt });
            builder.HasIndex(row => new { row.Network, row.ConsumedAt });
        });

        modelBuilder.Entity<AdRewardAttributionRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_attributions", table =>
                table.HasCheckConstraint("ck_economy_ad_reward_attributions_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"RewardSoftUnits\" >= 0"));
            builder.HasKey(row => row.SessionId);
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ProviderBatchId).HasMaxLength(256);
            builder.HasIndex(row => new { row.Network, row.ProviderBatchId, row.CompletedAt });
        });

        modelBuilder.Entity<AdProviderReportRow>(builder =>
        {
            builder.ToTable("economy_ad_provider_reports", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_provider_reports_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_ad_provider_reports_window", "\"PeriodEnd\" > \"PeriodStart\" AND \"ImportedAt\" >= \"PeriodEnd\"");
                table.HasCheckConstraint("ck_economy_ad_provider_reports_revenue", "\"ActualRevenueUsdNanos\" >= 0");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ReportId).HasMaxLength(256);
            builder.Property(row => row.BatchId).HasMaxLength(256);
            builder.Property(row => row.VerifiedSessionIds).HasColumnType("jsonb");
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.Property(row => row.Signature).HasColumnType("text");
            builder.HasIndex(row => new { row.Network, row.ReportId, row.Version }).IsUnique();
            builder.HasIndex(row => new { row.Network, row.BatchId, row.Version }).IsUnique();
        });

        modelBuilder.Entity<AdRewardReconciliationRow>(builder =>
        {
            builder.ToTable("economy_ad_reward_reconciliations", table =>
            {
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_version", "\"Version\" > 0");
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_nonnegative", "\"EstimatedRevenueUsdNanos\" >= 0 AND \"PreviousActualRevenueUsdNanos\" >= 0 AND \"ActualRevenueUsdNanos\" >= 0 AND \"HistoricalRewardSoftUnits\" >= 0");
                table.HasCheckConstraint("ck_economy_ad_reward_reconciliations_conservation", "\"ActualDeltaUsdNanos\" = \"ActualRevenueUsdNanos\" - \"PreviousActualRevenueUsdNanos\" AND \"VarianceUsdNanos\" = \"ActualRevenueUsdNanos\" - \"EstimatedRevenueUsdNanos\"");
            });
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.Network).HasMaxLength(100);
            builder.Property(row => row.ReportId).HasMaxLength(256);
            builder.Property(row => row.BatchId).HasMaxLength(256);
            builder.HasIndex(row => new { row.Network, row.ReportId, row.Version }).IsUnique();
            builder.HasOne<AdProviderReportRow>().WithOne().HasForeignKey<AdRewardReconciliationRow>(row => row.ProviderReportId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
