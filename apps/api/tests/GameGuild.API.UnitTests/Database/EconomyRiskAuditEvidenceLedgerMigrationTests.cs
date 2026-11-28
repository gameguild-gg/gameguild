using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

// #1092 Tier 3: economy_risk_audit_evidence is the RiskAuditEvidence stream
// (StreamKind = 15) of economy_events, and the five functions that appended to
// the table append to the ledger.
[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyRiskAuditEvidenceLedgerMigrationTests
{
    private const string BeforeConsolidation = "20261127094447_ConsolidateEconomyTier3AnchorVerifications";
    private const string LegacyTable = "public.economy_risk_audit_evidence";

    private static readonly Guid FirstDecision = new("a5000000-0000-0000-0000-000000000001");
    private static readonly Guid SecondDecision = new("a5000000-0000-0000-0000-000000000002");
    private static readonly Guid FirstEvidence = new("a5000000-0000-0000-0000-000000000011");
    private static readonly Guid SecondEvidence = new("a5000000-0000-0000-0000-000000000012");
    private static readonly Guid ThirdEvidence = new("a5000000-0000-0000-0000-000000000013");
    private static readonly Guid EvidenceAfterUpgrade = new("a5000000-0000-0000-0000-000000000014");

    private static readonly string[] Writers =
    [
        "issue_self_service_hard_to_soft_risk_decision_v1",
        "post_registered_aggregate_component_v1",
        "post_registered_posting_before_risk_aggregation_v1",
        "post_registered_posting_v1",
        "post_registered_posting_v2"
    ];

    private const string LegacyRows = """
        SELECT "Id" || '|' || "RiskDecisionId" || '|' || "EventKind" || '|' || "OperationFingerprint" || '|' ||
               "EvidenceHash" || '|' || "Payload"::text || '|' || "RecordedAt"
        FROM public.economy_risk_audit_evidence
        ORDER BY "RecordedAt", "Id"
        """;

    private const string LedgerRows = """
        SELECT "Id" || '|' || "RiskDecisionId" || '|' || "KindText" || '|' || "OperationFingerprint" || '|' ||
               "EvidenceHash" || '|' || "Payload"::text || '|' || "OccurredAt"
        FROM public.economy_events
        WHERE "StreamKind" = 15
        ORDER BY "OccurredAt", "Id"
        """;

    // What each writer's body looks like, so a round trip can be held to the
    // exact text it started from.
    private static readonly string WriterDigests = """
        SELECT string_agg(p.proname || '|' || md5(pg_get_functiondef(p.oid)), E'\n' ORDER BY p.proname)
        FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'economy_private'
          AND p.proname IN (
        """ + string.Join(", ", Writers.Select(name => $"'{name}'")) + ");";

    private const string WritersAddressingTheLedgerAsKind15 = """
        SELECT count(*) FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'economy_private'
          AND p.prosrc ~ 'INSERT INTO public\.economy_events \('
          AND p.prosrc ~ '"StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint", "EvidenceHash", "Payload", "OccurredAt"\)'
          AND p.prosrc ~ 'gen_random_uuid\(\), 15, 0, (p_risk_decision_id|selected_decision_id),';
        """;

    private const string WritersAddressingTheLegacyTable = """
        SELECT count(*) FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'economy_private' AND p.prosrc ~ 'economy_risk_audit_evidence';
        """;

    private const string LedgerTrigger = """
        SELECT pg_get_triggerdef(oid) FROM pg_trigger
        WHERE tgrelid = 'public.economy_events'::regclass AND tgname = 'deny_immutable_event_mutation';
        """;

    [DockerFact]
    public async Task RiskAuditEvidenceCarriesIntoTheLedgerAndBackWithItsWritersRetargeted()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_risk_audit_evidence_ledger");
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
        await ExecuteAsync(connection, $$"""
            INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt") VALUES
                ('a5000000-0000-0000-0000-0000000000a1', 'a5000000-0000-0000-0000-0000000000b1', 'a5000000-0000-0000-0000-0000000000c1', 1, '2026-11-01T09:00:00Z'),
                ('a5000000-0000-0000-0000-0000000000a2', 'a5000000-0000-0000-0000-0000000000b2', 'a5000000-0000-0000-0000-0000000000c2', 1, '2026-11-01T09:00:00Z');
            INSERT INTO public.economy_risk_decisions (
                "Id", "Outcome", "OperationFingerprint", "ActorHash", "TemplateKind", "SourceWalletId", "DestinationWalletId",
                "Currency", "AmountUnits", "CurrencyLegs", "SourceRoots", "ProviderReferenceHash", "PolicyVersion",
                "ReserveVersion", "ReserveAuthorizationEpoch", "FeatureVersion", "KillSwitchEpoch", "CounterVersion",
                "EntityGraphVersion", "EntityGraphEvidenceHash", "ReasonCodes", "IssuedAt", "ExpiresAt")
            VALUES ('{{FirstDecision}}', 1, 'spend-fingerprint', 'actor-hash', 4,
                    'a5000000-0000-0000-0000-0000000000a1', 'a5000000-0000-0000-0000-0000000000a2',
                    2, 50, '[]', '[]', 'provider-hash', 1, 1, 1, 1, 0, 1, 0, 'graph-hash', '[]',
                    '2026-11-01T09:30:00Z', '2026-11-01T10:30:00Z'),
                   ('{{SecondDecision}}', 1, 'convert-fingerprint', 'actor-hash', 4,
                    'a5000000-0000-0000-0000-0000000000a1', 'a5000000-0000-0000-0000-0000000000a2',
                    2, 70, '[]', '[]', 'provider-hash', 1, 1, 1, 1, 0, 1, 0, 'graph-hash', '[]',
                    '2026-11-01T09:30:00Z', '2026-11-01T10:30:00Z');

            -- One decision with two journal entries behind it, and one that
            -- relied on external evidence.
            INSERT INTO public.economy_risk_audit_evidence (
                "Id", "RiskDecisionId", "EventKind", "OperationFingerprint", "EvidenceHash", "Payload", "RecordedAt")
            VALUES ('{{FirstEvidence}}', '{{FirstDecision}}', 'posting-authorized', 'spend-fingerprint', 'journal-hash-1',
                    '{"postingId": "a5000000-0000-0000-0000-0000000000d1", "sequence": 41}', '2026-11-01T10:00:00Z'),
                   ('{{SecondEvidence}}', '{{FirstDecision}}', 'posting-authorized', 'spend-fingerprint', 'journal-hash-2',
                    '{"postingId": "a5000000-0000-0000-0000-0000000000d2", "sequence": 42}', '2026-11-01T10:05:00Z'),
                   ('{{ThirdEvidence}}', '{{SecondDecision}}', 'external-risk-evidence', 'convert-fingerprint',
                    'provider-evidence-1',
                    '{"provider": "external-risk", "reference": "ref-1", "level": "elevated"}', '2026-11-01T10:10:00Z');
            """);
        var legacyRows = await ReadRowsAsync(connection, LegacyRows);
        legacyRows.Should().HaveCount(3);
        var writerDigestsBefore = await ScalarAsync<string>(connection, WriterDigests);
        writerDigestsBefore.Split('\n').Should().HaveCount(5);
        (await ScalarAsync<long>(connection, WritersAddressingTheLegacyTable)).Should().Be(5);

        await migrator.MigrateAsync();

        // The table is gone and all three rows are rows of the ledger, unchanged.
        (await ScalarAsync<string?>(connection, $"SELECT to_regclass('{LegacyTable}')::text;")).Should().BeNull();
        (await ReadRowsAsync(connection, LedgerRows)).Should().Equal(legacyRows);
        (await ScalarAsync<long>(connection, """
            SELECT count(*) FROM public.economy_events
            WHERE "StreamKind" = 15 AND "Sequence" <> 0;
            """)).Should().Be(0);
        (await ScalarAsync<string>(connection, """
            SELECT indexdef FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'economy_events'
              AND indexname = 'ux_economy_events_risk_evidence';
            """)).Should().Contain("(\"RiskDecisionId\", \"EvidenceHash\")").And.Contain("\"StreamKind\" = 15");
        (await ScalarAsync<char>(connection, """
            SELECT confdeltype FROM pg_constraint
            WHERE conname = 'FK_economy_events_economy_risk_decisions_RiskDecisionId';
            """)).Should().Be('r');

        // Every writer appends to the ledger as kind 15, and nothing in
        // economy_private addresses the retired table any more.
        (await ScalarAsync<long>(connection, WritersAddressingTheLedgerAsKind15)).Should().Be(5);
        (await ScalarAsync<long>(connection, WritersAddressingTheLegacyTable)).Should().Be(0);

        // The model reads the carried rows.
        var carried = await context.Set<EconomyEventRow>()
            .Where(row => row.StreamKind == EconomyEventStreamKind.RiskAuditEvidence)
            .OrderBy(row => row.OccurredAt)
            .ToListAsync();
        carried.Select(row => (row.Id, row.RiskDecisionId, row.KindText, row.OperationFingerprint, row.EvidenceHash))
            .Should().Equal(
                (FirstEvidence, FirstDecision, "posting-authorized", "spend-fingerprint", "journal-hash-1"),
                (SecondEvidence, FirstDecision, "posting-authorized", "spend-fingerprint", "journal-hash-2"),
                (ThirdEvidence, SecondDecision, "external-risk-evidence", "convert-fingerprint", "provider-evidence-1"));
        carried.Select(row => row.Payload).Should().OnlyContain(payload =>
            payload != null && (payload.Contains("postingId") || payload.Contains("ref-1")));

        // A row recorded after the upgrade, exactly as the retargeted writers
        // record it.
        await ExecuteAsync(connection, $$"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint",
                "EvidenceHash", "Payload", "OccurredAt")
            VALUES ('{{EvidenceAfterUpgrade}}', 15, 0, '{{FirstDecision}}', 'posting-authorized', 'spend-fingerprint',
                    'journal-hash-3', '{"postingId": "a5000000-0000-0000-0000-0000000000d3", "sequence": 43}',
                    '2026-11-01T10:15:00Z');
            """);
        (await ScalarAsync<string>(connection, LedgerTrigger)).Should().Contain("15");

        // The stream's shape is enforced: evidence without its payload, a
        // second entry with the same hash for one decision, and evidence
        // against a decision that does not exist are all rejected, and so is
        // another stream carrying this stream's columns.
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events ("Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OccurredAt")
            VALUES ('a5000000-0000-0000-0000-000000000015', 15, 0, 'a5000000-0000-0000-0000-000000000001', 'posting-authorized', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint",
                "EvidenceHash", "Payload", "OccurredAt")
            VALUES ('a5000000-0000-0000-0000-000000000016', 15, 0, 'a5000000-0000-0000-0000-000000000001', 'posting-authorized',
                    'spend-fingerprint', 'journal-hash-3', '{}', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.UniqueViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "RiskDecisionId", "KindText", "OperationFingerprint",
                "EvidenceHash", "Payload", "OccurredAt")
            VALUES ('a5000000-0000-0000-0000-000000000017', 15, 0, 'a5000000-0000-0000-0000-0000000000ff', 'posting-authorized',
                    'spend-fingerprint', 'journal-hash-4', '{}', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, """
            INSERT INTO public.economy_events ("Id", "StreamKind", "Sequence", "SessionId", "Kind", "EvidenceHash",
                "RiskDecisionId", "OccurredAt")
            VALUES ('a5000000-0000-0000-0000-000000000018', 9, 1, 'a5000000-0000-0000-0000-0000000000e1', 1, 'hash',
                    'a5000000-0000-0000-0000-000000000001', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);

        // Evidence was immutable in its own table and stays immutable: the
        // kind-scoped trigger now covers kind 15.
        foreach (var statement in new[]
                 {
                     "UPDATE public.economy_events SET \"EvidenceHash\" = 'changed' WHERE \"StreamKind\" = 15;",
                     "DELETE FROM public.economy_events WHERE \"StreamKind\" = 15;"
                 })
        {
            (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, statement)))
                .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);
        }

        await ExecuteAsRoleAsync(connection, "gameguild_economy_runtime",
            "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 15;");

        await migrator.MigrateAsync(BeforeConsolidation);

        // Every row is back in the legacy table — including the one recorded
        // through the ledger after the upgrade — under the table's own shape:
        // the column order the migration chain gave it, its index, its
        // trigger, and its grants.
        (await ScalarAsync<string>(connection, $"SELECT to_regclass('{LegacyTable}')::text;"))
            .Should().Be("economy_risk_audit_evidence");
        var restoredRows = await ReadRowsAsync(connection, LegacyRows);
        restoredRows.Should().HaveCount(4);
        restoredRows.Take(3).Should().Equal(legacyRows);
        restoredRows[3].Should().Contain($"{EvidenceAfterUpgrade}|").And.Contain("|journal-hash-3|").And.Contain("\"sequence\": 43");
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 15;"))
            .Should().Be(0);
        (await ScalarAsync<string>(connection, """
            SELECT string_agg(attname, ',' ORDER BY attnum) FROM pg_attribute
            WHERE attrelid = 'public.economy_risk_audit_evidence'::regclass AND attnum > 0 AND NOT attisdropped;
            """)).Should().Be("Id,RiskDecisionId,EventKind,OperationFingerprint,EvidenceHash,Payload,RecordedAt");
        (await ScalarAsync<string[]>(connection, """
            SELECT array_agg(name ORDER BY name) FROM (
                SELECT 'constraint:' || conname AS name
                FROM pg_constraint
                WHERE conrelid = 'public.economy_risk_audit_evidence'::regclass
                  AND contype IN ('p', 'f')
                UNION ALL
                SELECT 'index:' || indexname AS name
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'economy_risk_audit_evidence'
                  AND indexname <> 'PK_economy_risk_audit_evidence'
                UNION ALL
                SELECT 'trigger:' || tgname AS name
                FROM pg_trigger
                WHERE tgrelid = 'public.economy_risk_audit_evidence'::regclass AND NOT tgisinternal
            ) names;
            """)).Should().BeEquivalentTo(
            "constraint:FK_economy_risk_audit_evidence_economy_risk_decisions_RiskDeci~",
            "constraint:PK_economy_risk_audit_evidence",
            "index:ux_economy_risk_audit_evidence_decision_hash",
            "trigger:deny_immutable_mutation");
        (await ScalarAsync<bool>(connection, """
            SELECT has_table_privilege('gameguild_economy_runtime', 'public.economy_risk_audit_evidence', 'SELECT')
                AND has_table_privilege('gameguild_economy_procedure_owner', 'public.economy_risk_audit_evidence', 'INSERT')
                AND NOT has_table_privilege('gameguild_economy_runtime', 'public.economy_risk_audit_evidence', 'INSERT');
            """)).Should().BeTrue();
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection,
                "UPDATE public.economy_risk_audit_evidence SET \"EvidenceHash\" = 'changed';")))
            .SqlState.Should().Be(PostgresErrorCodes.InsufficientPrivilege);

        // The writers are back to the exact text they had, and the ledger's
        // trigger no longer covers kind 15.
        (await ScalarAsync<string>(connection, WriterDigests)).Should().Be(writerDigestsBefore);
        (await ScalarAsync<long>(connection, WritersAddressingTheLegacyTable)).Should().Be(5);
        (await ScalarAsync<string>(connection, LedgerTrigger)).Should().NotContain("15");

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
