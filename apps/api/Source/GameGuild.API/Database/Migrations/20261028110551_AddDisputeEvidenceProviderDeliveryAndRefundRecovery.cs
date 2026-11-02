using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddDisputeEvidenceProviderDeliveryAndRefundRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LastFailureKind",
                table: "refunds",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceManifest",
                table: "payment_disputes",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EvidenceManifestHash",
                table: "payment_disputes",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContentDigest",
                table: "dispute_evidence",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProviderFileId",
                table: "dispute_evidence",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastFailureKind",
                table: "refunds");

            migrationBuilder.DropColumn(
                name: "EvidenceManifest",
                table: "payment_disputes");

            migrationBuilder.DropColumn(
                name: "EvidenceManifestHash",
                table: "payment_disputes");

            migrationBuilder.DropColumn(
                name: "ContentDigest",
                table: "dispute_evidence");

            migrationBuilder.DropColumn(
                name: "ProviderFileId",
                table: "dispute_evidence");
        }
    }
}
