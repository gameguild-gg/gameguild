using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddCoursesModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessReviewItems_AccessReviewCampaigns_CampaignId",
                table: "AccessReviewItems");

            migrationBuilder.DropForeignKey(
                name: "FK_pricing_rules_Products_ProductId",
                table: "pricing_rules");

            migrationBuilder.DropForeignKey(
                name: "FK_SoDViolations_SoDRules_RuleId",
                table: "SoDViolations");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_TenantMembers_ParentMemberId",
                table: "TenantMembers");

            migrationBuilder.DropTable(
                name: "order_audit_logs");

            migrationBuilder.DropTable(
                name: "order_line_items");

            migrationBuilder.DropTable(
                name: "PermissionTemplates");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoDViolations",
                table: "SoDViolations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoDRules",
                table: "SoDRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionDelegations",
                table: "PermissionDelegations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JitElevationRequests",
                table: "JitElevationRequests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DelegatedAdminScopes",
                table: "DelegatedAdminScopes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataMaskingRules",
                table: "DataMaskingRules");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConditionalPolicies",
                table: "ConditionalPolicies");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessReviewItems",
                table: "AccessReviewItems");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessReviewCampaigns",
                table: "AccessReviewCampaigns");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies");

            migrationBuilder.RenameTable(
                name: "SoDViolations",
                newName: "SoDViolation");

            migrationBuilder.RenameTable(
                name: "SoDRules",
                newName: "SoDRule");

            migrationBuilder.RenameTable(
                name: "PermissionDelegations",
                newName: "PermissionDelegation");

            migrationBuilder.RenameTable(
                name: "JitElevationRequests",
                newName: "JitElevationRequest");

            migrationBuilder.RenameTable(
                name: "DelegatedAdminScopes",
                newName: "DelegatedAdminScope");

            migrationBuilder.RenameTable(
                name: "DataMaskingRules",
                newName: "DataMaskingRule");

            migrationBuilder.RenameTable(
                name: "ConditionalPolicies",
                newName: "ConditionalPolicy");

            migrationBuilder.RenameTable(
                name: "AccessReviewItems",
                newName: "AccessReviewItem");

            migrationBuilder.RenameTable(
                name: "AccessReviewCampaigns",
                newName: "AccessReviewCampaign");

            migrationBuilder.RenameTable(
                name: "AbacPolicies",
                newName: "AbacPolicy");

            migrationBuilder.RenameColumn(
                name: "Amount_Currency",
                table: "Subscriptions",
                newName: "Currency");

            migrationBuilder.RenameColumn(
                name: "Amount_Amount",
                table: "Subscriptions",
                newName: "Amount");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                newName: "ix_subscription_plans_name");

            migrationBuilder.RenameIndex(
                name: "IX_SubscriptionPlans_ExternalId",
                table: "SubscriptionPlans",
                newName: "ix_subscription_plans_external_id");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolations_UserId",
                table: "SoDViolation",
                newName: "IX_SoDViolation_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolations_TenantId",
                table: "SoDViolation",
                newName: "IX_SoDViolation_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolations_Status",
                table: "SoDViolation",
                newName: "IX_SoDViolation_Status");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolations_RuleId",
                table: "SoDViolation",
                newName: "IX_SoDViolation_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDRules_TenantId",
                table: "SoDRule",
                newName: "IX_SoDRule_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDRules_IsEnabled",
                table: "SoDRule",
                newName: "IX_SoDRule_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegations_TenantId",
                table: "PermissionDelegation",
                newName: "IX_PermissionDelegation_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegations_IsActive",
                table: "PermissionDelegation",
                newName: "IX_PermissionDelegation_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegations_ExpiresAt",
                table: "PermissionDelegation",
                newName: "IX_PermissionDelegation_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegations_DelegatorUserId",
                table: "PermissionDelegation",
                newName: "IX_PermissionDelegation_DelegatorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegations_DelegateUserId",
                table: "PermissionDelegation",
                newName: "IX_PermissionDelegation_DelegateUserId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequests_TenantId",
                table: "JitElevationRequest",
                newName: "IX_JitElevationRequest_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequests_Status",
                table: "JitElevationRequest",
                newName: "IX_JitElevationRequest_Status");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequests_RequesterId",
                table: "JitElevationRequest",
                newName: "IX_JitElevationRequest_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequests_ExpiresAt",
                table: "JitElevationRequest",
                newName: "IX_JitElevationRequest_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScopes_TenantId",
                table: "DelegatedAdminScope",
                newName: "IX_DelegatedAdminScope_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScopes_StartsAt_ExpiresAt",
                table: "DelegatedAdminScope",
                newName: "IX_DelegatedAdminScope_StartsAt_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScopes_IsActive",
                table: "DelegatedAdminScope",
                newName: "IX_DelegatedAdminScope_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScopes_AdminUserId",
                table: "DelegatedAdminScope",
                newName: "IX_DelegatedAdminScope_AdminUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRules_TenantId",
                table: "DataMaskingRule",
                newName: "IX_DataMaskingRule_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRules_ResourceType",
                table: "DataMaskingRule",
                newName: "IX_DataMaskingRule_ResourceType");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRules_IsEnabled",
                table: "DataMaskingRule",
                newName: "IX_DataMaskingRule_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicies_TenantId",
                table: "ConditionalPolicy",
                newName: "IX_ConditionalPolicy_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicies_Priority",
                table: "ConditionalPolicy",
                newName: "IX_ConditionalPolicy_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicies_IsEnabled",
                table: "ConditionalPolicy",
                newName: "IX_ConditionalPolicy_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItems_SubjectUserId",
                table: "AccessReviewItem",
                newName: "IX_AccessReviewItem_SubjectUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItems_ReviewerId",
                table: "AccessReviewItem",
                newName: "IX_AccessReviewItem_ReviewerId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItems_Decision",
                table: "AccessReviewItem",
                newName: "IX_AccessReviewItem_Decision");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItems_CampaignId",
                table: "AccessReviewItem",
                newName: "IX_AccessReviewItem_CampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaigns_TenantId",
                table: "AccessReviewCampaign",
                newName: "IX_AccessReviewCampaign_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaigns_Status",
                table: "AccessReviewCampaign",
                newName: "IX_AccessReviewCampaign_Status");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaigns_StartDate_EndDate",
                table: "AccessReviewCampaign",
                newName: "IX_AccessReviewCampaign_StartDate_EndDate");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_TenantId",
                table: "AbacPolicy",
                newName: "IX_AbacPolicy_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_Priority",
                table: "AbacPolicy",
                newName: "IX_AbacPolicy_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicies_IsEnabled",
                table: "AbacPolicy",
                newName: "IX_AbacPolicy_IsEnabled");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserPreferences",
                type: "uuid",
                nullable: true);

            // Fix: Set NULL metadata to empty JSON object before making non-nullable
            migrationBuilder.Sql("UPDATE \"UserNotifications\" SET \"Metadata\" = '{}' WHERE \"Metadata\" IS NULL;");

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "UserNotifications",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 10000,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserNotifications",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserMetadata",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "user_products",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "AdminEmail",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 5000,
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 5000);

            migrationBuilder.AlterColumn<string>(
                name: "ExternalReferences",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "CustomFields",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 10000);

            migrationBuilder.AlterColumn<string>(
                name: "ContactInfo",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<string>(
                name: "BusinessInfo",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                defaultValue: "{}",
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000);

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Subscriptions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "Subscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "CancellationReason",
                table: "Subscriptions",
                type: "text",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "BillingCycle",
                table: "Subscriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "Subscriptions",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                table: "Subscriptions",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubscriptionPlans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "promo_codes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "product_subscription_plans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "product_subscription_plans",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "product_pricing",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "pricing_tiers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "USD",
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "pricing_rules",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<decimal>(
                name: "FixedPrice",
                table: "pricing_rules",
                type: "numeric(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "BuyQuantity",
                table: "pricing_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DiscountAmount",
                table: "pricing_rules",
                type: "numeric(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "GetQuantity",
                table: "pricing_rules",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Method",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "Metadata",
                table: "DynamicRole",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoDViolation",
                table: "SoDViolation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoDRule",
                table: "SoDRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionDelegation",
                table: "PermissionDelegation",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JitElevationRequest",
                table: "JitElevationRequest",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DelegatedAdminScope",
                table: "DelegatedAdminScope",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataMaskingRule",
                table: "DataMaskingRule",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConditionalPolicy",
                table: "ConditionalPolicy",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessReviewItem",
                table: "AccessReviewItem",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessReviewCampaign",
                table: "AccessReviewCampaign",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbacPolicy",
                table: "AbacPolicy",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "capability_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OldValue = table.Column<bool>(type: "boolean", nullable: true),
                    NewValue = table.Column<bool>(type: "boolean", nullable: false),
                    OldSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    NewSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ChangedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ChangeReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChangeType = table.Column<int>(type: "integer", nullable: false),
                    ChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_capability_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "feature_flags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    default_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    enabled_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_global = table.Column<bool>(type: "boolean", nullable: false),
                    rollout_percentage = table.Column<int>(type: "integer", nullable: false),
                    environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    review_date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_kill_switch = table.Column<bool>(type: "boolean", nullable: false),
                    owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    escalation_contact = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    governance_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    requires_encryption = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flags", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "pricing_rule_tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PricingRuleId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: true),
                    MaxQuantity = table.Column<int>(type: "integer", nullable: true),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_rule_tiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_pricing_rule_tiers_pricing_rules_PricingRuleId",
                        column: x => x.PricingRuleId,
                        principalTable: "pricing_rules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "programs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    Slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Thumbnail = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    VideoShowcaseUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    EstimatedHours = table.Column<int>(type: "integer", nullable: true),
                    EnrollmentStatus = table.Column<int>(type: "integer", nullable: false),
                    MaxEnrollments = table.Column<int>(type: "integer", nullable: true),
                    EnrollmentDeadline = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Category = table.Column<int>(type: "integer", nullable: false),
                    Difficulty = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_programs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tenant_capabilities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CapabilityKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    ModificationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tenant_capabilities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: true),
                    ActorName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ActorEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    BeforeValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    AfterValues = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CorrelationId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Metadata = table.Column<Dictionary<string, string>>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantAuditLogs_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feature_flag_targets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_identifier = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    rollout_percentage = table.Column<int>(type: "integer", nullable: false),
                    custom_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    depends_on = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flag_targets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feature_flag_targets_feature_flags_feature_flag_id",
                        column: x => x.feature_flag_id,
                        principalTable: "feature_flags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "feature_flag_usage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    access_count = table.Column<long>(type: "bigint", nullable: false),
                    was_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    returned_value = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    first_access_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_access_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    context_data = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_feature_flag_usage", x => x.Id);
                    table.ForeignKey(
                        name: "FK_feature_flag_usage_feature_flags_feature_flag_id",
                        column: x => x.feature_flag_id,
                        principalTable: "feature_flags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "course_prerequisites",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    PrerequisiteCourseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    MinimumGrade = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    PrerequisiteGroup = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_course_prerequisites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_course_prerequisites_programs_CourseId",
                        column: x => x.CourseId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_course_prerequisites_programs_PrerequisiteCourseId",
                        column: x => x.PrerequisiteCourseId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "program_contents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false),
                    GradingMethod = table.Column<int>(type: "integer", nullable: false),
                    MaxPoints = table.Column<int>(type: "integer", nullable: true),
                    EstimatedMinutes = table.Column<int>(type: "integer", nullable: true),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_contents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_contents_program_contents_ParentId",
                        column: x => x.ParentId,
                        principalTable: "program_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_program_contents_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletionPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    FinalGrade = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_users_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_program_users_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_wishlists",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    NotifyWhenAvailable = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    NotificationSent = table.Column<bool>(type: "boolean", nullable: false),
                    LastNotificationSentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InterestedTags = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_wishlists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_wishlists_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_program_wishlists_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "content_interactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    ProgressPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    TimeSpentMinutes = table.Column<int>(type: "integer", nullable: true),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FirstAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastAccessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CompletionPercentage = table.Column<decimal>(type: "numeric", nullable: false),
                    SubmissionData = table.Column<string>(type: "text", nullable: true),
                    AttemptCount = table.Column<int>(type: "integer", nullable: false),
                    BestScore = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    BookmarkPosition = table.Column<string>(type: "text", nullable: true),
                    ProgramContentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_content_interactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_content_interactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_interactions_program_contents_ContentId",
                        column: x => x.ContentId,
                        principalTable: "program_contents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_content_interactions_program_contents_ProgramContentId",
                        column: x => x.ProgramContentId,
                        principalTable: "program_contents",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_content_interactions_program_users_ProgramUserId",
                        column: x => x.ProgramUserId,
                        principalTable: "program_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "program_ratings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<decimal>(type: "numeric(3,2)", precision: 3, scale: 2, nullable: false),
                    Review = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IsVerified = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    HelpfulVotes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    UnhelpfulVotes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ProgramUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_program_ratings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_program_ratings_program_users_ProgramUserId",
                        column: x => x.ProgramUserId,
                        principalTable: "program_users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_program_ratings_programs_ProgramId",
                        column: x => x.ProgramId,
                        principalTable: "programs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "activity_grades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentId = table.Column<Guid>(type: "uuid", nullable: false),
                    GraderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ContentInteractionId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProgramUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Points = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    MaxPoints = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    GradeLetter = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    Feedback = table.Column<string>(type: "text", nullable: true),
                    GradedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsFinalized = table.Column<bool>(type: "boolean", nullable: false),
                    RubricData = table.Column<string>(type: "text", nullable: true),
                    GradingTimeMinutes = table.Column<int>(type: "integer", nullable: true),
                    GradeType = table.Column<int>(type: "integer", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    GraderProgramUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    GradingDetails = table.Column<string>(type: "text", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_grades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_activity_grades_Users_GraderId",
                        column: x => x.GraderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activity_grades_Users_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activity_grades_content_interactions_ContentInteractionId",
                        column: x => x.ContentInteractionId,
                        principalTable: "content_interactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_activity_grades_program_users_GraderProgramUserId",
                        column: x => x.GraderProgramUserId,
                        principalTable: "program_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_activity_grades_program_users_ProgramUserId",
                        column: x => x.ProgramUserId,
                        principalTable: "program_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_usersession_expires_at",
                schema: "gameguild.authentication",
                table: "usersession",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_usersession_refresh_token",
                schema: "gameguild.authentication",
                table: "usersession",
                column: "RefreshToken",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usersession_user_id",
                schema: "gameguild.authentication",
                table: "usersession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId1",
                table: "UserPreferences",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsArchived",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsArchived" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId_Type_IsRead",
                table: "UserNotifications",
                columns: new[] { "UserId", "Type", "IsRead" });

            migrationBuilder.CreateIndex(
                name: "IX_UserNotifications_UserId1",
                table: "UserNotifications",
                column: "UserId1");

            migrationBuilder.CreateIndex(
                name: "IX_UserMetadata_UserId1",
                table: "UserMetadata",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusteddevice_user_fingerprint",
                schema: "gameguild.authentication",
                table: "trusteddevice",
                columns: new[] { "UserId", "DeviceFingerprint" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_trusteddevice_user_id",
                schema: "gameguild.authentication",
                table: "trusteddevice",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_AdminEmail",
                table: "Tenants",
                column: "AdminEmail");

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetadata_Industry",
                table: "TenantMetadata",
                column: "Industry");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetadata_Size",
                table: "TenantMetadata",
                column: "Size");

            migrationBuilder.CreateIndex(
                name: "IX_TenantMetadata_Type",
                table: "TenantMetadata",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_is_active",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_is_featured",
                table: "SubscriptionPlans",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_slug",
                table: "SubscriptionPlans",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_subscription_plans_sort_order",
                table: "SubscriptionPlans",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "ix_refreshtoken_expires_at",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "ix_refreshtoken_token",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_refreshtoken_user_id",
                schema: "gameguild.authentication",
                table: "refreshtoken",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_mfaattempt_attempted_at",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "ix_mfaattempt_tenant_id",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_mfaattempt_user_id",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_identityverification_status",
                schema: "gameguild.authentication",
                table: "identityverification",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_identityverification_user_id",
                schema: "gameguild.authentication",
                table: "identityverification",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_identityverification_user_type",
                schema: "gameguild.authentication",
                table: "identityverification",
                columns: new[] { "UserId", "VerificationType" });

            migrationBuilder.CreateIndex(
                name: "ix_contenttypepermission_tenant_contenttype",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                columns: new[] { "TenantId", "ContentTypeName" });

            migrationBuilder.CreateIndex(
                name: "ix_contenttypepermission_tenant_id",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_contenttypepermission_user_id",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_blockchaincertificateanchor_certificate_hash",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                column: "CertificateHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_blockchaincertificateanchor_transaction_hash",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                column: "TransactionHash");

            migrationBuilder.CreateIndex(
                name: "ix_blockchaincertificateanchor_user_id",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "ix_authenticationattempt_attempted_at",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "ix_authenticationattempt_email",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "ix_authenticationattempt_ip_address",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "IpAddress");

            migrationBuilder.CreateIndex(
                name: "ix_authenticationattempt_tenant_id",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_authenticationattempt_user_id",
                schema: "gameguild.authentication",
                table: "authenticationattempt",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_ContentInteractionId",
                table: "activity_grades",
                column: "ContentInteractionId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_GradedAt",
                table: "activity_grades",
                column: "GradedAt");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_GraderId",
                table: "activity_grades",
                column: "GraderId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_GraderProgramUserId",
                table: "activity_grades",
                column: "GraderProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_Points",
                table: "activity_grades",
                column: "Points");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_ProgramUserId",
                table: "activity_grades",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_StudentId",
                table: "activity_grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_StudentId_ContentInteractionId",
                table: "activity_grades",
                columns: new[] { "StudentId", "ContentInteractionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_activity_grades_TenantId",
                table: "activity_grades",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_capability_audit_logs_capability",
                table: "capability_audit_logs",
                column: "CapabilityKey");

            migrationBuilder.CreateIndex(
                name: "ix_capability_audit_logs_tenant_changed",
                table: "capability_audit_logs",
                columns: new[] { "TenantId", "ChangedAt" });

            migrationBuilder.CreateIndex(
                name: "ix_capability_audit_logs_user",
                table: "capability_audit_logs",
                column: "ChangedByUserId",
                filter: "\"ChangedByUserId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_CompletedAt",
                table: "content_interactions",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ContentId",
                table: "content_interactions",
                column: "ContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_IsCompleted",
                table: "content_interactions",
                column: "IsCompleted");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ProgramContentId",
                table: "content_interactions",
                column: "ProgramContentId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_ProgramUserId",
                table: "content_interactions",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_StartedAt",
                table: "content_interactions",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_TenantId",
                table: "content_interactions",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_UserId",
                table: "content_interactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_content_interactions_UserId_ContentId",
                table: "content_interactions",
                columns: new[] { "UserId", "ContentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_prerequisites_CourseId",
                table: "course_prerequisites",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_course_prerequisites_CourseId_PrerequisiteCourseId",
                table: "course_prerequisites",
                columns: new[] { "CourseId", "PrerequisiteCourseId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_course_prerequisites_PrerequisiteCourseId",
                table: "course_prerequisites",
                column: "PrerequisiteCourseId");

            migrationBuilder.CreateIndex(
                name: "IX_course_prerequisites_TenantId",
                table: "course_prerequisites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_feature_flag_id",
                table: "feature_flag_targets",
                column: "feature_flag_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_priority",
                table: "feature_flag_targets",
                column: "priority");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_target_identifier",
                table: "feature_flag_targets",
                column: "target_identifier");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_target_type",
                table: "feature_flag_targets",
                column: "target_type");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_tenant_id",
                table: "feature_flag_targets",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_targets_unique",
                table: "feature_flag_targets",
                columns: new[] { "feature_flag_id", "target_type", "target_identifier" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_composite",
                table: "feature_flag_usage",
                columns: new[] { "feature_flag_id", "tenant_id", "environment" });

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_created_at",
                table: "feature_flag_usage",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_environment",
                table: "feature_flag_usage",
                column: "environment");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_feature_flag_id",
                table: "feature_flag_usage",
                column: "feature_flag_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_last_access_at",
                table: "feature_flag_usage",
                column: "last_access_at");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_tenant_id",
                table: "feature_flag_usage",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flag_usage_user_id",
                table: "feature_flag_usage",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_environment",
                table: "feature_flags",
                column: "environment");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_expires_at",
                table: "feature_flags",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_is_enabled",
                table: "feature_flags",
                column: "is_enabled");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_key",
                table: "feature_flags",
                column: "key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_key_environment",
                table: "feature_flags",
                columns: new[] { "key", "environment" });

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_review_date",
                table: "feature_flags",
                column: "review_date");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_tenant_id",
                table: "feature_flags",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "idx_feature_flags_type",
                table: "feature_flags",
                column: "type");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_tiers_MaxQuantity",
                table: "pricing_rule_tiers",
                column: "MaxQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_tiers_MinQuantity",
                table: "pricing_rule_tiers",
                column: "MinQuantity");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_rule_tiers_PricingRuleId",
                table: "pricing_rule_tiers",
                column: "PricingRuleId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_IsRequired",
                table: "program_contents",
                column: "IsRequired");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ParentId",
                table: "program_contents",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_ProgramId",
                table: "program_contents",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_SortOrder",
                table: "program_contents",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_TenantId",
                table: "program_contents",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_contents_Type",
                table: "program_contents",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProgramId",
                table: "program_ratings",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_ProgramUserId",
                table: "program_ratings",
                column: "ProgramUserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_ratings_UserId",
                table: "program_ratings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_CreatedAt",
                table: "program_ratings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_IsFeatured",
                table: "program_ratings",
                column: "IsFeatured");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_IsVerified",
                table: "program_ratings",
                column: "IsVerified");

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_ProgramId_UserId_Unique",
                table: "program_ratings",
                columns: new[] { "ProgramId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramRatings_Rating",
                table: "program_ratings",
                column: "Rating");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_CompletedAt",
                table: "program_users",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_IsActive",
                table: "program_users",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_JoinedAt",
                table: "program_users",
                column: "JoinedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_ProgramId",
                table: "program_users",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_TenantId",
                table: "program_users",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_UserId",
                table: "program_users",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_users_UserId_ProgramId",
                table: "program_users",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_AddedAt",
                table: "program_wishlists",
                column: "AddedAt");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_NotifyWhenAvailable",
                table: "program_wishlists",
                column: "NotifyWhenAvailable");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_Priority",
                table: "program_wishlists",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_ProgramId",
                table: "program_wishlists",
                column: "ProgramId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_TenantId",
                table: "program_wishlists",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId",
                table: "program_wishlists",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_program_wishlists_UserId_ProgramId",
                table: "program_wishlists",
                columns: new[] { "UserId", "ProgramId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_programs_Category",
                table: "programs",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_programs_CreatedAt",
                table: "programs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_programs_CreatorId",
                table: "programs",
                column: "CreatorId");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Difficulty",
                table: "programs",
                column: "Difficulty");

            migrationBuilder.CreateIndex(
                name: "IX_programs_EnrollmentStatus",
                table: "programs",
                column: "EnrollmentStatus");

            migrationBuilder.CreateIndex(
                name: "IX_programs_Slug",
                table: "programs",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_programs_Status",
                table: "programs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_programs_TenantId",
                table: "programs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_expires_at",
                table: "tenant_capabilities",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_tenant_capability",
                table: "tenant_capabilities",
                columns: new[] { "TenantId", "CapabilityKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_capabilities_tenant_id",
                table: "tenant_capabilities",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_Action",
                table: "TenantAuditLogs",
                column: "Action");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_ActorId",
                table: "TenantAuditLogs",
                column: "ActorId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_TenantId",
                table: "TenantAuditLogs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_TenantId_Timestamp",
                table: "TenantAuditLogs",
                columns: new[] { "TenantId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_TenantAuditLogs_Timestamp",
                table: "TenantAuditLogs",
                column: "Timestamp");

            migrationBuilder.AddForeignKey(
                name: "FK_AccessReviewItem_AccessReviewCampaign_CampaignId",
                table: "AccessReviewItem",
                column: "CampaignId",
                principalTable: "AccessReviewCampaign",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SoDViolation_SoDRule_RuleId",
                table: "SoDViolation",
                column: "RuleId",
                principalTable: "SoDRule",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMembers_TenantMembers_ParentMemberId",
                table: "TenantMembers",
                column: "ParentMemberId",
                principalTable: "TenantMembers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserMetadata_Users_UserId1",
                table: "UserMetadata",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserNotifications_Users_UserId1",
                table: "UserNotifications",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_UserPreferences_Users_UserId1",
                table: "UserPreferences",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AccessReviewItem_AccessReviewCampaign_CampaignId",
                table: "AccessReviewItem");

            migrationBuilder.DropForeignKey(
                name: "FK_SoDViolation_SoDRule_RuleId",
                table: "SoDViolation");

            migrationBuilder.DropForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_TenantMembers_TenantMembers_ParentMemberId",
                table: "TenantMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_UserMetadata_Users_UserId1",
                table: "UserMetadata");

            migrationBuilder.DropForeignKey(
                name: "FK_UserNotifications_Users_UserId1",
                table: "UserNotifications");

            migrationBuilder.DropForeignKey(
                name: "FK_UserPreferences_Users_UserId1",
                table: "UserPreferences");

            migrationBuilder.DropTable(
                name: "activity_grades");

            migrationBuilder.DropTable(
                name: "capability_audit_logs");

            migrationBuilder.DropTable(
                name: "course_prerequisites");

            migrationBuilder.DropTable(
                name: "feature_flag_targets");

            migrationBuilder.DropTable(
                name: "feature_flag_usage");

            migrationBuilder.DropTable(
                name: "pricing_rule_tiers");

            migrationBuilder.DropTable(
                name: "program_ratings");

            migrationBuilder.DropTable(
                name: "program_wishlists");

            migrationBuilder.DropTable(
                name: "tenant_capabilities");

            migrationBuilder.DropTable(
                name: "TenantAuditLogs");

            migrationBuilder.DropTable(
                name: "content_interactions");

            migrationBuilder.DropTable(
                name: "feature_flags");

            migrationBuilder.DropTable(
                name: "program_contents");

            migrationBuilder.DropTable(
                name: "program_users");

            migrationBuilder.DropTable(
                name: "programs");

            migrationBuilder.DropIndex(
                name: "ix_usersession_expires_at",
                schema: "gameguild.authentication",
                table: "usersession");

            migrationBuilder.DropIndex(
                name: "ix_usersession_refresh_token",
                schema: "gameguild.authentication",
                table: "usersession");

            migrationBuilder.DropIndex(
                name: "ix_usersession_user_id",
                schema: "gameguild.authentication",
                table: "usersession");

            migrationBuilder.DropIndex(
                name: "IX_UserPreferences_UserId1",
                table: "UserPreferences");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_IsArchived",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_IsRead",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId_Type_IsRead",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserNotifications_UserId1",
                table: "UserNotifications");

            migrationBuilder.DropIndex(
                name: "IX_UserMetadata_UserId1",
                table: "UserMetadata");

            migrationBuilder.DropIndex(
                name: "ix_trusteddevice_user_fingerprint",
                schema: "gameguild.authentication",
                table: "trusteddevice");

            migrationBuilder.DropIndex(
                name: "ix_trusteddevice_user_id",
                schema: "gameguild.authentication",
                table: "trusteddevice");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_AdminEmail",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_IsActive",
                table: "Tenants");

            migrationBuilder.DropIndex(
                name: "IX_TenantMetadata_Industry",
                table: "TenantMetadata");

            migrationBuilder.DropIndex(
                name: "IX_TenantMetadata_Size",
                table: "TenantMetadata");

            migrationBuilder.DropIndex(
                name: "IX_TenantMetadata_Type",
                table: "TenantMetadata");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_is_active",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_is_featured",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_slug",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "ix_subscription_plans_sort_order",
                table: "SubscriptionPlans");

            migrationBuilder.DropIndex(
                name: "ix_refreshtoken_expires_at",
                schema: "gameguild.authentication",
                table: "refreshtoken");

            migrationBuilder.DropIndex(
                name: "ix_refreshtoken_token",
                schema: "gameguild.authentication",
                table: "refreshtoken");

            migrationBuilder.DropIndex(
                name: "ix_refreshtoken_user_id",
                schema: "gameguild.authentication",
                table: "refreshtoken");

            migrationBuilder.DropIndex(
                name: "ix_mfaattempt_attempted_at",
                schema: "gameguild.authentication",
                table: "mfaattempt");

            migrationBuilder.DropIndex(
                name: "ix_mfaattempt_tenant_id",
                schema: "gameguild.authentication",
                table: "mfaattempt");

            migrationBuilder.DropIndex(
                name: "ix_mfaattempt_user_id",
                schema: "gameguild.authentication",
                table: "mfaattempt");

            migrationBuilder.DropIndex(
                name: "ix_identityverification_status",
                schema: "gameguild.authentication",
                table: "identityverification");

            migrationBuilder.DropIndex(
                name: "ix_identityverification_user_id",
                schema: "gameguild.authentication",
                table: "identityverification");

            migrationBuilder.DropIndex(
                name: "ix_identityverification_user_type",
                schema: "gameguild.authentication",
                table: "identityverification");

            migrationBuilder.DropIndex(
                name: "ix_contenttypepermission_tenant_contenttype",
                schema: "gameguild.authentication",
                table: "contenttypepermission");

            migrationBuilder.DropIndex(
                name: "ix_contenttypepermission_tenant_id",
                schema: "gameguild.authentication",
                table: "contenttypepermission");

            migrationBuilder.DropIndex(
                name: "ix_contenttypepermission_user_id",
                schema: "gameguild.authentication",
                table: "contenttypepermission");

            migrationBuilder.DropIndex(
                name: "ix_blockchaincertificateanchor_certificate_hash",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor");

            migrationBuilder.DropIndex(
                name: "ix_blockchaincertificateanchor_transaction_hash",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor");

            migrationBuilder.DropIndex(
                name: "ix_blockchaincertificateanchor_user_id",
                schema: "gameguild.authentication",
                table: "blockchaincertificateanchor");

            migrationBuilder.DropIndex(
                name: "ix_authenticationattempt_attempted_at",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropIndex(
                name: "ix_authenticationattempt_email",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropIndex(
                name: "ix_authenticationattempt_ip_address",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropIndex(
                name: "ix_authenticationattempt_tenant_id",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropIndex(
                name: "ix_authenticationattempt_user_id",
                schema: "gameguild.authentication",
                table: "authenticationattempt");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoDViolation",
                table: "SoDViolation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_SoDRule",
                table: "SoDRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PermissionDelegation",
                table: "PermissionDelegation");

            migrationBuilder.DropPrimaryKey(
                name: "PK_JitElevationRequest",
                table: "JitElevationRequest");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DelegatedAdminScope",
                table: "DelegatedAdminScope");

            migrationBuilder.DropPrimaryKey(
                name: "PK_DataMaskingRule",
                table: "DataMaskingRule");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ConditionalPolicy",
                table: "ConditionalPolicy");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessReviewItem",
                table: "AccessReviewItem");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AccessReviewCampaign",
                table: "AccessReviewCampaign");

            migrationBuilder.DropPrimaryKey(
                name: "PK_AbacPolicy",
                table: "AbacPolicy");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserPreferences");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserNotifications");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserMetadata");

            migrationBuilder.DropColumn(
                name: "StartDate",
                table: "Subscriptions");

            migrationBuilder.DropColumn(
                name: "BuyQuantity",
                table: "pricing_rules");

            migrationBuilder.DropColumn(
                name: "DiscountAmount",
                table: "pricing_rules");

            migrationBuilder.DropColumn(
                name: "GetQuantity",
                table: "pricing_rules");

            migrationBuilder.DropColumn(
                name: "Metadata",
                table: "DynamicRole");

            migrationBuilder.RenameTable(
                name: "SoDViolation",
                newName: "SoDViolations");

            migrationBuilder.RenameTable(
                name: "SoDRule",
                newName: "SoDRules");

            migrationBuilder.RenameTable(
                name: "PermissionDelegation",
                newName: "PermissionDelegations");

            migrationBuilder.RenameTable(
                name: "JitElevationRequest",
                newName: "JitElevationRequests");

            migrationBuilder.RenameTable(
                name: "DelegatedAdminScope",
                newName: "DelegatedAdminScopes");

            migrationBuilder.RenameTable(
                name: "DataMaskingRule",
                newName: "DataMaskingRules");

            migrationBuilder.RenameTable(
                name: "ConditionalPolicy",
                newName: "ConditionalPolicies");

            migrationBuilder.RenameTable(
                name: "AccessReviewItem",
                newName: "AccessReviewItems");

            migrationBuilder.RenameTable(
                name: "AccessReviewCampaign",
                newName: "AccessReviewCampaigns");

            migrationBuilder.RenameTable(
                name: "AbacPolicy",
                newName: "AbacPolicies");

            migrationBuilder.RenameColumn(
                name: "Currency",
                table: "Subscriptions",
                newName: "Amount_Currency");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Subscriptions",
                newName: "Amount_Amount");

            migrationBuilder.RenameIndex(
                name: "ix_subscription_plans_name",
                table: "SubscriptionPlans",
                newName: "IX_SubscriptionPlans_Name");

            migrationBuilder.RenameIndex(
                name: "ix_subscription_plans_external_id",
                table: "SubscriptionPlans",
                newName: "IX_SubscriptionPlans_ExternalId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolation_UserId",
                table: "SoDViolations",
                newName: "IX_SoDViolations_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolation_TenantId",
                table: "SoDViolations",
                newName: "IX_SoDViolations_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolation_Status",
                table: "SoDViolations",
                newName: "IX_SoDViolations_Status");

            migrationBuilder.RenameIndex(
                name: "IX_SoDViolation_RuleId",
                table: "SoDViolations",
                newName: "IX_SoDViolations_RuleId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDRule_TenantId",
                table: "SoDRules",
                newName: "IX_SoDRules_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_SoDRule_IsEnabled",
                table: "SoDRules",
                newName: "IX_SoDRules_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegation_TenantId",
                table: "PermissionDelegations",
                newName: "IX_PermissionDelegations_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegation_IsActive",
                table: "PermissionDelegations",
                newName: "IX_PermissionDelegations_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegation_ExpiresAt",
                table: "PermissionDelegations",
                newName: "IX_PermissionDelegations_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegation_DelegatorUserId",
                table: "PermissionDelegations",
                newName: "IX_PermissionDelegations_DelegatorUserId");

            migrationBuilder.RenameIndex(
                name: "IX_PermissionDelegation_DelegateUserId",
                table: "PermissionDelegations",
                newName: "IX_PermissionDelegations_DelegateUserId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequest_TenantId",
                table: "JitElevationRequests",
                newName: "IX_JitElevationRequests_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequest_Status",
                table: "JitElevationRequests",
                newName: "IX_JitElevationRequests_Status");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequest_RequesterId",
                table: "JitElevationRequests",
                newName: "IX_JitElevationRequests_RequesterId");

            migrationBuilder.RenameIndex(
                name: "IX_JitElevationRequest_ExpiresAt",
                table: "JitElevationRequests",
                newName: "IX_JitElevationRequests_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScope_TenantId",
                table: "DelegatedAdminScopes",
                newName: "IX_DelegatedAdminScopes_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScope_StartsAt_ExpiresAt",
                table: "DelegatedAdminScopes",
                newName: "IX_DelegatedAdminScopes_StartsAt_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScope_IsActive",
                table: "DelegatedAdminScopes",
                newName: "IX_DelegatedAdminScopes_IsActive");

            migrationBuilder.RenameIndex(
                name: "IX_DelegatedAdminScope_AdminUserId",
                table: "DelegatedAdminScopes",
                newName: "IX_DelegatedAdminScopes_AdminUserId");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRule_TenantId",
                table: "DataMaskingRules",
                newName: "IX_DataMaskingRules_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRule_ResourceType",
                table: "DataMaskingRules",
                newName: "IX_DataMaskingRules_ResourceType");

            migrationBuilder.RenameIndex(
                name: "IX_DataMaskingRule_IsEnabled",
                table: "DataMaskingRules",
                newName: "IX_DataMaskingRules_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicy_TenantId",
                table: "ConditionalPolicies",
                newName: "IX_ConditionalPolicies_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicy_Priority",
                table: "ConditionalPolicies",
                newName: "IX_ConditionalPolicies_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_ConditionalPolicy_IsEnabled",
                table: "ConditionalPolicies",
                newName: "IX_ConditionalPolicies_IsEnabled");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItem_SubjectUserId",
                table: "AccessReviewItems",
                newName: "IX_AccessReviewItems_SubjectUserId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItem_ReviewerId",
                table: "AccessReviewItems",
                newName: "IX_AccessReviewItems_ReviewerId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItem_Decision",
                table: "AccessReviewItems",
                newName: "IX_AccessReviewItems_Decision");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewItem_CampaignId",
                table: "AccessReviewItems",
                newName: "IX_AccessReviewItems_CampaignId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaign_TenantId",
                table: "AccessReviewCampaigns",
                newName: "IX_AccessReviewCampaigns_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaign_Status",
                table: "AccessReviewCampaigns",
                newName: "IX_AccessReviewCampaigns_Status");

            migrationBuilder.RenameIndex(
                name: "IX_AccessReviewCampaign_StartDate_EndDate",
                table: "AccessReviewCampaigns",
                newName: "IX_AccessReviewCampaigns_StartDate_EndDate");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicy_TenantId",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_TenantId");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicy_Priority",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_Priority");

            migrationBuilder.RenameIndex(
                name: "IX_AbacPolicy_IsEnabled",
                table: "AbacPolicies",
                newName: "IX_AbacPolicies_IsEnabled");

            migrationBuilder.AlterColumn<string>(
                name: "Metadata",
                table: "UserNotifications",
                type: "jsonb",
                maxLength: 10000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 10000);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "user_products",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "AdminEmail",
                table: "Tenants",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Tags",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 5000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 5000,
                oldDefaultValue: "[]");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalReferences",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000,
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "CustomFields",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 10000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 10000,
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "ContactInfo",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000,
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<string>(
                name: "BusinessInfo",
                table: "TenantMetadata",
                type: "jsonb",
                maxLength: 8000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "jsonb",
                oldMaxLength: 8000,
                oldDefaultValue: "{}");

            migrationBuilder.AlterColumn<Guid>(
                name: "TenantId",
                table: "Subscriptions",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AlterColumn<int>(
                name: "Status",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<int>(
                name: "CancellationReason",
                table: "Subscriptions",
                type: "integer",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "BillingCycle",
                table: "Subscriptions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "Amount_Currency",
                table: "Subscriptions",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3);

            migrationBuilder.AlterColumn<string>(
                name: "Slug",
                table: "SubscriptionPlans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "SubscriptionPlans",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);

            migrationBuilder.AlterColumn<string>(
                name: "Description",
                table: "SubscriptionPlans",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(2000)",
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "promo_codes",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "Name",
                table: "product_subscription_plans",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100);

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "product_subscription_plans",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "product_pricing",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<string>(
                name: "Currency",
                table: "pricing_tiers",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(3)",
                oldMaxLength: 3,
                oldDefaultValue: "USD");

            migrationBuilder.AlterColumn<Guid>(
                name: "ProductId",
                table: "pricing_rules",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "FixedPrice",
                table: "pricing_rules",
                type: "numeric(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,2)",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Method",
                schema: "gameguild.authentication",
                table: "mfaattempt",
                type: "integer",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Permissions",
                schema: "gameguild.authentication",
                table: "contenttypepermission",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoDViolations",
                table: "SoDViolations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_SoDRules",
                table: "SoDRules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PermissionDelegations",
                table: "PermissionDelegations",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_JitElevationRequests",
                table: "JitElevationRequests",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DelegatedAdminScopes",
                table: "DelegatedAdminScopes",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_DataMaskingRules",
                table: "DataMaskingRules",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ConditionalPolicies",
                table: "ConditionalPolicies",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessReviewItems",
                table: "AccessReviewItems",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AccessReviewCampaigns",
                table: "AccessReviewCampaigns",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AbacPolicies",
                table: "AbacPolicies",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscountTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FulfilledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Metadata = table.Column<string>(type: "jsonb", nullable: true),
                    OrderType = table.Column<int>(type: "integer", nullable: false),
                    PaidAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: true),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    PaymentProviderReference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RefundAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    RefundReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RefundedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Subtotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TargetSubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    TaxAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Total = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserAgent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_orders_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PermissionTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    IsSystemTemplate = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<Dictionary<string, object>>(type: "jsonb", nullable: true),
                    MinimumTier = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "order_audit_logs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    AdditionalContext = table.Column<string>(type: "jsonb", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalPaymentId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    InitiatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    NewStatus = table.Column<int>(type: "integer", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PreviousStatus = table.Column<int>(type: "integer", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_audit_logs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_audit_logs_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "order_line_items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    BasePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    BillingIntervalSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DiscountAmount = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    IsSubscription = table.Column<bool>(type: "boolean", nullable: false),
                    LineTotal = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    PricingTierId = table.Column<Guid>(type: "uuid", nullable: true),
                    PricingTierNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProductNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PromoCodesApplied = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    SalePriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitPriceSnapshot = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_order_line_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_order_line_items_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_order_line_items_orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_order_line_items_user_products_UserProductId",
                        column: x => x.UserProductId,
                        principalTable: "user_products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_NewStatus",
                table: "order_audit_logs",
                column: "NewStatus");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OccurredAt",
                table: "order_audit_logs",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_OrderId",
                table: "order_audit_logs",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_audit_logs_TenantId",
                table: "order_audit_logs",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_OrderId",
                table: "order_line_items",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_ProductId",
                table: "order_line_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_order_line_items_UserProductId",
                table: "order_line_items",
                column: "UserProductId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_CreatedAt",
                table: "orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_orders_IdempotencyKey",
                table: "orders",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_Status",
                table: "orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_TenantId",
                table: "orders",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_UserId",
                table: "orders",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_IsSystemTemplate",
                table: "PermissionTemplates",
                column: "IsSystemTemplate");

            migrationBuilder.CreateIndex(
                name: "IX_PermissionTemplates_Name",
                table: "PermissionTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AccessReviewItems_AccessReviewCampaigns_CampaignId",
                table: "AccessReviewItems",
                column: "CampaignId",
                principalTable: "AccessReviewCampaigns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_pricing_rules_Products_ProductId",
                table: "pricing_rules",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_SoDViolations_SoDRules_RuleId",
                table: "SoDViolations",
                column: "RuleId",
                principalTable: "SoDRules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                table: "Subscriptions",
                column: "PlanId",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TenantMembers_TenantMembers_ParentMemberId",
                table: "TenantMembers",
                column: "ParentMemberId",
                principalTable: "TenantMembers",
                principalColumn: "Id");
        }
    }
}
