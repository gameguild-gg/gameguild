using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class CleanupDuplicateEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "accessreviewitem",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "conditionalpolicy",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "tenantpermission",
                schema: "gameguild.authentication");

            migrationBuilder.DropTable(
                name: "accessreviewcampaign",
                schema: "gameguild.authentication");

            migrationBuilder.DropPrimaryKey(
                name: "PK_abacpolicy",
                schema: "gameguild.authentication",
                table: "abacpolicy");

            migrationBuilder.RenameTable(
                name: "abacpolicy",
                schema: "gameguild.authentication",
                newName: "AbacPolicies");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "AbacPolicies",
                newName: "Id");

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_TenantId",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_Priority",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_abacpolicy_IsEnabled",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_IsEnabled");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies");

            migrationBuilder.RenameTable(
                name: "AbacPolicies",
                newName: "abacpolicy",
                newSchema: "gameguild.authentication");

            migrationBuilder.RenameColumn(
                name: "Id",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "id");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_TenantId",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_Priority",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_IsEnabled",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                newName: "IX_abacpolicy_IsEnabled");

            migrationBuilder.AddPrimaryKey(
                name: "PK_abacpolicy",
                schema: "gameguild.authentication",
                table: "abacpolicy",
                column: "id");

            migrationBuilder.CreateTable(
                name: "accessreviewcampaign",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    AutoRevokeOnExpiry = table.Column<bool>(type: "boolean", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FilterCriteria = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GracePeriodDays = table.Column<int>(type: "integer", nullable: true),
                    Instructions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ReminderFrequencyDays = table.Column<int>(type: "integer", nullable: true),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StartedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessreviewcampaign", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conditionalpolicy",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    ConditionType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeviceConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnforcementMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EnvironmentConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LocationConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    PermissionType = table.Column<int>(type: "integer", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    TimeConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditionalpolicy", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tenantpermission",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    Permissions = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenantpermission", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "accessreviewitem",
                schema: "gameguild.authentication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ContextInfo = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Decision = table.Column<int>(type: "integer", nullable: true),
                    DecisionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastReminderSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Permissions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    RemindersSent = table.Column<int>(type: "integer", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    ResourceType = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewerId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accessreviewitem", x => x.id);
                    table.ForeignKey(
                        name: "FK_accessreviewitem_accessreviewcampaign_CampaignId",
                        column: x => x.CampaignId,
                        principalSchema: "gameguild.authentication",
                        principalTable: "accessreviewcampaign",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_accessreviewitem_CampaignId",
                schema: "gameguild.authentication",
                table: "accessreviewitem",
                column: "CampaignId");
        }
    }
}
