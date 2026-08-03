using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyProjectionRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_projection_reconciliation_events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PreviousHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RebuiltHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    SourceJournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_projection_reconciliation_events", x => x.Id);
                    table.CheckConstraint("ck_economy_projection_events_sequence_nonnegative", "\"SourceJournalSequence\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_projection_reconciliation_events_economy_wallets_Wa~",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "economy_wallet_balance_projections",
                columns: table => new
                {
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    PendingHard = table.Column<long>(type: "bigint", nullable: false),
                    PendingSoft = table.Column<long>(type: "bigint", nullable: false),
                    PurchasedHard = table.Column<long>(type: "bigint", nullable: false),
                    EarnedHard = table.Column<long>(type: "bigint", nullable: false),
                    RestrictedHard = table.Column<long>(type: "bigint", nullable: false),
                    Soft = table.Column<long>(type: "bigint", nullable: false),
                    ImmatureEarnedHard = table.Column<long>(type: "bigint", nullable: false),
                    HeldHard = table.Column<long>(type: "bigint", nullable: false),
                    HeldSoft = table.Column<long>(type: "bigint", nullable: false),
                    AvailableHardToSpend = table.Column<long>(type: "bigint", nullable: false),
                    AvailableSoftToSpend = table.Column<long>(type: "bigint", nullable: false),
                    WithdrawableHard = table.Column<long>(type: "bigint", nullable: false),
                    ReviewState = table.Column<int>(type: "integer", nullable: false),
                    SourceJournalSequence = table.Column<long>(type: "bigint", nullable: false),
                    ProjectionHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RebuiltAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_wallet_balance_projections", x => x.WalletId);
                    table.CheckConstraint("ck_economy_wallet_balance_projections_amounts_nonnegative", "\"PendingHard\" >= 0 AND \"PendingSoft\" >= 0 AND \"PurchasedHard\" >= 0 AND \"EarnedHard\" >= 0 AND \"RestrictedHard\" >= 0 AND \"Soft\" >= 0 AND \"ImmatureEarnedHard\" >= 0 AND \"HeldHard\" >= 0 AND \"HeldSoft\" >= 0 AND \"AvailableHardToSpend\" >= 0 AND \"AvailableSoftToSpend\" >= 0 AND \"WithdrawableHard\" >= 0");
                    table.CheckConstraint("ck_economy_wallet_balance_projections_sequence_nonnegative", "\"SourceJournalSequence\" >= 0");
                    table.ForeignKey(
                        name: "FK_economy_wallet_balance_projections_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_economy_projection_reconciliation_events_wallet_detected",
                table: "economy_projection_reconciliation_events",
                columns: new[] { "WalletId", "DetectedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_economy_wallet_balance_projections_review_state",
                table: "economy_wallet_balance_projections",
                column: "ReviewState");

            InstallProjectionRecovery(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveProjectionRecovery(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_projection_reconciliation_events");

            migrationBuilder.DropTable(
                name: "economy_wallet_balance_projections");
        }
    }
}
