using GameGuild.API.Database;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260718171325_RollupProviderSecurityConstraints")]
public partial class RollupProviderSecurityConstraints : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE billing_webhook_events
            SET "ProviderEnvironment" = lower("ProviderEnvironment")
            WHERE "ProviderEnvironment" IS NOT NULL
              AND lower("ProviderEnvironment") IN ('test', 'live');

            UPDATE payments
            SET "ProviderEnvironment" = lower("ProviderEnvironment")
            WHERE "ProviderEnvironment" IS NOT NULL
              AND lower("ProviderEnvironment") IN ('test', 'live');

            UPDATE billing_webhook_events
            SET "IsLiveMode" = ("ProviderEnvironment" = 'live')
            WHERE "ProviderEnvironment" IN ('test', 'live')
              AND "IsLiveMode" IS NULL
              AND "ProviderAccountId" IS NOT NULL
              AND "WebhookEndpointId" IS NOT NULL;

            UPDATE billing_webhook_events
            SET "ProviderEnvironment" = CASE WHEN "IsLiveMode" THEN 'live' ELSE 'test' END
            WHERE "ProviderEnvironment" IS NULL
              AND "IsLiveMode" IS NOT NULL
              AND "ProviderAccountId" IS NOT NULL
              AND "WebhookEndpointId" IS NOT NULL;

            DO $$
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM payments
                    WHERE num_nonnulls(
                        "ProviderEnvironment",
                        "ProviderAccountId",
                        "ProviderObjectId",
                        "ProviderObjectType",
                        "ProviderMonetaryLeg") NOT IN (0, 5)
                ) THEN
                    RAISE EXCEPTION 'payments contains an incomplete provider mapping'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM payments
                    WHERE lower("Provider") = 'stripe'
                      AND "Status" IN (1, 2, 5, 6, 7)
                      AND num_nonnulls(
                          "ProviderEnvironment",
                          "ProviderAccountId",
                          "ProviderObjectId",
                          "ProviderObjectType",
                          "ProviderMonetaryLeg") <> 5
                ) THEN
                    RAISE EXCEPTION 'value-relevant Stripe payments require verified provider mapping reconciliation'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM payments
                    WHERE "ProviderEnvironment" IS NOT NULL
                      AND "ProviderEnvironment" NOT IN ('test', 'live')
                ) THEN
                    RAISE EXCEPTION 'payments contains a non-canonical provider environment'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM billing_webhook_events
                    WHERE num_nonnulls(
                        "ProviderEnvironment",
                        "ProviderAccountId",
                        "WebhookEndpointId",
                        "IsLiveMode") NOT IN (0, 4)
                ) THEN
                    RAISE EXCEPTION 'billing webhook inbox contains an incomplete provider scope'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM billing_webhook_events
                    WHERE "ProviderEnvironment" IS NOT NULL
                      AND NOT (
                          ("ProviderEnvironment" = 'live' AND "IsLiveMode" = true)
                          OR ("ProviderEnvironment" = 'test' AND "IsLiveMode" = false)
                      )
                ) THEN
                    RAISE EXCEPTION 'billing webhook inbox contains an inconsistent provider environment'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1 FROM billing_webhook_events
                    WHERE num_nonnulls(
                        "ProviderObjectId",
                        "ProviderObjectType",
                        "ProviderMonetaryLeg") NOT IN (0, 3)
                       OR (
                           "ProviderObjectId" IS NOT NULL
                           AND ("ProviderEnvironment" IS NULL OR "ProviderAccountId" IS NULL)
                       )
                ) THEN
                    RAISE EXCEPTION 'billing webhook inbox contains an incomplete provider object mapping'
                        USING ERRCODE = '23514';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM payments
                    WHERE "ProviderEnvironment" IS NOT NULL
                    GROUP BY
                        "Provider",
                        "ProviderEnvironment",
                        "ProviderAccountId",
                        "ProviderObjectId",
                        "ProviderMonetaryLeg"
                    HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'payments contains duplicate provider monetary legs'
                        USING ERRCODE = '23505';
                END IF;

                IF EXISTS (
                    SELECT 1
                    FROM billing_webhook_events
                    WHERE "ProviderEnvironment" IS NOT NULL
                    GROUP BY
                        "Provider",
                        "ProviderEnvironment",
                        "ProviderAccountId",
                        "WebhookEndpointId",
                        "ExternalEventId"
                    HAVING count(*) > 1
                ) THEN
                    RAISE EXCEPTION 'billing webhook inbox contains duplicate scoped provider events'
                        USING ERRCODE = '23505';
                END IF;
            END
            $$;
            """);

        migrationBuilder.Sql(
            """
            DO $$
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'payments'::regclass
                      AND conname = 'ck_payments_provider_mapping_complete'
                ) THEN
                    ALTER TABLE payments
                        ADD CONSTRAINT ck_payments_provider_mapping_complete
                        CHECK (
                            ("ProviderEnvironment" IS NULL
                             AND "ProviderAccountId" IS NULL
                             AND "ProviderObjectId" IS NULL
                             AND "ProviderObjectType" IS NULL
                             AND "ProviderMonetaryLeg" IS NULL)
                            OR
                            ("ProviderEnvironment" IS NOT NULL
                             AND "ProviderAccountId" IS NOT NULL
                             AND "ProviderObjectId" IS NOT NULL
                             AND "ProviderObjectType" IS NOT NULL
                             AND "ProviderMonetaryLeg" IS NOT NULL)
                        ) NOT VALID;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'payments'::regclass
                      AND conname = 'ck_payments_provider_environment'
                ) THEN
                    ALTER TABLE payments
                        ADD CONSTRAINT ck_payments_provider_environment
                        CHECK (
                            "ProviderEnvironment" IS NULL
                            OR "ProviderEnvironment" IN ('test', 'live')
                        ) NOT VALID;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'payments'::regclass
                      AND conname = 'ck_payments_stripe_value_mapping_required'
                ) THEN
                    ALTER TABLE payments
                        ADD CONSTRAINT ck_payments_stripe_value_mapping_required
                        CHECK (
                            lower("Provider") <> 'stripe'
                            OR "Status" NOT IN (1, 2, 5, 6, 7)
                            OR (
                                "ProviderEnvironment" IS NOT NULL
                                AND "ProviderAccountId" IS NOT NULL
                                AND "ProviderObjectId" IS NOT NULL
                                AND "ProviderObjectType" IS NOT NULL
                                AND "ProviderMonetaryLeg" IS NOT NULL
                            )
                        ) NOT VALID;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'billing_webhook_events'::regclass
                      AND conname = 'ck_billing_webhook_events_provider_scope_complete'
                ) THEN
                    ALTER TABLE billing_webhook_events
                        ADD CONSTRAINT ck_billing_webhook_events_provider_scope_complete
                        CHECK (
                            ("ProviderEnvironment" IS NULL
                             AND "ProviderAccountId" IS NULL
                             AND "WebhookEndpointId" IS NULL
                             AND "IsLiveMode" IS NULL)
                            OR
                            ("ProviderEnvironment" IS NOT NULL
                             AND "ProviderAccountId" IS NOT NULL
                             AND "WebhookEndpointId" IS NOT NULL
                             AND "IsLiveMode" IS NOT NULL)
                        ) NOT VALID;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'billing_webhook_events'::regclass
                      AND conname = 'ck_billing_webhook_events_provider_environment'
                ) THEN
                    ALTER TABLE billing_webhook_events
                        ADD CONSTRAINT ck_billing_webhook_events_provider_environment
                        CHECK (
                            "ProviderEnvironment" IS NULL
                            OR ("ProviderEnvironment" = 'live' AND "IsLiveMode" = true)
                            OR ("ProviderEnvironment" = 'test' AND "IsLiveMode" = false)
                        ) NOT VALID;
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM pg_constraint
                    WHERE conrelid = 'billing_webhook_events'::regclass
                      AND conname = 'ck_billing_webhook_events_provider_object_complete'
                ) THEN
                    ALTER TABLE billing_webhook_events
                        ADD CONSTRAINT ck_billing_webhook_events_provider_object_complete
                        CHECK (
                            ("ProviderObjectId" IS NULL
                             AND "ProviderObjectType" IS NULL
                             AND "ProviderMonetaryLeg" IS NULL)
                            OR
                            ("ProviderObjectId" IS NOT NULL
                             AND "ProviderObjectType" IS NOT NULL
                             AND "ProviderMonetaryLeg" IS NOT NULL
                             AND "ProviderEnvironment" IS NOT NULL
                             AND "ProviderAccountId" IS NOT NULL)
                        ) NOT VALID;
                END IF;
            END
            $$;

            ALTER TABLE payments VALIDATE CONSTRAINT ck_payments_provider_mapping_complete;
            ALTER TABLE payments VALIDATE CONSTRAINT ck_payments_provider_environment;
            ALTER TABLE payments VALIDATE CONSTRAINT ck_payments_stripe_value_mapping_required;
            ALTER TABLE billing_webhook_events VALIDATE CONSTRAINT ck_billing_webhook_events_provider_scope_complete;
            ALTER TABLE billing_webhook_events VALIDATE CONSTRAINT ck_billing_webhook_events_provider_environment;
            ALTER TABLE billing_webhook_events VALIDATE CONSTRAINT ck_billing_webhook_events_provider_object_complete;
            """);

        ReplaceIndex(
            migrationBuilder,
            "ix_billing_webhook_events_external_id_provider",
            """
            CREATE UNIQUE INDEX CONCURRENTLY ix_billing_webhook_events_external_id_provider__rollup
                ON billing_webhook_events ("ExternalEventId", "Provider")
                WHERE "ProviderEnvironment" IS NULL
                  AND "ProviderAccountId" IS NULL
                  AND "WebhookEndpointId" IS NULL;
            """,
            "__rollup");
        ReplaceIndex(
            migrationBuilder,
            "ix_billing_webhook_events_provider_scope_event",
            """
            CREATE UNIQUE INDEX CONCURRENTLY ix_billing_webhook_events_provider_scope_event__rollup
                ON billing_webhook_events
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "WebhookEndpointId", "ExternalEventId")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "WebhookEndpointId" IS NOT NULL;
            """,
            "__rollup");
        ReplaceIndex(
            migrationBuilder,
            "ix_payments_provider_object_leg",
            """
            CREATE UNIQUE INDEX CONCURRENTLY ix_payments_provider_object_leg__rollup
                ON payments
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId", "ProviderMonetaryLeg")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "ProviderObjectId" IS NOT NULL
                  AND "ProviderMonetaryLeg" IS NOT NULL;
            """,
            "__rollup");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ReplaceIndex(
            migrationBuilder,
            "ix_billing_webhook_events_external_id_provider",
            """
            CREATE UNIQUE INDEX CONCURRENTLY ix_billing_webhook_events_external_id_provider__rollback
                ON billing_webhook_events ("ExternalEventId", "Provider");
            """,
            "__rollback");
        ReplaceIndex(
            migrationBuilder,
            "ix_billing_webhook_events_provider_scope_event",
            """
            CREATE INDEX CONCURRENTLY ix_billing_webhook_events_provider_scope_event__rollback
                ON billing_webhook_events
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "WebhookEndpointId", "ExternalEventId")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "WebhookEndpointId" IS NOT NULL;
            """,
            "__rollback");
        ReplaceIndex(
            migrationBuilder,
            "ix_payments_provider_object_leg",
            """
            CREATE INDEX CONCURRENTLY ix_payments_provider_object_leg__rollback
                ON payments
                ("Provider", "ProviderEnvironment", "ProviderAccountId", "ProviderObjectId", "ProviderMonetaryLeg")
                WHERE "ProviderEnvironment" IS NOT NULL
                  AND "ProviderAccountId" IS NOT NULL
                  AND "ProviderObjectId" IS NOT NULL
                  AND "ProviderMonetaryLeg" IS NOT NULL;
            """,
            "__rollback");

        migrationBuilder.Sql(
            """
            ALTER TABLE billing_webhook_events
                DROP CONSTRAINT IF EXISTS ck_billing_webhook_events_provider_object_complete;
            ALTER TABLE billing_webhook_events
                DROP CONSTRAINT IF EXISTS ck_billing_webhook_events_provider_environment;
            ALTER TABLE billing_webhook_events
                DROP CONSTRAINT IF EXISTS ck_billing_webhook_events_provider_scope_complete;
            ALTER TABLE payments
                DROP CONSTRAINT IF EXISTS ck_payments_stripe_value_mapping_required;
            ALTER TABLE payments
                DROP CONSTRAINT IF EXISTS ck_payments_provider_environment;
            ALTER TABLE payments
                DROP CONSTRAINT IF EXISTS ck_payments_provider_mapping_complete;
            """);
    }

    private static void ReplaceIndex(
        MigrationBuilder migrationBuilder,
        string finalName,
        string createTemporarySql,
        string temporarySuffix)
    {
        var temporaryName = finalName + temporarySuffix;
        migrationBuilder.Sql(
            $"DROP INDEX CONCURRENTLY IF EXISTS {temporaryName};",
            suppressTransaction: true);
        migrationBuilder.Sql(createTemporarySql, suppressTransaction: true);
        migrationBuilder.Sql(
            $"DROP INDEX CONCURRENTLY IF EXISTS {finalName};",
            suppressTransaction: true);
        migrationBuilder.Sql(
            $"ALTER INDEX IF EXISTS {temporaryName} RENAME TO {finalName};");
    }
}
