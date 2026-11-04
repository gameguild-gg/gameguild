using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GameGuild.API.Database.Migrations;

/// <summary>
///     Issue #1202 (payments gaps N4): registers the Economy posting capability that lets the
///     top-up webhook consumer reverse HardCoin mints when Stripe refunds a top-up payment.
///     The persisted reversal writer (economy_private.post_provider_reversal_v2, introduced by
///     AddEconomyProviderReversalWriter) requires the resolved capability row to allow template
///     kinds 2, 3, 18, 19 and 20; the seed mirrors the idempotent insert pattern of
///     AddEconomyTopUpProviderSettlement for the confirm-side capability.
/// </summary>
public partial class AddEconomyTopUpRefundReversalCapability : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO public.economy_registered_capabilities
                ("Id", "Name", "AllowedTemplateKinds", "IsEnabled", "CreatedAt", "RevokedAt")
            VALUES
                ('e1000000-0000-0000-0000-000000000015',
                 'economy.reverse-provider-funding.v1', '[2, 3, 18, 19, 20]'::jsonb,
                 true, TIMESTAMPTZ '2026-11-03T00:00:00Z', NULL)
            ON CONFLICT ("Name") DO UPDATE
            SET "AllowedTemplateKinds" = EXCLUDED."AllowedTemplateKinds",
                "IsEnabled" = true,
                "RevokedAt" = NULL;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM public.economy_registered_capabilities
            WHERE "Name" = 'economy.reverse-provider-funding.v1';
            """);
    }
}
