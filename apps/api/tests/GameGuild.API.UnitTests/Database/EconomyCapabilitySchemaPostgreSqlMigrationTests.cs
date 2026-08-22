using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyCapabilitySchemaPostgreSqlMigrationTests
{
    private const string PreviousMigration = "20260729193201_RestorePermissionTemplates";
    private const string RollupMigration = "20260803005725_AddEconomyCapabilitySchemaRollup";

    private static readonly string[] ExpectedTables =
    [
        "ai_provider_cost_facts",
        "economy_ad_network_policy_versions",
        "economy_ad_provider_reports",
        "economy_ad_reward_accumulators",
        "economy_ad_reward_attributions",
        "economy_ad_reward_budget_consumptions",
        "economy_ad_reward_completions",
        "economy_ad_reward_reconciliations",
        "economy_bounties",
        "economy_bounty_escrow_fragments",
        "economy_bounty_terminal_events",
        "economy_marketplace_currency_policy_versions",
        "economy_marketplace_funding_fragments",
        "economy_marketplace_refund_legs",
        "economy_marketplace_refunds",
        "economy_marketplace_settlement_credits",
        "economy_marketplace_settlement_legs",
        "economy_marketplace_settlements"
    ];

    [Fact]
    public void MigrationContainsOnlyTheExpectedCapabilityTablesAndIndexes()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new ExposedMigration();

        migration.BuildUp(up);
        migration.BuildDown(down);

        up.Operations.Should().OnlyContain(operation =>
            operation is CreateTableOperation || operation is CreateIndexOperation);
        up.Operations.OfType<CreateTableOperation>().Select(operation => operation.Name)
            .Should().BeEquivalentTo(ExpectedTables);
        down.Operations.Should().OnlyContain(operation => operation is DropTableOperation);
        down.Operations.OfType<DropTableOperation>().Select(operation => operation.Name)
            .Should().BeEquivalentTo(ExpectedTables);
    }

    [DockerFact]
    public async Task UpDownAndCurrentEnforceCapabilityConstraintsOnPostgreSql()
    {
        await using var container = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_capability_rollup");

        var connectionString = container.ConnectionString;
        await using var context = CreateContext(connectionString);
        var migrator = context.GetService<IMigrator>();
        await migrator.MigrateAsync(PreviousMigration);

        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        (await CapabilityTableCountAsync(connection)).Should().Be(0);

        await migrator.MigrateAsync(RollupMigration);
        (await CapabilityTableCountAsync(connection)).Should().Be(ExpectedTables.Length);
        (await ScalarAsync<long>(connection, $$"""
            SELECT count(*)
            FROM "__EFMigrationsHistory"
            WHERE "MigrationId" = '{{RollupMigration}}';
            """)).Should().Be(1);

        await AssertAiCostConstraintsAsync(connection);
        await AssertAdRewardConstraintsAsync(connection);
        await AssertBountyConstraintsAsync(connection);
        await AssertMarketplaceConstraintsAsync(connection);

        await migrator.MigrateAsync(PreviousMigration);
        (await CapabilityTableCountAsync(connection)).Should().Be(0);
        (await ScalarAsync<long>(connection, """
            SELECT count(*)
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND table_name = 'Assessments'
              AND column_name IN ('DefinitionPayload', 'DefinitionSchemaVersion');
            """)).Should().Be(2);

        await migrator.MigrateAsync(RollupMigration);
        await migrator.MigrateAsync(RollupMigration);
        (await CapabilityTableCountAsync(connection)).Should().Be(ExpectedTables.Length);
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        appliedMigrations.Count(migration => migration == RollupMigration).Should().Be(1);
    }

    private static async Task AssertAiCostConstraintsAsync(NpgsqlConnection connection)
    {
        var invalidConservation = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO ai_provider_cost_facts
                ("Id", "AuthorizationId", "RequestId", "TenantId", "ActorId", "ServiceCode", "Provider",
                 "Model", "ProviderUsageId", "InputTokens", "OutputTokens", "TotalTokens",
                 "InputCostUsdNanos", "OutputCostUsdNanos", "ExactProviderCostUsdNanos",
                 "ChargedSoftUnits", "RateCardVersion", "CompletedAt")
            VALUES
                ('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002',
                 '10000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000004',
                 '10000000-0000-0000-0000-000000000005', 'ai.grade', 1, 'model', 'usage-invalid',
                 10, 5, 16, 100, 50, 150, 100000, 'rate-v1', now());
            """));
        invalidConservation.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        invalidConservation.ConstraintName.Should().Be("ck_ai_provider_cost_facts_token_conservation");

        await ExecuteAsync(connection, """
            INSERT INTO ai_provider_cost_facts
                ("Id", "AuthorizationId", "RequestId", "TenantId", "ActorId", "ServiceCode", "Provider",
                 "Model", "ProviderUsageId", "InputTokens", "OutputTokens", "TotalTokens",
                 "InputCostUsdNanos", "OutputCostUsdNanos", "ExactProviderCostUsdNanos",
                 "ChargedSoftUnits", "RateCardVersion", "CompletedAt")
            VALUES
                ('10000000-0000-0000-0000-000000000010', '10000000-0000-0000-0000-000000000020',
                 '10000000-0000-0000-0000-000000000030', '10000000-0000-0000-0000-000000000040',
                 '10000000-0000-0000-0000-000000000050', 'ai.grade', 1, 'model', 'usage-valid',
                 10, 5, 15, 100, 50, 150, 100000, 'rate-v1', now());
            """);
        var duplicateAuthorization = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO ai_provider_cost_facts
                ("Id", "AuthorizationId", "RequestId", "TenantId", "ActorId", "ServiceCode", "Provider",
                 "Model", "ProviderUsageId", "InputTokens", "OutputTokens", "TotalTokens",
                 "InputCostUsdNanos", "OutputCostUsdNanos", "ExactProviderCostUsdNanos",
                 "ChargedSoftUnits", "RateCardVersion", "CompletedAt")
            VALUES
                ('10000000-0000-0000-0000-000000000011', '10000000-0000-0000-0000-000000000020',
                 '10000000-0000-0000-0000-000000000031', '10000000-0000-0000-0000-000000000041',
                 '10000000-0000-0000-0000-000000000051', 'ai.grade', 1, 'model', 'usage-other',
                 1, 1, 2, 10, 10, 20, 1000, 'rate-v1', now());
            """));
        duplicateAuthorization.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    private static async Task AssertAdRewardConstraintsAsync(NpgsqlConnection connection)
    {
        var invalidPpm = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO economy_ad_network_policy_versions
                ("Network", "Version", "EffectiveAt", "ExpiresAt", "IssuanceMode", "YieldState",
                 "EstimatedNetEcpmUsdNanos", "ContractedRevenueSharePpm", "SafetyBufferPpm",
                 "MinimumVisiblePpm", "MaximumFocusLossTicks", "MaximumRewardSoftUnits",
                 "ReportsCurrentThrough", "ReportStaleAfterTicks", "Ranking")
            VALUES
                ('network', 1, now(), now() + interval '1 day', 1, 1, 1000, 1000001, 0, 0, 0, 1,
                 now(), 1, 0);
            """));
        invalidPpm.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        invalidPpm.ConstraintName.Should().Be("ck_economy_ad_network_policy_versions_ppm");
    }

    private static async Task AssertBountyConstraintsAsync(NpgsqlConnection connection)
    {
        await ExecuteAsync(connection, """
            INSERT INTO economy_bounties
                ("Id", "PosterId", "PosterWalletId", "EscrowWalletId", "Currency", "AmountUnits",
                 "ReclaimFeePpm", "RequiresPrerequisite", "MinimumReputation",
                 "RequiresInstructorVerification", "Status", "IdempotencyKey", "PostedAt", "ExpiresAt", "Version")
            VALUES
                ('20000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000002',
                 '20000000-0000-0000-0000-000000000003', '20000000-0000-0000-0000-000000000004',
                 1, 10, 0, false, 0, false, 1, 'bounty-post-1', now(), now() + interval '1 day', 1);
            """);
        var duplicatePost = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO economy_bounties
                ("Id", "PosterId", "PosterWalletId", "EscrowWalletId", "Currency", "AmountUnits",
                 "ReclaimFeePpm", "RequiresPrerequisite", "MinimumReputation",
                 "RequiresInstructorVerification", "Status", "IdempotencyKey", "PostedAt", "ExpiresAt", "Version")
            VALUES
                ('20000000-0000-0000-0000-000000000011', '20000000-0000-0000-0000-000000000012',
                 '20000000-0000-0000-0000-000000000013', '20000000-0000-0000-0000-000000000014',
                 1, 10, 0, false, 0, false, 1, 'bounty-post-1', now(), now() + interval '1 day', 1);
            """));
        duplicatePost.SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
    }

    private static async Task AssertMarketplaceConstraintsAsync(NpgsqlConnection connection)
    {
        var invalidPrice = await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO economy_marketplace_currency_policy_versions
                ("ProductId", "Version", "SellerId", "Mode", "HardPriceUnits", "SoftPriceUnits",
                 "PlatformFeePpm", "EffectiveAt")
            VALUES
                ('30000000-0000-0000-0000-000000000001', 1,
                 '30000000-0000-0000-0000-000000000002', 1, 0, 0, 0, now());
            """));
        invalidPrice.SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        invalidPrice.ConstraintName.Should().Be("ck_economy_marketplace_currency_policy_versions_prices");
    }

    private static ApplicationDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new ApplicationDbContext(options);
    }

    private static async Task<long> CapabilityTableCountAsync(NpgsqlConnection connection) =>
        await ScalarAsync<long>(connection, $$"""
            SELECT count(*)
            FROM pg_tables
            WHERE schemaname = 'public'
              AND tablename = ANY (ARRAY[{{string.Join(", ", ExpectedTables.Select(table => $"'{table}'"))}}]);
            """);

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private sealed class ExposedMigration : AddEconomyCapabilitySchemaRollup
    {
        public void BuildUp(MigrationBuilder builder) => Up(builder);
        public void BuildDown(MigrationBuilder builder) => Down(builder);
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
        }
    }
}
