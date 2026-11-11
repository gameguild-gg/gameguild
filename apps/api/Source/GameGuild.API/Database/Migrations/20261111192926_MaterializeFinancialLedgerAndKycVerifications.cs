using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class MaterializeFinancialLedgerAndKycVerifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "financial_ledger_entries",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntryType = table.Column<string>(type: "text", nullable: false),
                    DebitAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreditAccount = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DebitLedgerAccount = table.Column<int>(type: "integer", nullable: true),
                    CreditLedgerAccount = table.Column<int>(type: "integer", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    RevenueEventId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsReconciled = table.Column<bool>(type: "boolean", nullable: false),
                    ReconciledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReconciledBy = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    FiscalPeriod = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_financial_ledger_entries", x => x.Id);
                    table.CheckConstraint("CK_FinancialLedgerEntry_Amount_Positive", "amount > 0");
                    table.CheckConstraint("CK_FinancialLedgerEntry_CreditAccount_NotEmpty", "LENGTH(credit_account) > 0");
                    table.CheckConstraint("CK_FinancialLedgerEntry_DebitAccount_NotEmpty", "LENGTH(debit_account) > 0");
                    table.ForeignKey(
                        name: "FK_financial_ledger_entries_revenue_events_RevenueEventId",
                        column: x => x.RevenueEventId,
                        principalTable: "revenue_events",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "user_kyc_verifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ExternalVerificationId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    VerificationLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    DocumentTypes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DocumentCountry = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: true),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ProviderData = table.Column<string>(type: "jsonb", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_kyc_verifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_user_kyc_verifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_CreatedAt",
                table: "financial_ledger_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_CreditAccount",
                table: "financial_ledger_entries",
                column: "CreditAccount");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_DebitAccount",
                table: "financial_ledger_entries",
                column: "DebitAccount");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_EntryType",
                table: "financial_ledger_entries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_FiscalPeriod",
                table: "financial_ledger_entries",
                column: "FiscalPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_FiscalYear",
                table: "financial_ledger_entries",
                column: "FiscalYear");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_IsReconciled",
                table: "financial_ledger_entries",
                column: "IsReconciled");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_ReferenceNumber",
                table: "financial_ledger_entries",
                column: "ReferenceNumber");

            migrationBuilder.CreateIndex(
                name: "IX_financial_ledger_entries_RevenueEventId",
                table: "financial_ledger_entries",
                column: "RevenueEventId");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_ExternalVerificationId",
                table: "user_kyc_verifications",
                column: "ExternalVerificationId");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_Provider",
                table: "user_kyc_verifications",
                column: "Provider");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_Status",
                table: "user_kyc_verifications",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_SubmittedAt",
                table: "user_kyc_verifications",
                column: "SubmittedAt");

            migrationBuilder.CreateIndex(
                name: "IX_user_kyc_verifications_UserId",
                table: "user_kyc_verifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "financial_ledger_entries");

            migrationBuilder.DropTable(
                name: "user_kyc_verifications");
        }
    }
}
