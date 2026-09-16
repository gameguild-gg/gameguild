using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260811120000_AddEconomyPayoutRequestPersistence")]
public partial class AddEconomyPayoutRequestPersistence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "economy_payout_requests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                PayeeId = table.Column<Guid>(type: "uuid", nullable: false),
                WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                State = table.Column<int>(type: "integer", nullable: false),
                Version = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_payout_requests", x => x.Id);
                table.CheckConstraint("ck_economy_payout_requests_amount_positive", "\"AmountUnits\" > 0");
                table.CheckConstraint("ck_economy_payout_requests_state", "\"State\" BETWEEN 1 AND 4");
                table.CheckConstraint("ck_economy_payout_requests_version_positive", "\"Version\" > 0");
                table.CheckConstraint("ck_economy_payout_requests_timestamps", "\"UpdatedAt\" >= \"CreatedAt\"");
                table.ForeignKey(
                    name: "FK_economy_payout_requests_economy_wallets_WalletId",
                    column: x => x.WalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_payout_requests_payee_idempotency",
            table: "economy_payout_requests",
            columns: new[] { "PayeeId", "IdempotencyKey" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_economy_payout_requests_active_wallet",
            table: "economy_payout_requests",
            column: "WalletId",
            unique: true,
            filter: "\"State\" IN (1, 3)");

        migrationBuilder.CreateIndex(
            name: "ix_economy_payout_requests_payee_created",
            table: "economy_payout_requests",
            columns: new[] { "PayeeId", "CreatedAt" });

        PayoutRequestPersistenceSql.Install(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        PayoutRequestPersistenceSql.Remove(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_payout_requests");
    }
}
