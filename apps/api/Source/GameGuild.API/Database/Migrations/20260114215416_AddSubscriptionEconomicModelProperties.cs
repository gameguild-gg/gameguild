using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionEconomicModelProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AffiliateCommissionPercentage",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "BundleItemsJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "MaxAffiliateDiscount",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "ReferralCommissionPercentage",
                table: "Products");

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsFeatured = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    HasPrioritySupport = table.Column<bool>(type: "boolean", nullable: false),
                    HasAdvancedAnalytics = table.Column<bool>(type: "boolean", nullable: false),
                    HasCustomBranding = table.Column<bool>(type: "boolean", nullable: false),
                    Features = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    TrialPeriodDays = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    MonthlyPriceInCents = table.Column<long>(type: "bigint", nullable: false),
                    AnnualPriceInCents = table.Column<long>(type: "bigint", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MaxUsers = table.Column<int>(type: "integer", nullable: true),
                    MaxStorageMb = table.Column<long>(type: "bigint", nullable: true),
                    MaxApiCallsPerMonth = table.Column<long>(type: "bigint", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FulfilledOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastModifyingOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastRenewalIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LastPaymentIdempotencyKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    LockedPriceVersionId = table.Column<Guid>(type: "uuid", nullable: true),
                    LastProcessedBillingCycle = table.Column<int>(type: "integer", nullable: false),
                    TrialEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<int>(type: "integer", nullable: true),
                    CancellationNote = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExternalCustomerId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    AutoRenew = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentPeriodStart = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentPeriodEnd = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BillingCycleCount = table.Column<int>(type: "integer", nullable: false),
                    LastPaymentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Metadata = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    BillingCycle = table.Column<int>(type: "integer", nullable: false),
                    Amount_Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Amount_Currency = table.Column<string>(type: "text", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    NextBillingDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_SubscriptionPlans_PlanId",
                        column: x => x.PlanId,
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_ExternalId",
                table: "SubscriptionPlans",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_CancelledAt",
                table: "Subscriptions",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalCustomerId",
                table: "Subscriptions",
                column: "ExternalCustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ExternalId",
                table: "Subscriptions",
                column: "ExternalId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_LastPaymentAt",
                table: "Subscriptions",
                column: "LastPaymentAt");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_NextBillingDate",
                table: "Subscriptions",
                column: "NextBillingDate");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_Status",
                table: "Subscriptions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId_Status",
                table: "Subscriptions",
                columns: new[] { "TenantId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TrialEndDate",
                table: "Subscriptions",
                column: "TrialEndDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans");

            migrationBuilder.AddColumn<decimal>(
                name: "AffiliateCommissionPercentage",
                table: "Products",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "BundleItemsJson",
                table: "Products",
                type: "jsonb",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MaxAffiliateDiscount",
                table: "Products",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "ReferralCommissionPercentage",
                table: "Products",
                type: "numeric(5,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
