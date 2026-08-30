using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddSelfServiceEconomyTransferIntents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "economy_self_service_transfer_intents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecipientUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferType = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<int>(type: "integer", nullable: false),
                    Provenance = table.Column<int>(type: "integer", nullable: false),
                    AmountUnits = table.Column<long>(type: "bigint", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    ProviderReferenceHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    DestinationHash = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RequestedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_self_service_transfer_intents", x => x.Id);
                    table.CheckConstraint("ck_economy_self_service_transfer_intents_amount_positive", "\"AmountUnits\" > 0");
                    table.CheckConstraint("ck_economy_self_service_transfer_intents_currency_provenance", "(\"Currency\" = 1 AND \"Provenance\" = 1) OR (\"Currency\" = 2 AND \"Provenance\" = 3)");
                    table.CheckConstraint("ck_economy_self_service_transfer_intents_parties_distinct", "\"ActorId\" <> \"RecipientUserId\"");
                    table.CheckConstraint("ck_economy_self_service_transfer_intents_type_valid", "\"TransferType\" IN (1, 2, 3)");
                });

            migrationBuilder.CreateIndex(
                name: "ix_economy_self_service_transfer_intents_recipient_time",
                table: "economy_self_service_transfer_intents",
                columns: new[] { "TenantId", "RecipientUserId", "RequestedAt" });

            migrationBuilder.CreateIndex(
                name: "ux_economy_self_service_transfer_intents_actor_key",
                table: "economy_self_service_transfer_intents",
                columns: new[] { "TenantId", "ActorId", "IdempotencyKey" },
                unique: true);

            InstallSelfServiceTransferSecurity(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RemoveSelfServiceTransferSecurity(migrationBuilder);

            migrationBuilder.DropTable(
                name: "economy_self_service_transfer_intents");
        }
    }
}
