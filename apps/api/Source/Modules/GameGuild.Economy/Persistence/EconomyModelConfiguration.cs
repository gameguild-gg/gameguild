using Microsoft.EntityFrameworkCore;

namespace GameGuild.Economy.Persistence;

public sealed class EconomyModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        ConfigureWallets(modelBuilder);
        ConfigureSources(modelBuilder);
        ConfigurePostings(modelBuilder);
        ConfigureLedger(modelBuilder);
        ConfigureLineage(modelBuilder);
        ConfigureOperations(modelBuilder);
        ConfigureChain(modelBuilder);
    }

    private static void ConfigureWallets(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyWalletRow>(builder =>
        {
            builder.ToTable("economy_wallets");
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.TenantId, row.OwnerId }).IsUnique();
        });

        modelBuilder.Entity<EconomyAccountRow>(builder =>
        {
            builder.ToTable("economy_accounts", table =>
                table.HasCheckConstraint(
                    "ck_economy_accounts_wallet_partition",
                    "(\"WalletId\" IS NULL AND \"Code\" NOT IN (2, 3, 4)) OR (\"WalletId\" IS NOT NULL AND \"Code\" IN (2, 3, 4))"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.WalletId, row.Code, row.Currency, row.Provenance }).IsUnique();
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSources(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomySourceStampRow>(builder =>
        {
            builder.ToTable("economy_source_stamps");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SourceKind).HasMaxLength(100);
            builder.Property(row => row.InternalSourceId).HasMaxLength(256);
            builder.Property(row => row.SourceLegId).HasMaxLength(256);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.ProviderReference).HasMaxLength(256);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.SourceKind, row.InternalSourceId, row.SourceLegId })
                .IsUnique()
                .HasDatabaseName("ux_economy_source_stamps_internal_leg");
            builder.HasIndex(row => new { row.Provider, row.ProviderReference })
                .IsUnique()
                .HasFilter("\"Provider\" IS NOT NULL AND \"ProviderReference\" IS NOT NULL")
                .HasDatabaseName("ux_economy_source_stamps_provider_reference");
        });

        modelBuilder.Entity<EconomySourceStampEventRow>(builder =>
        {
            builder.ToTable("economy_source_stamp_events", table =>
                table.HasCheckConstraint("ck_economy_source_stamp_events_sequence_positive", "\"Sequence\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.EvidenceHash).HasMaxLength(128);
            builder.HasIndex(row => new { row.SourceStampId, row.Sequence })
                .IsUnique()
                .HasDatabaseName("ux_economy_source_stamp_events_source_sequence");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigurePostings(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyPostingGroupRow>(builder =>
        {
            builder.ToTable("economy_posting_groups");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.IdempotencyKey).HasMaxLength(128);
            builder.HasIndex(row => row.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("ux_economy_posting_groups_idempotency_key");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyJournalEntryRow>(builder =>
        {
            builder.ToTable("economy_journal_entries");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.PreviousHash).HasMaxLength(128);
            builder.Property(row => row.Hash).HasMaxLength(128);
            builder.HasIndex(row => row.PostingGroupId)
                .IsUnique()
                .HasDatabaseName("ux_economy_journal_entries_posting_group_id");
            builder.HasIndex(row => row.Sequence)
                .IsUnique()
                .HasDatabaseName("ux_economy_journal_entries_sequence");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyJournalLineRow>(builder =>
        {
            builder.ToTable("economy_journal_lines", table =>
                table.HasCheckConstraint("ck_economy_journal_lines_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.JournalEntryId, row.Sequence }).IsUnique();
            builder.HasOne<EconomyJournalEntryRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalEntryId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyAccountRow>()
                .WithMany()
                .HasForeignKey(row => row.AccountId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.CreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLedger(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyCreditLotRow>(builder =>
        {
            builder.ToTable("economy_credit_lots", table =>
            {
                table.HasCheckConstraint("ck_economy_credit_lots_amount_positive", "\"AmountUnits\" > 0");
                table.HasCheckConstraint("ck_economy_credit_lots_maturity_order", "\"OriginalMaturesAt\" >= \"ConfirmedAt\"");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.RootSourceStampId)
                .IsUnique()
                .HasDatabaseName("ux_economy_credit_lots_root_source");
            builder.HasOne<EconomyWalletRow>()
                .WithMany()
                .HasForeignKey(row => row.WalletId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyEntryAllocationRow>(builder =>
        {
            builder.ToTable("economy_entry_allocations", table =>
                table.HasCheckConstraint("ck_economy_entry_allocations_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.ParentLotId).HasDatabaseName("ix_economy_entry_allocations_parent_lot");
            builder.HasIndex(row => new { row.JournalLineId, row.ParentLotId })
                .IsUnique()
                .HasDatabaseName("ux_economy_entry_allocations_line_parent");
            builder.HasOne<EconomyJournalLineRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalLineId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ParentLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyProviderFactAllocationRow>(builder =>
        {
            builder.ToTable("economy_provider_fact_allocations", table =>
                table.HasCheckConstraint(
                    "ck_economy_provider_fact_allocations_cumulative_bounds",
                    "\"AllocatedUnits\" > 0 AND \"CumulativeCreditedUnits\" >= \"AllocatedUnits\" AND \"CumulativeCreditedUnits\" <= \"AuthoritativeUnits\""));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.Environment).HasMaxLength(50);
            builder.Property(row => row.ConnectedAccount).HasMaxLength(256);
            builder.Property(row => row.ProviderObject).HasMaxLength(256);
            builder.Property(row => row.ProviderMonetaryLeg).HasMaxLength(256);
            builder.HasIndex(row => new
                {
                    row.Provider,
                    row.Environment,
                    row.ConnectedAccount,
                    row.ProviderObject,
                    row.ProviderMonetaryLeg
                })
                .IsUnique()
                .HasDatabaseName("ux_economy_provider_fact_allocations_provider_leg");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyJournalLineRow>()
                .WithMany()
                .HasForeignKey(row => row.JournalLineId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureLineage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyLotLineageEdgeRow>(builder =>
        {
            builder.ToTable("economy_lot_lineage_edges", table =>
                table.HasCheckConstraint("ck_economy_lot_lineage_edges_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => row.ParentLotId).HasDatabaseName("ix_economy_lot_lineage_edges_parent_lot");
            builder.HasIndex(row => new { row.ParentLotId, row.ChildLotId })
                .IsUnique()
                .HasDatabaseName("ux_economy_lot_lineage_edges_parent_child");
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ParentLotId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.ChildLotId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyFragmentRootRangeRow>(builder =>
        {
            builder.ToTable("economy_fragment_root_ranges", table =>
            {
                table.HasCheckConstraint("ck_economy_fragment_root_ranges_half_open", "\"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\"");
                table.HasCheckConstraint("ck_economy_fragment_root_ranges_single_owner", "(\"CreditLotId\" IS NULL) <> (\"EntryAllocationId\" IS NULL)");
            });
            builder.HasKey(row => row.Id);
            builder.HasIndex(row => new { row.RootSourceStampId, row.ReversalEpoch })
                .HasDatabaseName("ix_economy_fragment_root_ranges_root_epoch");
            builder.HasIndex(row => new { row.RootSourceStampId, row.ReversalEpoch, row.StartInclusive, row.EndExclusive })
                .IsUnique()
                .HasDatabaseName("ux_economy_fragment_root_ranges_owner_interval");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyCreditLotRow>()
                .WithMany()
                .HasForeignKey(row => row.CreditLotId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasOne<EconomyEntryAllocationRow>()
                .WithMany()
                .HasForeignKey(row => row.EntryAllocationId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyRootReversalStateRow>(builder =>
        {
            builder.ToTable("economy_root_reversal_states", table =>
            {
                table.HasCheckConstraint("ck_economy_root_reversal_states_epoch_nonnegative", "\"Epoch\" >= 0");
                table.HasCheckConstraint(
                    "ck_economy_root_reversal_states_cumulative_bounds",
                    "\"CumulativeProviderUnits\" >= 0 AND \"ReversedUnits\" >= 0 AND \"ReversedUnits\" <= \"CumulativeProviderUnits\"");
            });
            builder.HasKey(row => row.RootSourceStampId);
            builder.HasIndex(row => new { row.RootSourceStampId, row.Epoch })
                .IsUnique()
                .HasDatabaseName("ux_economy_root_reversal_states_root_epoch");
            builder.Property(row => row.State).HasMaxLength(50);
            builder.Property(row => row.TargetedRanges).HasColumnType("jsonb");
            builder.HasOne<EconomySourceStampRow>()
                .WithMany()
                .HasForeignKey(row => row.RootSourceStampId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureOperations(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyDispatchSnapshotRow>(builder =>
        {
            builder.ToTable("economy_dispatch_snapshots", table =>
                table.HasCheckConstraint("ck_economy_dispatch_snapshots_amount_positive", "\"AmountUnits\" > 0"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.SnapshotHash).HasMaxLength(128);
            builder.Property(row => row.Destination).HasMaxLength(512);
            builder.Property(row => row.EligibilityPayload).HasColumnType("jsonb");
            builder.Property(row => row.ChainHash).HasMaxLength(128);
            builder.HasIndex(row => row.SnapshotHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_dispatch_snapshots_hash");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyOutboxMessageRow>(builder =>
        {
            builder.ToTable("economy_outbox_messages");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Type).HasMaxLength(200);
            builder.Property(row => row.PayloadHash).HasMaxLength(128);
            builder.HasIndex(row => row.PayloadHash)
                .IsUnique()
                .HasDatabaseName("ux_economy_outbox_messages_payload_hash");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<EconomyIdempotencyRecordRow>(builder =>
        {
            builder.ToTable("economy_idempotency_records");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Key).HasMaxLength(128);
            builder.Property(row => row.RequestHash).HasMaxLength(128);
            builder.HasIndex(row => row.Key)
                .IsUnique()
                .HasDatabaseName("ux_economy_idempotency_records_key");
            builder.HasOne<EconomyPostingGroupRow>()
                .WithMany()
                .HasForeignKey(row => row.PostingGroupId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureChain(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EconomyChainHeadRow>(builder =>
        {
            builder.ToTable("economy_chain_head", table =>
                table.HasCheckConstraint("ck_economy_chain_head_singleton", "\"Id\" = 1"));
            builder.HasKey(row => row.Id);
            builder.Property(row => row.Hash).HasMaxLength(128);
        });

        modelBuilder.Entity<EconomyExternalAnchorRow>(builder =>
        {
            builder.ToTable("economy_external_anchors");
            builder.HasKey(row => row.Id);
            builder.Property(row => row.JournalHash).HasMaxLength(128);
            builder.Property(row => row.Signature).HasMaxLength(1024);
            builder.Property(row => row.WormReference).HasMaxLength(1024);
            builder.Property(row => row.DispatchSnapshotHash).HasMaxLength(128);
            builder.Property(row => row.Provider).HasMaxLength(100);
            builder.Property(row => row.ProviderReference).HasMaxLength(256);
            builder.HasIndex(row => row.JournalSequence)
                .HasDatabaseName("ix_economy_external_anchors_chain_sequence");
        });
    }
}
