using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPolicyBundleRegistry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PolicyBundles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BundleType = table.Column<int>(type: "integer", nullable: false),
                    ContentHash = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DigitalSignature = table.Column<string>(type: "text", nullable: true),
                    SignedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    SignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PolicyData = table.Column<string>(type: "text", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    IsGlobal = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PreviousVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeploymentCount = table.Column<int>(type: "integer", nullable: false),
                    LastDeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyBundles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyRegistryAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    PerformedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Details = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyRegistryAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PolicyBundleDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BundleId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Environment = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    DeployedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeployedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RolledBackBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RollbackReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    VerificationPassed = table.Column<bool>(type: "boolean", nullable: false),
                    VerificationDetails = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeploymentNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PolicyBundleDeployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PolicyBundleDeployments_PolicyBundles_BundleId",
                        column: x => x.BundleId,
                        principalTable: "PolicyBundles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBundleDeployments_Bundle_Time",
                table: "PolicyBundleDeployments",
                columns: new[] { "BundleId", "DeployedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBundleDeployments_Tenant_Status",
                table: "PolicyBundleDeployments",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBundles_Name",
                table: "PolicyBundles",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBundles_PreviousVersion",
                table: "PolicyBundles",
                column: "PreviousVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyBundles_Tenant_Status",
                table: "PolicyBundles",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PolicyRegistryAuditLogs_Bundle",
                table: "PolicyRegistryAuditLogs",
                column: "BundleId");

            migrationBuilder.CreateIndex(
                name: "IX_PolicyRegistryAuditLogs_Time",
                table: "PolicyRegistryAuditLogs",
                column: "PerformedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PolicyBundleDeployments");

            migrationBuilder.DropTable(
                name: "PolicyRegistryAuditLogs");

            migrationBuilder.DropTable(
                name: "PolicyBundles");
        }
    }
}
