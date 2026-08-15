using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddLtiModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LtiDeployments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Issuer = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    DeploymentId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuthTokenUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    PlatformJwksUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    AuthorizationUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    KeyId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    PrivateKeyPem = table.Column<string>(type: "text", nullable: false),
                    Active = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiDeployments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiLineItemMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssessmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    LineItemId = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    LineItemUrl = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    MaxScore = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiLineItemMappings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LtiUserMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DeploymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Sub = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LtiUserMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "UX_LtiDeployments_Issuer_Client_Deployment",
                table: "LtiDeployments",
                columns: new[] { "Issuer", "ClientId", "DeploymentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiLineItemMappings_AssessmentId",
                table: "LtiLineItemMappings",
                column: "AssessmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LtiLineItemMappings_DeploymentId",
                table: "LtiLineItemMappings",
                column: "DeploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_LtiUserMappings_UserId",
                table: "LtiUserMappings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UX_LtiUserMappings_Deployment_Sub",
                table: "LtiUserMappings",
                columns: new[] { "DeploymentId", "Sub" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LtiDeployments");

            migrationBuilder.DropTable(
                name: "LtiLineItemMappings");

            migrationBuilder.DropTable(
                name: "LtiUserMappings");
        }
    }
}
