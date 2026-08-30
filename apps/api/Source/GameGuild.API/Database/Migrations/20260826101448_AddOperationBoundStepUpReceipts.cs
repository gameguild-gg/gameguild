using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationBoundStepUpReceipts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "step_up_challenges",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    target_reference = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    payload_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verification_method = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    receipt_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: true),
                    consumed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_step_up_challenges", x => x.id);
                    table.CheckConstraint("ck_step_up_challenges_consumption", "consumed_at IS NULL OR (verified_at IS NOT NULL AND consumed_at >= verified_at)");
                    table.CheckConstraint("ck_step_up_challenges_expiry", "expires_at > created_at");
                    table.CheckConstraint("ck_step_up_challenges_payload_hash", "payload_hash ~ '^[0-9a-f]{64}$'");
                    table.CheckConstraint("ck_step_up_challenges_verification", "(verified_at IS NULL AND verification_method IS NULL AND receipt_hash IS NULL) OR (verified_at IS NOT NULL AND verification_method IS NOT NULL AND receipt_hash ~ '^[0-9a-f]{64}$')");
                });

            migrationBuilder.CreateIndex(
                name: "ix_step_up_challenges_subject_expiry",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                columns: new[] { "tenant_id", "actor_id", "session_id", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ux_step_up_challenges_receipt_hash",
                schema: "gameguild.authentication",
                table: "step_up_challenges",
                column: "receipt_hash",
                unique: true,
                filter: "receipt_hash IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "step_up_challenges",
                schema: "gameguild.authentication");
        }
    }
}
