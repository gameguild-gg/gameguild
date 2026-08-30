using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyLegacyShadowMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_legacy_shadow_batches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    RequestedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    WalletCount = table.Column<int>(type: "integer", nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    FinancialLedgerEntryCount = table.Column<int>(type: "integer", nullable: false),
                    ExpectedHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    BackfilledHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    ReconciledHardUnits = table.Column<long>(type: "bigint", nullable: false),
                    WalletSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    TransactionSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FinancialLedgerSnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    FailureCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_legacy_shadow_batches", x => x.Id);
                    table.CheckConstraint("ck_economy_legacy_shadow_batches_counts", "\"WalletCount\" >= 0 AND \"TransactionCount\" >= 0 AND \"FinancialLedgerEntryCount\" >= 0");
                    table.CheckConstraint("ck_economy_legacy_shadow_batches_state", "\"State\" BETWEEN 1 AND 8");
                    table.CheckConstraint("ck_economy_legacy_shadow_batches_units", "\"ExpectedHardUnits\" >= 0 AND \"BackfilledHardUnits\" >= 0 AND \"ReconciledHardUnits\" >= 0");
                    table.CheckConstraint("ck_economy_legacy_shadow_batches_version", "\"Version\" > 0");
                });

            migrationBuilder.CreateTable(
                name: "economy_legacy_cutovers",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ProposedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    SecondApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RolledBackBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ProposedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    FirstApprovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ActivatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RolledBackAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Epoch = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_legacy_cutovers", x => x.TenantId);
                    table.CheckConstraint("ck_economy_legacy_cutovers_epoch", "\"Epoch\" > 0 AND \"Version\" > 0");
                    table.CheckConstraint("ck_economy_legacy_cutovers_state", "\"State\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_economy_legacy_cutovers_economy_legacy_shadow_batches_Batch~",
                        column: x => x.BatchId,
                        principalTable: "economy_legacy_shadow_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_legacy_shadow_wallets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    EconomyWalletId = table.Column<Guid>(type: "uuid", nullable: true),
                    OwnerId = table.Column<Guid>(type: "uuid", nullable: false),
                    LegacyBalanceMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CompletedCreditsMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    CompletedDebitsMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    TransactionCount = table.Column<int>(type: "integer", nullable: false),
                    SnapshotHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostingId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreditLotId = table.Column<Guid>(type: "uuid", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    JournalSequence = table.Column<long>(type: "bigint", nullable: true),
                    JournalHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ReconciliationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    FailureCode = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PostedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReconciledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_legacy_shadow_wallets", x => x.Id);
                    table.CheckConstraint("ck_economy_legacy_shadow_wallets_state", "\"State\" BETWEEN 1 AND 5");
                    table.CheckConstraint("ck_economy_legacy_shadow_wallets_transactions", "\"TransactionCount\" >= 0");
                    table.CheckConstraint("ck_economy_legacy_shadow_wallets_units", "\"LegacyBalanceMinorUnits\" >= 0 AND \"CompletedCreditsMinorUnits\" >= 0 AND \"CompletedDebitsMinorUnits\" >= 0");
                    table.CheckConstraint("ck_economy_legacy_shadow_wallets_version", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_legacy_shadow_wallets_economy_legacy_shadow_batches~",
                        column: x => x.BatchId,
                        principalTable: "economy_legacy_shadow_batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_legacy_shadow_wallets_economy_wallets_EconomyWallet~",
                        column: x => x.EconomyWalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_legacy_cutover_audit",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sequence = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ReauthenticationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_legacy_cutover_audit", x => x.Id);
                    table.CheckConstraint("ck_economy_legacy_cutover_audit_sequence", "\"Sequence\" > 0");
                    table.CheckConstraint("ck_economy_legacy_cutover_audit_state", "\"State\" BETWEEN 1 AND 4");
                    table.ForeignKey(
                        name: "FK_economy_legacy_cutover_audit_economy_legacy_cutovers_Tenant~",
                        column: x => x.TenantId,
                        principalTable: "economy_legacy_cutovers",
                        principalColumn: "TenantId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_cutover_audit_TenantId_Sequence",
                table: "economy_legacy_cutover_audit",
                columns: new[] { "TenantId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_cutovers_BatchId",
                table: "economy_legacy_cutovers",
                column: "BatchId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_batches_RequestHash",
                table: "economy_legacy_shadow_batches",
                column: "RequestHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_batches_TenantId_State",
                table: "economy_legacy_shadow_batches",
                columns: new[] { "TenantId", "State" });

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_wallets_BatchId_LegacyWalletId",
                table: "economy_legacy_shadow_wallets",
                columns: new[] { "BatchId", "LegacyWalletId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_wallets_CreditLotId",
                table: "economy_legacy_shadow_wallets",
                column: "CreditLotId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_wallets_EconomyWalletId",
                table: "economy_legacy_shadow_wallets",
                column: "EconomyWalletId");

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_wallets_PostingId",
                table: "economy_legacy_shadow_wallets",
                column: "PostingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_legacy_shadow_wallets_SourceStampId",
                table: "economy_legacy_shadow_wallets",
                column: "SourceStampId",
                unique: true);

            InstallLegacyShadowSecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveLegacyShadowSecurity(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_legacy_cutover_audit");

            migrationBuilder.DropTable(
                name: "economy_legacy_shadow_wallets");

            migrationBuilder.DropTable(
                name: "economy_legacy_cutovers");

            migrationBuilder.DropTable(
                name: "economy_legacy_shadow_batches");
        }
    }
}
