using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPayoutAuthorizationEvidence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_payout_authorization_evidence",
                columns: table => new
                {
                    OperationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Phase = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskDecisionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ReauthenticationEvidenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    OperationFingerprintHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CapabilityReceiptId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityReceiptHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_payout_authorization_evidence", x => new { x.OperationId, x.Phase });
                    table.CheckConstraint("ck_economy_payout_authorization_evidence_phase", "\"Phase\" BETWEEN 1 AND 2");
                    table.ForeignKey(
                        name: "FK_economy_payout_authorization_evidence_economy_payout_operat~",
                        column: x => x.OperationId,
                        principalTable: "economy_payout_operations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_payout_authorization_evidence_CapabilityReceiptId",
                table: "economy_payout_authorization_evidence",
                column: "CapabilityReceiptId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_economy_payout_authorization_evidence_TenantId_RecordedAt",
                table: "economy_payout_authorization_evidence",
                columns: new[] { "TenantId", "RecordedAt" });

            InstallPayoutAuthorizationEvidence(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemovePayoutAuthorizationEvidence(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_payout_authorization_evidence");
        }
    }
}
