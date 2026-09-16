using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations
{
    /// <inheritdoc />
    public partial class FixPaymentEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tax_jurisdictions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ParentJurisdictionId = table.Column<Guid>(type: "uuid", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    TaxRegistrationNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsReverseChargeApplicable = table.Column<bool>(type: "boolean", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_jurisdictions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_jurisdictions_tax_jurisdictions_ParentJurisdictionId",
                        column: x => x.ParentJurisdictionId,
                        principalTable: "tax_jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tax_rates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxJurisdictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    TaxType = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(5,4)", nullable: false),
                    ProductCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    MinimumTaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaximumTaxableAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_rates", x => x.Id);
                    table.CheckConstraint("CK_TaxRate_Rate_Valid", "\"Rate\" >= 0 AND \"Rate\" <= 1");
                    table.ForeignKey(
                        name: "FK_tax_rates_tax_jurisdictions_TaxJurisdictionId",
                        column: x => x.TaxJurisdictionId,
                        principalTable: "tax_jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tax_rules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TaxJurisdictionId = table.Column<Guid>(type: "uuid", nullable: false),
                    RuleType = table.Column<int>(type: "integer", nullable: false),
                    Priority = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CustomerTypeFilter = table.Column<int>(type: "integer", nullable: true),
                    ProductCategories = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    MinimumAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    MaximumAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsTaxInclusive = table.Column<bool>(type: "boolean", nullable: false),
                    IsReverseCharge = table.Column<bool>(type: "boolean", nullable: false),
                    ExemptionConditions = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DefaultTaxRateId = table.Column<Guid>(type: "uuid", nullable: true),
                    Version = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tax_rules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tax_rules_tax_jurisdictions_TaxJurisdictionId",
                        column: x => x.TaxJurisdictionId,
                        principalTable: "tax_jurisdictions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tax_rules_tax_rates_DefaultTaxRateId",
                        column: x => x.DefaultTaxRateId,
                        principalTable: "tax_rates",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "ix_tax_jurisdictions_code",
                table: "tax_jurisdictions",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tax_jurisdictions_is_active",
                table: "tax_jurisdictions",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_tax_jurisdictions_parent_id",
                table: "tax_jurisdictions",
                column: "ParentJurisdictionId");

            migrationBuilder.CreateIndex(
                name: "ix_tax_jurisdictions_type",
                table: "tax_jurisdictions",
                column: "Type");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_effective_from",
                table: "tax_rates",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_effective_to",
                table: "tax_rates",
                column: "EffectiveTo");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_is_active",
                table: "tax_rates",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_jurisdiction_id",
                table: "tax_rates",
                column: "TaxJurisdictionId");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rates_tax_type",
                table: "tax_rates",
                column: "TaxType");

            migrationBuilder.CreateIndex(
                name: "IX_tax_rules_DefaultTaxRateId",
                table: "tax_rules",
                column: "DefaultTaxRateId");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_effective_from",
                table: "tax_rules",
                column: "EffectiveFrom");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_effective_to",
                table: "tax_rules",
                column: "EffectiveTo");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_is_active",
                table: "tax_rules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_jurisdiction_id",
                table: "tax_rules",
                column: "TaxJurisdictionId");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_priority",
                table: "tax_rules",
                column: "Priority");

            migrationBuilder.CreateIndex(
                name: "ix_tax_rules_rule_type",
                table: "tax_rules",
                column: "RuleType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tax_rules");

            migrationBuilder.DropTable(
                name: "tax_rates");

            migrationBuilder.DropTable(
                name: "tax_jurisdictions");
        }
    }
}
