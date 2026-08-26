using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyFoundationSchemaRollup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            InstallRolesAndExtensions(migrationBuilder);

            migrationBuilder.CreateTable(
                name: "economy_chain_head",
                columns: table => new
                {
                    Id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_chain_head", x => x.Id);
                    table.CheckConstraint("ck_economy_chain_head_singleton", "\"Id\" = 1");
                });

            migrationBuilder.CreateTable(
                name: "economy_external_anchors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    JournalHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Signature = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    WormReference = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    DispatchSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProviderReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AnchoredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_external_anchors", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_protected_change_cooldowns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ValueHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_protected_change_cooldowns", x => x.Id);
                    table.CheckConstraint("ck_economy_protected_change_cooldowns_version", "\"Version\" > 0");
                    table.CheckConstraint("ck_economy_protected_change_cooldowns_window", "\"AvailableAt\" > \"ChangedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_registered_capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AllowedTemplateKinds = table.Column<string>(type: "jsonb", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RevokedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_registered_capabilities", x => x.Id);
                    table.CheckConstraint("ck_economy_registered_capabilities_state", "(\"IsEnabled\" AND \"RevokedAt\" IS NULL) OR (NOT \"IsEnabled\" AND \"RevokedAt\" IS NOT NULL)");
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_counters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Dimension = table.Column<int>(type: "integer", nullable: false),
                    SubjectHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    WindowStartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    WindowEndsAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CounterVersion = table.Column<long>(type: "bigint", nullable: false),
                    MaxUnits = table.Column<long>(type: "bigint", nullable: false),
                    UsedUnits = table.Column<long>(type: "bigint", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_counters", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_counters_bounds", "\"CounterVersion\" > 0 AND \"MaxUnits\" > 0 AND \"UsedUnits\" >= 0 AND \"UsedUnits\" <= \"MaxUnits\"");
                    table.CheckConstraint("ck_economy_risk_counters_window", "\"WindowEndsAt\" > \"WindowStartedAt\"");
                });

            migrationBuilder.CreateTable(
                name: "economy_source_stamps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InternalSourceId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    SourceLegId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderReference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    AuthoritativeUnits = table.Column<long>(type: "bigint", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_source_stamps", x => x.Id);
                    table.CheckConstraint("ck_economy_source_stamps_confirmation", "(\"State\" = 2 AND \"ConfirmedAt\" IS NOT NULL AND \"ConfirmedAt\" >= \"ObservedAt\") OR (\"State\" <> 2 AND \"ConfirmedAt\" IS NULL)");
                    table.CheckConstraint("ck_economy_source_stamps_units_nonnegative", "\"AuthoritativeUnits\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_wallets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "economy_posting_groups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TemplateKind = table.Column<int>(type: "integer", nullable: false),
                    TemplateVersion = table.Column<int>(type: "integer", nullable: false),
                    Authority = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CapabilityId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: true),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_posting_groups", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_posting_groups_economy_source_stamps_SourceStampId",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_root_reversal_states",
                columns: table => new
                {
                    RootSourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Epoch = table.Column<long>(type: "bigint", nullable: false),
                    CumulativeProviderUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReversedUnits = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    TargetedRanges = table.Column<string>(type: "jsonb", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_root_reversal_states", x => x.RootSourceStampId);
                    table.CheckConstraint("ck_economy_root_reversal_states_cumulative_bounds", "\"CumulativeProviderUnits\" >= 0 AND \"ReversedUnits\" >= 0 AND \"ReversedUnits\" <= \"CumulativeProviderUnits\"");
                    table.CheckConstraint("ck_economy_root_reversal_states_epoch_nonnegative", "\"Epoch\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_root_reversal_states_economy_source_stamps_RootSour~",
                        column: x => x.RootSourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_source_stamp_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_source_stamp_events", x => x.Id);
                    table.CheckConstraint("ck_economy_source_stamp_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_source_stamp_events_economy_source_stamps_SourceSta~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_accounts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    Code = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_accounts", x => x.Id);
                    table.CheckConstraint("ck_economy_accounts_wallet_partition", "(\"WalletId\" IS NULL AND \"Code\" NOT IN (2, 3, 4)) OR (\"WalletId\" IS NOT NULL AND \"Code\" IN (2, 3, 4))");
                    table.ForeignKey(
                        name: "FK_economy_accounts_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_credit_lots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    RootSourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: false),
                    CreditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OriginalMaturesAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CashOutEligible = table.Column<bool>(type: "boolean", nullable: false),
                    JournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ReversalEpoch = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_credit_lots", x => x.Id);
                    table.CheckConstraint("ck_economy_credit_lots_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_credit_lots_maturity_order", "\"OriginalMaturesAt\" >= \"ConfirmedAt\"");
                    table.CheckConstraint("ck_economy_credit_lots_maturity_policy", "(\"Provenance\" = 2 AND \"Currency\" = 1 AND \"CashOutEligible\" AND \"OriginalMaturesAt\" = \"ConfirmedAt\" + INTERVAL '120 days') OR (\"Provenance\" <> 2 AND NOT \"CashOutEligible\")");
                    table.ForeignKey(
                        name: "FK_economy_credit_lots_economy_source_stamps_RootSourceStampId",
                        column: x => x.RootSourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_credit_lots_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_holds",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    Reason = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EffectiveAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReleasedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_holds", x => x.Id);
                    table.CheckConstraint("ck_economy_holds_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_holds_state_timestamp", "(\"Status\" = 1 AND \"ReleasedAt\" IS NULL) OR (\"Status\" <> 1 AND \"ReleasedAt\" >= \"EffectiveAt\")");
                    table.ForeignKey(
                        name: "FK_economy_holds_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_decisions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Outcome = table.Column<int>(type: "integer", nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ActorHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TemplateKind = table.Column<int>(type: "integer", nullable: false),
                    SourceWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    DestinationWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    CurrencyLegs = table.Column<string>(type: "jsonb", nullable: false),
                    SourceRoots = table.Column<string>(type: "jsonb", nullable: false),
                    ProviderReferenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    FeatureVersion = table.Column<long>(type: "bigint", nullable: false),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: false),
                    CounterVersion = table.Column<long>(type: "bigint", nullable: false),
                    EntityGraphVersion = table.Column<long>(type: "bigint", nullable: false),
                    EntityGraphEvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReasonCodes = table.Column<string>(type: "jsonb", nullable: false),
                    IssuedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_decisions", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_decisions_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_risk_decisions_lifetime", "\"ExpiresAt\" > \"IssuedAt\"");
                    table.CheckConstraint("ck_economy_risk_decisions_versions_positive", "\"PolicyVersion\" > 0 AND \"ReserveVersion\" > 0 AND \"FeatureVersion\" > 0 AND \"CounterVersion\" > 0 AND \"EntityGraphVersion\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_risk_decisions_economy_wallets_DestinationWalletId",
                        column: x => x.DestinationWalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_risk_decisions_economy_wallets_SourceWalletId",
                        column: x => x.SourceWalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_dispatch_snapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    Destination = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    EligibilityPayload = table.Column<string>(type: "jsonb", nullable: false),
                    ChainSequence = table.Column<long>(type: "bigint", nullable: false),
                    ChainHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ReserveVersion = table.Column<long>(type: "bigint", nullable: false),
                    KillSwitchEpoch = table.Column<long>(type: "bigint", nullable: false),
                    FencingToken = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_dispatch_snapshots", x => x.Id);
                    table.CheckConstraint("ck_economy_dispatch_snapshots_amount_positive", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_dispatch_snapshots_economy_posting_groups_PostingGr~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_idempotency_records",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_idempotency_records", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_idempotency_records_economy_posting_groups_PostingG~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_journal_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Hash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_journal_entries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_journal_entries_economy_posting_groups_PostingGroup~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_outbox_messages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    PayloadHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_outbox_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_outbox_messages_economy_posting_groups_PostingGroup~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_lot_lineage_edges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChildLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_lot_lineage_edges", x => x.Id);
                    table.CheckConstraint("ck_economy_lot_lineage_edges_amount_positive", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_lot_lineage_edges_economy_credit_lots_ChildLotId",
                        column: x => x.ChildLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_lot_lineage_edges_economy_credit_lots_ParentLotId",
                        column: x => x.ParentLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_hold_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    HoldId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_hold_events", x => x.Id);
                    table.CheckConstraint("ck_economy_hold_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_hold_events_economy_holds_HoldId",
                        column: x => x.HoldId,
                        principalTable: "economy_holds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_audit_evidence",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventKind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_audit_evidence", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_risk_audit_evidence_economy_risk_decisions_RiskDeci~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_counter_reservations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskCounterId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_counter_reservations", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_counter_reservations_amount_positive", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_risk_counter_reservations_economy_risk_counters_Ris~",
                        column: x => x.RiskCounterId,
                        principalTable: "economy_risk_counters",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_risk_counter_reservations_economy_risk_decisions_Ri~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_decision_consumptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperationFingerprint = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_decision_consumptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_economy_risk_decision_consumptions_economy_posting_groups_P~",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_risk_decision_consumptions_economy_risk_decisions_R~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_review_cases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubmittedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResolvedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ResolvedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    RequiredApprovals = table.Column<int>(type: "integer", nullable: false),
                    AppealOf = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_review_cases", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_review_cases_approvals", "\"RequiredApprovals\" BETWEEN 1 AND 2");
                    table.CheckConstraint("ck_economy_risk_review_cases_state", "(\"Status\" = 1 AND \"ResolvedAt\" IS NULL AND \"ResolvedBy\" IS NULL AND \"Resolution\" IS NULL) OR (\"Status\" IN (2, 3) AND \"ResolvedAt\" >= \"SubmittedAt\" AND \"ResolvedBy\" IS NOT NULL AND length(btrim(\"Resolution\")) > 0)");
                    table.ForeignKey(
                        name: "FK_economy_risk_review_cases_economy_risk_decisions_RiskDecisi~",
                        column: x => x.RiskDecisionId,
                        principalTable: "economy_risk_decisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_risk_review_cases_economy_risk_review_cases_AppealOf",
                        column: x => x.AppealOf,
                        principalTable: "economy_risk_review_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_journal_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalEntryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreditLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_journal_lines", x => x.Id);
                    table.CheckConstraint("ck_economy_journal_lines_amount_positive", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_journal_lines_economy_accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "economy_accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_journal_lines_economy_credit_lots_CreditLotId",
                        column: x => x.CreditLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_journal_lines_economy_journal_entries_JournalEntryId",
                        column: x => x.JournalEntryId,
                        principalTable: "economy_journal_entries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_journal_lines_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_risk_review_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskReviewCaseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    EvidenceHashes = table.Column<string>(type: "jsonb", nullable: false),
                    Resolution = table.Column<string>(type: "text", nullable: true),
                    DecisionCode = table.Column<int>(type: "integer", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_risk_review_events", x => x.Id);
                    table.CheckConstraint("ck_economy_risk_review_events_sequence_positive", "\"Sequence\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_risk_review_events_economy_risk_review_cases_RiskRe~",
                        column: x => x.RiskReviewCaseId,
                        principalTable: "economy_risk_review_cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_entry_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_entry_allocations", x => x.Id);
                    table.CheckConstraint("ck_economy_entry_allocations_amount_positive", "\"AmountUnits\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_entry_allocations_economy_credit_lots_ParentLotId",
                        column: x => x.ParentLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_entry_allocations_economy_journal_lines_JournalLine~",
                        column: x => x.JournalLineId,
                        principalTable: "economy_journal_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_provider_fact_allocations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    JournalLineId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectedAccount = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderObject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderMonetaryLeg = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    AllocatedUnits = table.Column<long>(type: "bigint", nullable: false),
                    CumulativeCreditedUnits = table.Column<long>(type: "bigint", nullable: false),
                    AuthoritativeUnits = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_provider_fact_allocations", x => x.Id);
                    table.CheckConstraint("ck_economy_provider_fact_allocations_cumulative_bounds", "\"AllocatedUnits\" > 0 AND \"CumulativeCreditedUnits\" >= \"AllocatedUnits\" AND \"CumulativeCreditedUnits\" <= \"AuthoritativeUnits\"");
                    table.ForeignKey(
                        name: "FK_economy_provider_fact_allocations_economy_journal_lines_Jou~",
                        column: x => x.JournalLineId,
                        principalTable: "economy_journal_lines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_provider_fact_allocations_economy_source_stamps_Sou~",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_fragment_root_ranges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RootSourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntryAllocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    StartInclusive = table.Column<long>(type: "bigint", nullable: false),
                    EndExclusive = table.Column<long>(type: "bigint", nullable: false),
                    ReversalEpoch = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_fragment_root_ranges", x => x.Id);
                    table.CheckConstraint("ck_economy_fragment_root_ranges_half_open", "\"StartInclusive\" >= 0 AND \"EndExclusive\" > \"StartInclusive\"");
                    table.CheckConstraint("ck_economy_fragment_root_ranges_single_owner", "(\"CreditLotId\" IS NULL) <> (\"EntryAllocationId\" IS NULL)");
                    table.ForeignKey(
                        name: "FK_economy_fragment_root_ranges_economy_credit_lots_CreditLotId",
                        column: x => x.CreditLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_fragment_root_ranges_economy_entry_allocations_Entr~",
                        column: x => x.EntryAllocationId,
                        principalTable: "economy_entry_allocations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_fragment_root_ranges_economy_source_stamps_RootSour~",
                        column: x => x.RootSourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_accounts_WalletId_Code_Currency_Provenance",
                table: "economy_accounts",
                columns: new[] { "WalletId", "Code", "Currency", "Provenance" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_credit_lots_WalletId",
                table: "economy_credit_lots",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_credit_lots_root_source",
                table: "economy_credit_lots",
                column: "RootSourceStampId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_dispatch_snapshots_PostingGroupId",
                table: "economy_dispatch_snapshots",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_dispatch_snapshots_hash",
                table: "economy_dispatch_snapshots",
                column: "SnapshotHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_entry_allocations_parent_lot",
                table: "economy_entry_allocations",
                column: "ParentLotId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_entry_allocations_line_parent",
                table: "economy_entry_allocations",
                columns: new[] { "JournalLineId", "ParentLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_external_anchors_chain_sequence",
                table: "economy_external_anchors",
                column: "JournalSequence");

            migrationBuilder.CreateIndex(
                name: "IX_economy_fragment_root_ranges_CreditLotId",
                table: "economy_fragment_root_ranges",
                column: "CreditLotId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_fragment_root_ranges_EntryAllocationId",
                table: "economy_fragment_root_ranges",
                column: "EntryAllocationId");

            migrationBuilder.CreateIndex(
                name: "ix_economy_fragment_root_ranges_root_epoch",
                table: "economy_fragment_root_ranges",
                columns: new[] { "RootSourceStampId", "ReversalEpoch" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_fragment_root_ranges_owner_interval",
                table: "economy_fragment_root_ranges",
                columns: new[] { "RootSourceStampId", "ReversalEpoch", "StartInclusive", "EndExclusive" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_hold_events_hold_sequence",
                table: "economy_hold_events",
                columns: new[] { "HoldId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_economy_holds_wallet_status",
                table: "economy_holds",
                columns: new[] { "WalletId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_idempotency_records_PostingGroupId",
                table: "economy_idempotency_records",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_idempotency_records_key",
                table: "economy_idempotency_records",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_journal_entries_posting_group_id",
                table: "economy_journal_entries",
                column: "PostingGroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_journal_entries_sequence",
                table: "economy_journal_entries",
                column: "Sequence",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_journal_lines_AccountId",
                table: "economy_journal_lines",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_journal_lines_CreditLotId",
                table: "economy_journal_lines",
                column: "CreditLotId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_journal_lines_JournalEntryId_Sequence",
                table: "economy_journal_lines",
                columns: new[] { "JournalEntryId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_journal_lines_WalletId",
                table: "economy_journal_lines",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_lot_lineage_edges_ChildLotId",
                table: "economy_lot_lineage_edges",
                column: "ChildLotId");

            migrationBuilder.CreateIndex(
                name: "ix_economy_lot_lineage_edges_parent_lot",
                table: "economy_lot_lineage_edges",
                column: "ParentLotId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_lot_lineage_edges_parent_child",
                table: "economy_lot_lineage_edges",
                columns: new[] { "ParentLotId", "ChildLotId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_outbox_messages_PostingGroupId",
                table: "economy_outbox_messages",
                column: "PostingGroupId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_outbox_messages_payload_hash",
                table: "economy_outbox_messages",
                column: "PayloadHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_posting_groups_idempotency_key",
                table: "economy_posting_groups",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_posting_groups_source_stamp",
                table: "economy_posting_groups",
                column: "SourceStampId",
                unique: true,
                filter: "\"SourceStampId\" IS NOT NULL AND \"TemplateKind\" = 1");

            migrationBuilder.CreateIndex(
                name: "ux_economy_protected_change_cooldowns_subject_kind",
                table: "economy_protected_change_cooldowns",
                columns: new[] { "SubjectId", "Kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_provider_fact_allocations_JournalLineId",
                table: "economy_provider_fact_allocations",
                column: "JournalLineId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_provider_fact_allocations_SourceStampId",
                table: "economy_provider_fact_allocations",
                column: "SourceStampId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_provider_fact_allocations_provider_leg",
                table: "economy_provider_fact_allocations",
                columns: new[] { "Provider", "Environment", "ConnectedAccount", "ProviderObject", "ProviderMonetaryLeg" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_registered_capabilities_name",
                table: "economy_registered_capabilities",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_audit_evidence_decision_hash",
                table: "economy_risk_audit_evidence",
                columns: new[] { "RiskDecisionId", "EvidenceHash" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_risk_counter_reservations_RiskCounterId",
                table: "economy_risk_counter_reservations",
                column: "RiskCounterId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_counter_reservations_decision_counter",
                table: "economy_risk_counter_reservations",
                columns: new[] { "RiskDecisionId", "RiskCounterId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_counters_scope_window",
                table: "economy_risk_counters",
                columns: new[] { "Dimension", "SubjectHash", "Operation", "Currency", "WindowStartedAt" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_decision_consumptions_decision",
                table: "economy_risk_decision_consumptions",
                column: "RiskDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_decision_consumptions_posting",
                table: "economy_risk_decision_consumptions",
                column: "PostingGroupId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_risk_decisions_DestinationWalletId",
                table: "economy_risk_decisions",
                column: "DestinationWalletId");

            migrationBuilder.CreateIndex(
                name: "ix_economy_risk_decisions_operation_fingerprint",
                table: "economy_risk_decisions",
                column: "OperationFingerprint",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_risk_decisions_SourceWalletId",
                table: "economy_risk_decisions",
                column: "SourceWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_risk_review_cases_AppealOf",
                table: "economy_risk_review_cases",
                column: "AppealOf");

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_review_cases_decision",
                table: "economy_risk_review_cases",
                column: "RiskDecisionId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_risk_review_events_case_sequence",
                table: "economy_risk_review_events",
                columns: new[] { "RiskReviewCaseId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_root_reversal_states_root_epoch",
                table: "economy_root_reversal_states",
                columns: new[] { "RootSourceStampId", "Epoch" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_source_stamp_events_source_sequence",
                table: "economy_source_stamp_events",
                columns: new[] { "SourceStampId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_source_stamps_internal_leg",
                table: "economy_source_stamps",
                columns: new[] { "SourceKind", "InternalSourceId", "SourceLegId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_source_stamps_provider_reference",
                table: "economy_source_stamps",
                columns: new[] { "Provider", "ProviderReference" },
                unique: true,
                filter: "\"Provider\" IS NOT NULL AND \"ProviderReference\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_economy_wallets_TenantId_OwnerId",
                table: "economy_wallets",
                columns: new[] { "TenantId", "OwnerId" },
                unique: true);

            HardenSchema(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveSecurity(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_chain_head");

            migrationBuilder.DropTable(
                name: "economy_dispatch_snapshots");

            migrationBuilder.DropTable(
                name: "economy_external_anchors");

            migrationBuilder.DropTable(
                name: "economy_fragment_root_ranges");

            migrationBuilder.DropTable(
                name: "economy_hold_events");

            migrationBuilder.DropTable(
                name: "economy_idempotency_records");

            migrationBuilder.DropTable(
                name: "economy_lot_lineage_edges");

            migrationBuilder.DropTable(
                name: "economy_outbox_messages");

            migrationBuilder.DropTable(
                name: "economy_protected_change_cooldowns");

            migrationBuilder.DropTable(
                name: "economy_provider_fact_allocations");

            migrationBuilder.DropTable(
                name: "economy_registered_capabilities");

            migrationBuilder.DropTable(
                name: "economy_risk_audit_evidence");

            migrationBuilder.DropTable(
                name: "economy_risk_counter_reservations");

            migrationBuilder.DropTable(
                name: "economy_risk_decision_consumptions");

            migrationBuilder.DropTable(
                name: "economy_risk_review_events");

            migrationBuilder.DropTable(
                name: "economy_root_reversal_states");

            migrationBuilder.DropTable(
                name: "economy_source_stamp_events");

            migrationBuilder.DropTable(
                name: "economy_entry_allocations");

            migrationBuilder.DropTable(
                name: "economy_holds");

            migrationBuilder.DropTable(
                name: "economy_risk_counters");

            migrationBuilder.DropTable(
                name: "economy_risk_review_cases");

            migrationBuilder.DropTable(
                name: "economy_journal_lines");

            migrationBuilder.DropTable(
                name: "economy_risk_decisions");

            migrationBuilder.DropTable(
                name: "economy_accounts");

            migrationBuilder.DropTable(
                name: "economy_credit_lots");

            migrationBuilder.DropTable(
                name: "economy_journal_entries");

            migrationBuilder.DropTable(
                name: "economy_wallets");

            migrationBuilder.DropTable(
                name: "economy_posting_groups");

            migrationBuilder.DropTable(
                name: "economy_source_stamps");

            RemoveRolePrivileges(migrationBuilder);

        }
    }
}
