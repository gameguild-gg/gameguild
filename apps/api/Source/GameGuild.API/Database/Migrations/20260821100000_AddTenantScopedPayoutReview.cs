using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260821100000_AddTenantScopedPayoutReview")]
public partial class AddTenantScopedPayoutReview : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "TenantId",
            table: "economy_payout_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "FirstApprovalActorId",
            table: "economy_payout_requests",
            type: "uuid",
            nullable: true);

        migrationBuilder.Sql(
            """
            UPDATE public.economy_payout_requests request
            SET "TenantId" = wallet."TenantId"
            FROM public.economy_wallets wallet
            WHERE wallet."Id" = request."WalletId";

            ALTER TABLE public.economy_payout_requests
                ALTER COLUMN "TenantId" SET NOT NULL;

            ALTER TABLE public.economy_payout_requests
                DROP CONSTRAINT IF EXISTS ck_economy_payout_requests_state;
            ALTER TABLE public.economy_payout_requests
                ADD CONSTRAINT ck_economy_payout_requests_state
                CHECK ("State" IN (1, 2, 3, 4, 5));
            """);

        migrationBuilder.DropIndex(
            name: "ux_economy_payout_requests_payee_idempotency",
            table: "economy_payout_requests");

        migrationBuilder.CreateIndex(
            name: "ux_economy_payout_requests_tenant_payee_idempotency",
            table: "economy_payout_requests",
            columns: new[] { "TenantId", "PayeeId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_economy_payout_requests_tenant_review_queue",
            table: "economy_payout_requests",
            columns: new[] { "TenantId", "State", "CreatedAt", "Id" });

        migrationBuilder.CreateTable(
            name: "economy_payout_request_review_audit_events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                RequestId = table.Column<Guid>(type: "uuid", nullable: false),
                TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                Outcome = table.Column<int>(type: "integer", nullable: false),
                Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_payout_request_review_audit_events", x => x.Id);
                table.CheckConstraint(
                    "ck_economy_payout_request_review_audit_events_outcome",
                    "\"Outcome\" IN (3, 4)");
                table.CheckConstraint(
                    "ck_economy_payout_request_review_audit_events_reason",
                    "char_length(btrim(\"Reason\")) BETWEEN 3 AND 1000");
                table.ForeignKey(
                    name: "FK_economy_payout_request_review_audit_events_economy_payout_requests_RequestId",
                    column: x => x.RequestId,
                    principalTable: "economy_payout_requests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ix_economy_payout_request_review_audit_events_tenant_request_occurred",
            table: "economy_payout_request_review_audit_events",
            columns: new[] { "TenantId", "RequestId", "OccurredAt", "Id" });

        InstallPayoutRequestReviewSecurity(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemovePayoutRequestReviewSecurity(migrationBuilder);

        migrationBuilder.Sql(
            """
            UPDATE public.economy_payout_requests
            SET "State" = 1,
                "FirstApprovalActorId" = NULL
            WHERE "State" = 5;

            ALTER TABLE public.economy_payout_requests
                DROP CONSTRAINT IF EXISTS ck_economy_payout_requests_state;
            ALTER TABLE public.economy_payout_requests
                ADD CONSTRAINT ck_economy_payout_requests_state
                CHECK ("State" BETWEEN 1 AND 4);
            """);

        migrationBuilder.DropTable(name: "economy_payout_request_review_audit_events");
        migrationBuilder.DropIndex(
            name: "ix_economy_payout_requests_tenant_review_queue",
            table: "economy_payout_requests");
        migrationBuilder.DropIndex(
            name: "ux_economy_payout_requests_tenant_payee_idempotency",
            table: "economy_payout_requests");
        migrationBuilder.CreateIndex(
            name: "ux_economy_payout_requests_payee_idempotency",
            table: "economy_payout_requests",
            columns: new[] { "PayeeId", "IdempotencyKey" },
            unique: true);
        migrationBuilder.DropColumn(name: "FirstApprovalActorId", table: "economy_payout_requests");
        migrationBuilder.DropColumn(name: "TenantId", table: "economy_payout_requests");
    }
}
