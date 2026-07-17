using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260717112100_CreateProviderSecurityIndexesConcurrently")]
public partial class CreateProviderSecurityIndexesConcurrently : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_billing_webhook_events_provider_object_leg;",
            suppressTransaction: true);
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_billing_webhook_events_provider_object_leg
                ON billing_webhook_events
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId", "ProviderMonetaryLeg")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "ProviderObjectId" IS NOT NULL
                  AND "ProviderMonetaryLeg" IS NOT NULL;
            """,
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_billing_webhook_events_provider_scope_event;",
            suppressTransaction: true);
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_billing_webhook_events_provider_scope_event
                ON billing_webhook_events
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "WebhookEndpointId", "ExternalEventId")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "WebhookEndpointId" IS NOT NULL;
            """,
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_payments_provider_object_leg;",
            suppressTransaction: true);
        migrationBuilder.Sql(
            """
            CREATE INDEX CONCURRENTLY IF NOT EXISTS ix_payments_provider_object_leg
                ON payments
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId", "ProviderMonetaryLeg")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "ProviderObjectId" IS NOT NULL
                  AND "ProviderMonetaryLeg" IS NOT NULL;
            """,
            suppressTransaction: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_billing_webhook_events_provider_object_leg;",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_billing_webhook_events_provider_scope_event;",
            suppressTransaction: true);
        migrationBuilder.Sql(
            "DROP INDEX CONCURRENTLY IF EXISTS ix_payments_provider_object_leg;",
            suppressTransaction: true);
    }
}
