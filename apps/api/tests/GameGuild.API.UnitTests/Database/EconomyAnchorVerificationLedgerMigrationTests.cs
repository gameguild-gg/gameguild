using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Finance.Economy.Operations;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

// #1092 Tier 3: economy_anchor_verifications is the AnchorVerification stream
// (StreamKind = 14) of economy_events.
[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyAnchorVerificationLedgerMigrationTests
{
    private const string BeforeConsolidation = "20261126205120_ConsolidateEconomyTier3ProjectionEvents";
    private const string LegacyTable = "public.economy_anchor_verifications";

    private static readonly Guid AnchorId = new("a4000000-0000-0000-0000-000000000001");
    private static readonly Guid TenantId = new("a4000000-0000-0000-0000-000000000002");
    private static readonly Guid FirstRun = new("a4000000-0000-0000-0000-000000000011");
    private static readonly Guid SecondRun = new("a4000000-0000-0000-0000-000000000012");
    private static readonly Guid RunAfterUpgrade = new("a4000000-0000-0000-0000-000000000013");

    private const string LegacyRows = """
        SELECT "Id" || '|' || "ExternalAnchorId" || '|' || "KeyId" || '|' || "ObjectVersion" || '|' || "ETag" || '|' ||
               "RetainUntil" || '|' || "ObjectHash" || '|' || "SignatureValid" || '|' || "ObjectMatches" || '|' || "VerifiedAt"
        FROM public.economy_anchor_verifications
        ORDER BY "VerifiedAt", "Id"
        """;

    private const string LedgerRows = """
        SELECT "Id" || '|' || "ExternalAnchorId" || '|' || "KeyId" || '|' || "ObjectVersion" || '|' || "ETag" || '|' ||
               "ExpiresAt" || '|' || "EvidenceHash" || '|' || "SignatureValid" || '|' || "ObjectMatches" || '|' || "OccurredAt"
        FROM public.economy_events
        WHERE "StreamKind" = 14
        ORDER BY "OccurredAt", "Id"
        """;

    [DockerFact]
    public async Task AnchorVerificationsCarryIntoTheLedgerAndBackIncludingRunsRecordedAfterTheUpgrade()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_anchor_verification_ledger");
        await using var context = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(database.ConnectionString)
                .Options);
        var migrator = context.GetService<IMigrator>();
        // Start from the schema before the merge whether the database is empty
        // or was cloned from the migrated template.
        await migrator.MigrateAsync(BeforeConsolidation);

        await using var connection = new NpgsqlConnection(database.ConnectionString);
        await connection.OpenAsync();
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_external_anchors (
                "Id", "JournalSequence", "JournalHash", "Signature", "WormReference", "DispatchSnapshotHash",
                "Provider", "ProviderReference", "AnchoredAt")
            VALUES ('{AnchorId}', 100, 'chain-hash', 'anchor-signature', 'worm/anchor', NULL,
                    's3-object-lock', 'v1', '2026-11-01T10:00:00Z');

            -- One anchor, two runs: the second found the object changed.
            INSERT INTO public.economy_anchor_verifications (
                "Id", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "RetainUntil", "ObjectHash",
                "SignatureValid", "ObjectMatches", "VerifiedAt")
            VALUES ('{FirstRun}', '{AnchorId}', 'kms-key', 'v1', 'etag-1', '2027-11-01T10:00:00Z', 'object-hash-1',
                    true, true, '2026-11-01T10:00:00Z'),
                   ('{SecondRun}', '{AnchorId}', 'kms-key', 'v2', 'etag-2', '2027-11-01T10:05:00Z', 'object-hash-2',
                    true, false, '2026-11-01T10:05:00Z');
            """);
        var legacyRows = await ReadRowsAsync(connection, LegacyRows);
        legacyRows.Should().HaveCount(2);

        await migrator.MigrateAsync();

        // The table is gone and both runs are rows of the ledger, unchanged.
        (await ScalarAsync<string?>(connection, $"SELECT to_regclass('{LegacyTable}')::text;")).Should().BeNull();
        (await ReadRowsAsync(connection, LedgerRows)).Should().Equal(legacyRows);
        (await ScalarAsync<long>(connection, """
            SELECT count(*) FROM public.economy_events
            WHERE "StreamKind" = 14 AND "Sequence" <> 0;
            """)).Should().Be(0);
        (await ScalarAsync<string>(connection, """
            SELECT indexdef FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'economy_events'
              AND indexname = 'ix_economy_events_anchor_verification';
            """)).Should().Contain("(\"ExternalAnchorId\", \"OccurredAt\")").And.Contain("\"StreamKind\" = 14");
        (await ScalarAsync<char>(connection, """
            SELECT confdeltype FROM pg_constraint
            WHERE conname = 'FK_economy_events_economy_external_anchors_ExternalAnchorId';
            """)).Should().Be('r');

        // The readers see the same history through the ledger.
        var reader = new PostgreSqlEconomyLedgerQueryReader(context);
        var history = await reader.ReadAnchorVerificationsAsync(TenantId, AnchorId, default);
        history.Select(item => item.Id).Should().Equal(FirstRun, SecondRun);
        history[1].ObjectMatches.Should().BeFalse();
        history[1].ObjectVersion.Should().Be("v2");
        var summary = (await reader.FindAnchorAsync(TenantId, AnchorId, default))!.Summary;
        summary.ObjectMatches.Should().BeFalse("the newest run is the one an anchor is judged by");
        summary.RetainUntil.Should().Be(new DateTimeOffset(2027, 11, 1, 10, 5, 0, TimeSpan.Zero));

        // A run recorded after the upgrade, exactly as the writers record it.
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "ExpiresAt",
                "EvidenceHash", "SignatureValid", "ObjectMatches", "OccurredAt")
            VALUES ('{RunAfterUpgrade}', 14, 0, '{AnchorId}', 'kms-key', 'v3', 'etag-3', '2027-11-01T10:10:00Z',
                    'object-hash-3', true, true, '2026-11-01T10:10:00Z');
            """);
        (await reader.ReadAnchorVerificationsAsync(TenantId, AnchorId, default))
            .Select(item => item.Id).Should().Equal(FirstRun, SecondRun, RunAfterUpgrade);
        (await reader.FindAnchorAsync(TenantId, AnchorId, default))!.Summary.ObjectMatches.Should().BeTrue();

        // The stream's shape is enforced: a run without its object details, and
        // a run against an anchor that does not exist, are both rejected.
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events ("Id", "StreamKind", "Sequence", "ExternalAnchorId", "OccurredAt")
            VALUES ('a4000000-0000-0000-0000-000000000014', 14, 0, 'a4000000-0000-0000-0000-000000000001', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "ExternalAnchorId", "KeyId", "ObjectVersion", "ETag", "ExpiresAt",
                "EvidenceHash", "SignatureValid", "ObjectMatches", "OccurredAt")
            VALUES ('a4000000-0000-0000-0000-000000000015', 14, 0, 'a4000000-0000-0000-0000-0000000000ff', 'kms-key',
                    'v1', 'etag', now() + interval '1 year', 'hash', true, true, now());
            """))).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        await ExecuteAsRoleAsync(connection, "gameguild_economy_runtime",
            "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 14;");

        await migrator.MigrateAsync(BeforeConsolidation);

        // Every run is back in the legacy table — including the one recorded
        // through the ledger after the upgrade — under the table's own shape.
        (await ScalarAsync<string>(connection, $"SELECT to_regclass('{LegacyTable}')::text;"))
            .Should().Be("economy_anchor_verifications");
        var restoredRows = await ReadRowsAsync(connection, LegacyRows);
        restoredRows.Should().HaveCount(3);
        restoredRows.Take(2).Should().Equal(legacyRows);
        restoredRows[2].Should().Contain($"{RunAfterUpgrade}|").And.Contain("|v3|etag-3|").And.Contain("|object-hash-3|true|true|");
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 14;"))
            .Should().Be(0);
        (await ScalarAsync<string[]>(connection, """
            SELECT array_agg(name ORDER BY name) FROM (
                SELECT 'constraint:' || conname AS name
                FROM pg_constraint
                WHERE conrelid = 'public.economy_anchor_verifications'::regclass
                  AND contype IN ('p', 'f')
                UNION ALL
                SELECT 'index:' || indexname AS name
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'economy_anchor_verifications'
                  AND indexname <> 'PK_economy_anchor_verifications'
            ) names;
            """)).Should().BeEquivalentTo(
            "constraint:FK_economy_anchor_verifications_economy_external_anchors_Exter~",
            "constraint:PK_economy_anchor_verifications",
            "index:IX_economy_anchor_verifications_ExternalAnchorId_VerifiedAt");

        await migrator.MigrateAsync();

        (await ReadRowsAsync(connection, LedgerRows)).Should().Equal(restoredRows);
    }

    private static async Task<string[]> ReadRowsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows.ToArray();
    }

    private static async Task ExecuteAsRoleAsync(NpgsqlConnection connection, string role, string sql)
    {
        await ExecuteAsync(connection, $"SET ROLE {role};");
        try
        {
            await ExecuteAsync(connection, sql);
        }
        finally
        {
            await ExecuteAsync(connection, "RESET ROLE;");
        }
    }

    private static async Task ExecuteAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<T> ScalarAsync<T>(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull
            ? default!
            : (T)value;
    }

    private sealed class DockerFactAttribute : FactAttribute
    {
        public DockerFactAttribute()
        {
            if (string.Equals(Environment.GetEnvironmentVariable("SKIP_DOCKER_TESTS"), "1", StringComparison.Ordinal))
            {
                Skip = "Docker tests disabled by SKIP_DOCKER_TESTS=1.";
            }
        }
    }
}
