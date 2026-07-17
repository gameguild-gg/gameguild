using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717112000_ExpandProviderSecuritySchema")]
public partial class ExpandProviderSecuritySchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "EventSchemaVersion",
            table: "billing_webhook_events",
            type: "character varying(50)",
            maxLength: 50,
            nullable: true);
        migrationBuilder.AddColumn<bool>(
            name: "IsLiveMode",
            table: "billing_webhook_events",
            type: "boolean",
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderAccountId",
            table: "billing_webhook_events",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderEnvironment",
            table: "billing_webhook_events",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderMonetaryLeg",
            table: "billing_webhook_events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderObjectId",
            table: "billing_webhook_events",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderObjectType",
            table: "billing_webhook_events",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "WebhookEndpointId",
            table: "billing_webhook_events",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "ProviderAccountId",
            table: "payments",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderEnvironment",
            table: "payments",
            type: "character varying(32)",
            maxLength: 32,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderMonetaryLeg",
            table: "payments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderObjectId",
            table: "payments",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);
        migrationBuilder.AddColumn<string>(
            name: "ProviderObjectType",
            table: "payments",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true);

    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        foreach (var column in new[]
                 {
                     "EventSchemaVersion", "IsLiveMode", "ProviderAccountId", "ProviderEnvironment",
                     "ProviderMonetaryLeg", "ProviderObjectId", "ProviderObjectType", "WebhookEndpointId"
                 })
        {
            migrationBuilder.DropColumn(name: column, table: "billing_webhook_events");
        }

        foreach (var column in new[]
                 {
                     "ProviderAccountId", "ProviderEnvironment", "ProviderMonetaryLeg", "ProviderObjectId",
                     "ProviderObjectType"
                 })
        {
            migrationBuilder.DropColumn(name: column, table: "payments");
        }
    }
}
