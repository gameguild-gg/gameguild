using System;
using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260810090000_AddEconomyHardToSoftConversionWriter")]
public partial class AddEconomyHardToSoftConversionWriter : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "economy_hard_to_soft_conversion_operations",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                OutputLotId = table.Column<Guid>(type: "uuid", nullable: false),
                PrincipalHardUnits = table.Column<long>(type: "bigint", nullable: false),
                FeeHardUnits = table.Column<long>(type: "bigint", nullable: false),
                PrincipalPostingId = table.Column<Guid>(type: "uuid", nullable: false),
                FeePostingId = table.Column<Guid>(type: "uuid", nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_economy_hard_to_soft_conversion_operations", x => x.Id);
                table.ForeignKey(
                    name: "FK_economy_hard_to_soft_conversion_operations_economy_wallets_WalletId",
                    column: x => x.WalletId,
                    principalTable: "economy_wallets",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint(
                    "ck_economy_hard_to_soft_conversion_operations_amounts",
                    "\"PrincipalHardUnits\" > 0 AND \"FeeHardUnits\" >= 0 AND ((\"FeeHardUnits\" = 0 AND \"FeePostingId\" IS NULL) OR (\"FeeHardUnits\" > 0 AND \"FeePostingId\" IS NOT NULL))");
            });

        migrationBuilder.CreateIndex(
            name: "ux_economy_hard_to_soft_conversion_operations_idempotency",
            table: "economy_hard_to_soft_conversion_operations",
            column: "IdempotencyKey",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_economy_hard_to_soft_conversion_operations_principal_posting",
            table: "economy_hard_to_soft_conversion_operations",
            column: "PrincipalPostingId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ux_economy_hard_to_soft_conversion_operations_fee_posting",
            table: "economy_hard_to_soft_conversion_operations",
            column: "FeePostingId",
            unique: true,
            filter: "\"FeePostingId\" IS NOT NULL");

        InstallHardToSoftConversionWriter(migrationBuilder);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RemoveHardToSoftConversionWriter(migrationBuilder);
        migrationBuilder.DropTable(name: "economy_hard_to_soft_conversion_operations");
    }
}
