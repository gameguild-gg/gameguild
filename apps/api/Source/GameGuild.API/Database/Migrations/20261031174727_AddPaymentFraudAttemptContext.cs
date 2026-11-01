using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFraudAttemptContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FraudDeviceFingerprint",
                table: "payments",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FraudSourceIpAddress",
                table: "payments",
                type: "character varying(45)",
                maxLength: 45,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_payments_fraud_device_lookup",
                table: "payments",
                columns: new[] { "TenantId", "FraudDeviceFingerprint", "CreatedAt" },
                filter: "\"FraudDeviceFingerprint\" IS NOT NULL")
                .Annotation("Npgsql:CreatedConcurrently", true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_payments_fraud_device_lookup",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "FraudDeviceFingerprint",
                table: "payments");

            migrationBuilder.DropColumn(
                name: "FraudSourceIpAddress",
                table: "payments");
        }
    }
}
