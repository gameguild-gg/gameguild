using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260809220000_AddEconomyFifoTransferWriter")]
public partial class AddEconomyFifoTransferWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ux_economy_fragment_root_ranges_owner_interval",
            table: "economy_fragment_root_ranges");

        migrationBuilder.CreateTable(
            name: "economy_fifo_transfer_operations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                SourceWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                DestinationWalletId = table.Column<Guid>(type: "uuid", nullable: false),
                Currency = table.Column<int>(type: "integer", nullable: false),
                Provenance = table.Column<int>(type: "integer", nullable: false),
                AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_fifo_transfer_operations", row => row.Id);
                table.CheckConstraint(
                    "ck_economy_fifo_transfer_operations_values",
                    "\"AmountUnits\" > 0 AND \"Currency\" IN (1, 2) AND \"Provenance\" IN (1, 2, 3, 4, 5, 6, 7) AND \"SourceWalletId\" <> \"DestinationWalletId\"");
                table.ForeignKey(
                    name: "FK_economy_fifo_transfer_operations_economy_wallets_DestinationWalletId",
                    column: row => row.DestinationWalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_economy_fifo_transfer_operations_economy_wallets_SourceWalletId",
                    column: row => row.SourceWalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_fifo_transfer_operations_idempotency",
            table: "economy_fifo_transfer_operations",
            column: "IdempotencyKey",
            unique: true);

        InstallFifoTransferWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveFifoTransferWriter(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_fifo_transfer_operations");

        migrationBuilder.CreateIndex(
            name: "ux_economy_fragment_root_ranges_owner_interval",
            table: "economy_fragment_root_ranges",
            columns: new[] { "RootSourceStampId", "ReversalEpoch", "StartInclusive", "EndExclusive" },
            unique: true);
    }
}
