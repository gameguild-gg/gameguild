using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Payouts;

internal sealed class PayoutOperationRow
{
    public Guid Id { get; set; }
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public Guid ActorId { get; set; }
    public Guid PayeeId { get; set; }
    public Guid WalletId { get; set; }
    public long AmountUnits { get; set; }
    public string ProviderAccountId { get; set; } = string.Empty;
    public string DestinationHash { get; set; } = string.Empty;
    public string ProviderBindingHash { get; set; } = string.Empty;
    public string EligibilityHash { get; set; } = string.Empty;
    public string? DispatchSnapshotHash { get; set; }
    public string? ProviderPayoutId { get; set; }
    public PayoutOperationState State { get; set; }
    public long Version { get; set; }
    public long FencingToken { get; set; }
    public long KillSwitchEpoch { get; set; }
    public long ReserveVersion { get; set; }
    public long ReserveAuthorizationEpoch { get; set; }
    public long PolicyVersion { get; set; }
    public Guid RiskDecisionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

internal sealed class PayoutProviderEventRow
{
    public string EventId { get; set; } = string.Empty;
    public string EventHash { get; set; } = string.Empty;
    public Guid OperationId { get; set; }
    public PayoutOperationState ResultingState { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

public sealed class PayoutsModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<PayoutOperationRow>(builder =>
        {
            builder.ToTable("economy_payout_operations", table =>
            {
                table.HasCheckConstraint("ck_economy_payout_operations_state", "\"State\" BETWEEN 1 AND 6");
                table.HasCheckConstraint(
                    "ck_economy_payout_operations_positive_values",
                    "\"AmountUnits\" > 0 AND \"Version\" > 0 AND \"FencingToken\" > 0 AND \"KillSwitchEpoch\" > 0 AND \"ReserveVersion\" > 0 AND \"ReserveAuthorizationEpoch\" > 0 AND \"PolicyVersion\" > 0");
                table.HasCheckConstraint(
                    "ck_economy_payout_operations_timestamps",
                    "\"UpdatedAt\" >= \"CreatedAt\"");
                table.HasCheckConstraint(
                    "ck_economy_payout_operations_dispatch",
                    "(\"State\" = 1 AND \"DispatchSnapshotHash\" IS NULL) OR (\"State\" BETWEEN 2 AND 6 AND \"DispatchSnapshotHash\" IS NOT NULL)");
            });

            builder.HasKey(row => row.Id);
            builder.Property(row => row.Id).ValueGeneratedNever();
            builder.Property(row => row.IdempotencyKey).HasMaxLength(256);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.Property(row => row.ProviderAccountId).HasMaxLength(256);
            builder.Property(row => row.DestinationHash).HasMaxLength(128);
            builder.Property(row => row.ProviderBindingHash).HasMaxLength(128);
            builder.Property(row => row.EligibilityHash).HasMaxLength(128);
            builder.Property(row => row.DispatchSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.ProviderPayoutId).HasMaxLength(256);
            builder.Property(row => row.Version).IsConcurrencyToken();
            builder.HasIndex(row => row.IdempotencyKey).IsUnique()
                .HasDatabaseName("ux_economy_payout_operations_idempotency");
            builder.HasIndex(row => new { row.State, row.UpdatedAt })
                .HasDatabaseName("ix_economy_payout_operations_state_updated");
        });

        modelBuilder.Entity<PayoutProviderEventRow>(builder =>
        {
            builder.ToTable("economy_payout_provider_events");
            builder.HasKey(row => row.EventId);
            builder.Property(row => row.EventId).HasMaxLength(256);
            builder.Property(row => row.EventHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.OperationId, row.RecordedAt })
                .HasDatabaseName("ix_economy_payout_provider_events_operation_recorded");
            builder.HasOne<PayoutOperationRow>()
                .WithMany()
                .HasForeignKey(row => row.OperationId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
