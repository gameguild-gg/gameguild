using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTenantDomainsStructure : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_domain",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_tenant_id",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_unique_primary",
                table: "tenant_domains");

            migrationBuilder.RenameColumn(
                name: "is_primary",
                table: "tenant_domains",
                newName: "is_secondary_domain");

            migrationBuilder.RenameColumn(
                name: "domain",
                table: "tenant_domains",
                newName: "top_level_domain");

            migrationBuilder.AddColumn<bool>(
                name: "is_main_domain",
                table: "tenant_domains",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "subdomain",
                table: "tenant_domains",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_toplevel_subdomain",
                table: "tenant_domains",
                columns: new[] { "top_level_domain", "subdomain" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_unique_main",
                table: "tenant_domains",
                columns: new[] { "tenant_id", "is_main_domain" },
                unique: true,
                filter: "is_main_domain = true");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_toplevel_subdomain",
                table: "tenant_domains");

            migrationBuilder.DropIndex(
                name: "ix_tenant_domains_unique_main",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "is_main_domain",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "subdomain",
                table: "tenant_domains");

            migrationBuilder.RenameColumn(
                name: "top_level_domain",
                table: "tenant_domains",
                newName: "domain");

            migrationBuilder.RenameColumn(
                name: "is_secondary_domain",
                table: "tenant_domains",
                newName: "is_primary");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_domain",
                table: "tenant_domains",
                column: "domain",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_tenant_id",
                table: "tenant_domains",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_tenant_domains_unique_primary",
                table: "tenant_domains",
                columns: new[] { "tenant_id", "is_primary" },
                unique: true,
                filter: "is_primary = true");
        }
    }
}
