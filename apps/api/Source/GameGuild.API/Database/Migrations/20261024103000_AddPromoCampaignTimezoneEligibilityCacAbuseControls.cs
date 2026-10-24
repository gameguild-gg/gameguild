using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoCampaignTimezoneEligibilityCacAbuseControls : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Issue #418 timezone awareness: validity windows may be expressed in an IANA
            // timezone; edges are stored as UTC instants (existing columns unchanged type).
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "promo_codes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Issue #418 customer-type eligibility: Any (0) keeps existing codes unrestricted.
            migrationBuilder.AddColumn<int>(
                name: "EligibleCustomerType",
                table: "promo_codes",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "ix_promo_codes_eligible_customer_type",
                table: "promo_codes",
                column: "EligibleCustomerType");

            // Issue #418 campaign management: managed lifecycle entity behind the free-form
            // PromoCode.CampaignId grouping key, with CAC spend inputs.
            migrationBuilder.CreateTable(
                name: "promo_campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    StartsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EndsAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TimeZoneId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Budget = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    AcquisitionSpend = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "USD"),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_campaigns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_campaigns_Users_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaigns_ends_at_utc",
                table: "promo_campaigns",
                column: "EndsAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaigns_key",
                table: "promo_campaigns",
                column: "Key");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaigns_starts_at_utc",
                table: "promo_campaigns",
                column: "StartsAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaigns_status",
                table: "promo_campaigns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaigns_tenant_id_key",
                table: "promo_campaigns",
                columns: new[] { "TenantId", "Key" },
                unique: true);

            // Issue #418 CAC attribution: one row per customer — the unique index on UserId
            // makes the first attribution win.
            migrationBuilder.CreateTable(
                name: "promo_campaign_attributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CampaignKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    FirstPurchaseUsageId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    AttributedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_promo_campaign_attributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_promo_campaign_attributions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaign_attributions_attributed_at_utc",
                table: "promo_campaign_attributions",
                column: "AttributedAtUtc");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaign_attributions_campaign_key",
                table: "promo_campaign_attributions",
                column: "CampaignKey");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaign_attributions_first_purchase_usage_id",
                table: "promo_campaign_attributions",
                column: "FirstPurchaseUsageId");

            migrationBuilder.CreateIndex(
                name: "ix_promo_campaign_attributions_user_id",
                table: "promo_campaign_attributions",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "promo_campaign_attributions");

            migrationBuilder.DropTable(
                name: "promo_campaigns");

            migrationBuilder.DropIndex(
                name: "ix_promo_codes_eligible_customer_type",
                table: "promo_codes");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "promo_codes");

            migrationBuilder.DropColumn(
                name: "EligibleCustomerType",
                table: "promo_codes");
        }
    }
}
