using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(GameGuild.API.Database.ApplicationDbContext))]
[Migration("20261022110000_EncryptPaymentMethodGatewayIds")]
public partial class EncryptPaymentMethodGatewayIds : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Issue #414: gateway payment-method references are stored protected (DataProtection).
        // The column widens for the protected payload and gains a deterministic SHA-256
        // lookup hash so equality queries never depend on the (non-deterministic) ciphertext.
        migrationBuilder.AddColumn<string>(
            name: "GatewayPaymentMethodIdHash",
            table: "payment_methods",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true);

        migrationBuilder.AlterColumn<string>(
            name: "GatewayPaymentMethodId",
            table: "payment_methods",
            type: "character varying(512)",
            maxLength: 512,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        migrationBuilder.CreateIndex(
            name: "IX_payment_methods_GatewayPaymentMethodIdHash",
            table: "payment_methods",
            column: "GatewayPaymentMethodIdHash");

        migrationBuilder.CreateIndex(
            name: "ix_payment_methods_tenant_gateway_hash_unique",
            table: "payment_methods",
            columns: new[] { "TenantId", "GatewayProvider", "GatewayPaymentMethodIdHash" },
            unique: true,
            filter: "\"DeletedAt\" IS NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "ix_payment_methods_tenant_gateway_hash_unique",
            table: "payment_methods");

        migrationBuilder.DropIndex(
            name: "IX_payment_methods_GatewayPaymentMethodIdHash",
            table: "payment_methods");

        migrationBuilder.DropColumn(
            name: "GatewayPaymentMethodIdHash",
            table: "payment_methods");

        migrationBuilder.AlterColumn<string>(
            name: "GatewayPaymentMethodId",
            table: "payment_methods",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(512)",
            oldMaxLength: 512);
    }
}
