using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddGatewayCredentialsAndBillingCountry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillingCountry",
                table: "payments",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "gateway_credentials",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProviderKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SecretName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EncryptedValue = table.Column<string>(type: "text", nullable: false),
                    Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ActivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeactivatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProvisionedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gateway_credentials", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_gateway_credentials_active_slot",
                table: "gateway_credentials",
                columns: new[] { "ProviderKey", "SecretName" },
                unique: true,
                filter: "\"DeactivatedAtUtc\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_gateway_credentials_lookup",
                table: "gateway_credentials",
                columns: new[] { "ProviderKey", "SecretName", "DeactivatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gateway_credentials");

            migrationBuilder.DropColumn(
                name: "BillingCountry",
                table: "payments");
        }
    }
}
