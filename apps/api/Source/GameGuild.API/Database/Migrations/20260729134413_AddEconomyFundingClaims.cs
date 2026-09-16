using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEconomyFundingClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_source_stamps_confirmation",
                table: "economy_source_stamps");

            migrationBuilder.CreateTable(
                name: "economy_funding_claims",
                columns: table => new
                {
                    SourceStampId = table.Column<Guid>(type: "uuid", nullable: false),
                    WalletId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Environment = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ConnectedAccount = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderObject = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    ProviderMonetaryLeg = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    AuthoritativeUsdMinorUnits = table.Column<long>(type: "bigint", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    ObservedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConfirmedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StateChangedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PostingGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    RootCreditLotId = table.Column<Guid>(type: "uuid", nullable: true),
                    CumulativeProviderReversalUnits = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_economy_funding_claims", x => x.SourceStampId);
                    table.CheckConstraint("ck_economy_funding_claims_amount_positive", "\"AuthoritativeUsdMinorUnits\" > 0");
                    table.CheckConstraint("ck_economy_funding_claims_lifecycle", "(\"State\" = 1 AND \"ConfirmedAt\" IS NULL AND \"StateChangedAt\" = \"ObservedAt\" AND \"PostingGroupId\" IS NULL AND \"RootCreditLotId\" IS NULL AND \"CumulativeProviderReversalUnits\" = 0) OR (\"State\" = 2 AND \"ConfirmedAt\" >= \"ObservedAt\" AND \"StateChangedAt\" >= \"ConfirmedAt\" AND \"PostingGroupId\" IS NOT NULL AND \"RootCreditLotId\" IS NOT NULL) OR (\"State\" IN (3, 4) AND \"ConfirmedAt\" IS NULL AND \"StateChangedAt\" >= \"ObservedAt\" AND \"PostingGroupId\" IS NULL AND \"RootCreditLotId\" IS NULL AND \"CumulativeProviderReversalUnits\" = 0) OR (\"State\" IN (5, 6) AND \"ConfirmedAt\" >= \"ObservedAt\" AND \"StateChangedAt\" >= \"ConfirmedAt\" AND \"PostingGroupId\" IS NOT NULL AND \"RootCreditLotId\" IS NOT NULL AND \"CumulativeProviderReversalUnits\" > 0)");
                    table.CheckConstraint("ck_economy_funding_claims_provider_reversal_bounds", "\"CumulativeProviderReversalUnits\" >= 0 AND \"CumulativeProviderReversalUnits\" <= \"AuthoritativeUsdMinorUnits\"");
                    table.CheckConstraint("ck_economy_funding_claims_version_positive", "\"Version\" > 0");
                    table.ForeignKey(
                        name: "FK_economy_funding_claims_economy_credit_lots_RootCreditLotId",
                        column: x => x.RootCreditLotId,
                        principalTable: "economy_credit_lots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_funding_claims_economy_posting_groups_PostingGroupId",
                        column: x => x.PostingGroupId,
                        principalTable: "economy_posting_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_funding_claims_economy_source_stamps_SourceStampId",
                        column: x => x.SourceStampId,
                        principalTable: "economy_source_stamps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_economy_funding_claims_economy_wallets_WalletId",
                        column: x => x.WalletId,
                        principalTable: "economy_wallets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_source_stamps_confirmation",
                table: "economy_source_stamps",
                sql: "(\"State\" IN (2, 5, 6) AND \"ConfirmedAt\" IS NOT NULL AND \"ConfirmedAt\" >= \"ObservedAt\") OR (\"State\" IN (1, 3, 4) AND \"ConfirmedAt\" IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_economy_funding_claims_WalletId",
                table: "economy_funding_claims",
                column: "WalletId");

            migrationBuilder.CreateIndex(
                name: "ux_economy_funding_claims_posting_group",
                table: "economy_funding_claims",
                column: "PostingGroupId",
                unique: true,
                filter: "\"PostingGroupId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ux_economy_funding_claims_provider_leg",
                table: "economy_funding_claims",
                columns: new[] { "Provider", "Environment", "ConnectedAccount", "ProviderObject", "ProviderMonetaryLeg" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_economy_funding_claims_root_lot",
                table: "economy_funding_claims",
                column: "RootCreditLotId",
                unique: true,
                filter: "\"RootCreditLotId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "economy_funding_claims");

            migrationBuilder.DropCheckConstraint(
                name: "ck_economy_source_stamps_confirmation",
                table: "economy_source_stamps");

            migrationBuilder.AddCheckConstraint(
                name: "ck_economy_source_stamps_confirmation",
                table: "economy_source_stamps",
                sql: "(\"State\" = 2 AND \"ConfirmedAt\" IS NOT NULL AND \"ConfirmedAt\" >= \"ObservedAt\") OR (\"State\" <> 2 AND \"ConfirmedAt\" IS NULL)");
        }
    }
}
