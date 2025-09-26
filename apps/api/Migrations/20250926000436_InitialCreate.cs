using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "require_registration_approval",
                table: "tenant_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "allow_user_registration",
                table: "tenant_settings",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "tenant_settings",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldDefaultValueSql: "gen_random_uuid()");

            migrationBuilder.AddColumn<int>(
                name: "access_level",
                table: "tenant_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "user_group_id",
                table: "tenant_domains",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "tenant_settings_id",
                table: "resource_localizations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_resource_localizations_tenant_settings_id",
                table: "resource_localizations",
                column: "tenant_settings_id");

            migrationBuilder.AddForeignKey(
                name: "fk_resource_localizations_tenant_settings_tenant_settings_id",
                table: "resource_localizations",
                column: "tenant_settings_id",
                principalTable: "tenant_settings",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_resource_localizations_tenant_settings_tenant_settings_id",
                table: "resource_localizations");

            migrationBuilder.DropIndex(
                name: "ix_resource_localizations_tenant_settings_id",
                table: "resource_localizations");

            migrationBuilder.DropColumn(
                name: "access_level",
                table: "tenant_settings");

            migrationBuilder.DropColumn(
                name: "user_group_id",
                table: "tenant_domains");

            migrationBuilder.DropColumn(
                name: "tenant_settings_id",
                table: "resource_localizations");

            migrationBuilder.AlterColumn<bool>(
                name: "require_registration_approval",
                table: "tenant_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "allow_user_registration",
                table: "tenant_settings",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "tenant_settings",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid");
        }
    }
}
