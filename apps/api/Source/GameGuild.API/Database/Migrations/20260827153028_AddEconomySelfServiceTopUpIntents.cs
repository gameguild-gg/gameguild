using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomySelfServiceTopUpIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_top_up_intents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PaymentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    HardCoinUnits = table.Column<long>(type: "bigint", nullable: false),
                    UsdMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    JurisdictionCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    PolicyVersion = table.Column<long>(type: "bigint", nullable: false),
                    PolicyHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Provider = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderEnvironment = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ProviderAccountId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderObjectId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    ProviderObjectType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ProviderMonetaryLeg = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ProviderBoundAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_top_up_intents", x => x.Id);
                    table.CheckConstraint("ck_economy_top_up_intents_amount_positive", "\"HardCoinUnits\" > 0 AND \"UsdMinorUnits\" > 0");
                    table.CheckConstraint("ck_economy_top_up_intents_provider_binding", "(\"Status\" = 1 AND \"ProviderEnvironment\" IS NULL AND \"ProviderAccountId\" IS NULL AND \"ProviderObjectId\" IS NULL AND \"ProviderObjectType\" IS NULL AND \"ProviderMonetaryLeg\" IS NULL AND \"ProviderBoundAt\" IS NULL) OR (\"Status\" <> 1 AND \"ProviderEnvironment\" IS NOT NULL AND \"ProviderAccountId\" IS NOT NULL AND \"ProviderObjectId\" IS NOT NULL AND \"ProviderObjectType\" IS NOT NULL AND \"ProviderMonetaryLeg\" IS NOT NULL AND \"ProviderBoundAt\" IS NOT NULL)");
                    table.CheckConstraint("ck_economy_top_up_intents_version_positive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_top_up_intents_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_economy_top_up_intents_WalletId",
                table: "economy_top_up_intents",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_top_up_intents_actor_key",
                table: "economy_top_up_intents",
                columns: new[] { "TenantId", "ActorId", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_top_up_intents_payment",
                table: "economy_top_up_intents",
                column: "PaymentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_top_up_intents_provider_object",
                table: "economy_top_up_intents",
                columns: new[] { "Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId", "ProviderObjectType", "ProviderMonetaryLeg" },
                unique: true,
                filter: "\"ProviderObjectId\" IS NOT NULL");

            InstallEconomyTopUpSecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveEconomyTopUpSecurity(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_top_up_intents");
        }
    }
}
