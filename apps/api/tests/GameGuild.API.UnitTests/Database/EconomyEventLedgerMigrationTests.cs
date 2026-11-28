using System.Text.RegularExpressions;
using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

// #1092 Tier 2.1 and 2.5: the Economy event and audit tables consolidated into
// economy_events, exercised against the real migration chain. The streams
// covered here are the ones whose mapping is not a plain column rename: the
// ad-reward timeline (two independently numbered streams), the admin withdrawal
// audit chain (keyed by RunId + Sequence instead of Id) and bounty expirations
// (RecordedAt becomes OccurredAt). Everything is raw SQL so the tests depend on
// the migrations alone.
[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyEventLedgerMigrationTests
{
    // The migration immediately before the ledger consolidation.
    private const string BeforeLedger = "20261118214644_EnableEmailSessionMfaEvidence";

    private const string RetiredEventTables =
        "('economy_source_stamp_events', 'economy_provider_dispute_events', 'economy_wallet_debt_events', " +
        "'economy_hold_events', 'economy_risk_review_events', 'economy_legacy_cutover_audit', " +
        "'economy_compliance_hold_events', 'economy_marketplace_events', 'economy_ad_reward_session_timeline', " +
        "'economy_admin_withdrawal_audit_events', 'economy_bounty_expiration_events')";

    private static readonly Guid TenantId = Guid.Parse("97000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);

    [DockerFact]
    public async Task Consolidation_CarriesIndependentKeyedAndRestampedStreamsAndRollsThemBack()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_event_ledger_carry");
        await using var context = CreateContext(database.ConnectionString);
        var migrator = context.GetService<IMigrator>();
        // Start from the pre-consolidation schema whether the database is empty
        // or was cloned from the migrated template.
        await migrator.MigrateAsync(BeforeLedger);

        var sessionId = Guid.NewGuid();
        var bountyId = Guid.NewGuid();
        var expirationId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        var runTenantId = Guid.NewGuid();
        await using (var connection = await OpenAsync(database.ConnectionString))
        {
            await ExecuteAsync(connection, Session(sessionId));
            await ExecuteAsync(connection, Bounty(bountyId));
            await CreateWithdrawalRunAsync(connection, runId, runTenantId);

            // The session timeline held two independently numbered streams: a
            // lifecycle event and a milestone both sit at Sequence 1 and 2.
            await ExecuteAsync(connection, TimelineRow(sessionId, 1, 1, "1, NULL"));
            await ExecuteAsync(connection, TimelineRow(sessionId, 1, 2, "3, NULL"));
            await ExecuteAsync(connection, TimelineRow(sessionId, 2, 1, "NULL, 25"));
            await ExecuteAsync(connection, TimelineRow(sessionId, 2, 2, "NULL, 50"));
            await ExecuteAsync(connection, ExpirationRow(expirationId, bountyId, legacy: true));
            await ExecuteAsync(connection, AuditChain(runId, Now, legacy: true));
        }

        await migrator.MigrateAsync();

        await using var ledger = await OpenAsync(database.ConnectionString);
        (await ScalarAsync<long>(ledger,
            $"SELECT count(*) FROM pg_tables WHERE schemaname = 'public' AND tablename IN {RetiredEventTables};"))
            .Should().Be(0, "the legacy event tables are retired");

        // Each timeline stream becomes its own kind with its sequence intact.
        (await RowsAsync(ledger, $"""
            SELECT 'kind ' || "StreamKind" || ' sequence ' || "Sequence" || ' state ' || coalesce("Kind"::text, '-')
                   || ' percentage ' || coalesce("Percentage"::text, '-')
            FROM public.economy_events WHERE "SessionId" = '{sessionId}' ORDER BY "StreamKind", "Sequence";
            """)).Should().Equal(
            "kind 9 sequence 1 state 1 percentage -",
            "kind 9 sequence 2 state 3 percentage -",
            "kind 10 sequence 1 state - percentage 25",
            "kind 10 sequence 2 state - percentage 50");

        // An expiration is the first entry of its bounty's stream, recorded when it expired.
        (await RowsAsync(ledger, $"""
            SELECT 'kind ' || "StreamKind" || ' sequence ' || "Sequence" || ' version ' || "BountyVersion"
                   || ' recorded ' || to_char("OccurredAt" AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI:SS')
            FROM public.economy_events WHERE "BountyId" = '{bountyId}';
            """)).Should().Equal("kind 12 sequence 1 version 2 recorded 2026-11-26 12:01:00");

        // The audit chain keeps its sequence, kinds and hashes.
        var zeros = new string('0', 64);
        var expectedChain = new[]
        {
            $"sequence 1 kind created previous {zeros} hash hash-1", "sequence 2 kind approved previous hash-1 hash hash-2"
        };
        (await RowsAsync(ledger, AuditChainRows("public.economy_events", "KindText", $" AND \"RunId\" = '{runId}'")))
            .Should().Equal(expectedChain);

        // A row written through the ledger after the upgrade must survive a rollback too.
        await ExecuteAsync(ledger, $"""
            INSERT INTO public.economy_events ("Id", "StreamKind", "SessionId", "Sequence", "Kind", "EvidenceHash", "OccurredAt")
            VALUES ('{Guid.NewGuid()}', 9, '{sessionId}', 3, 4, 'posted-evidence', '{Now.AddMinutes(3):O}');
            """);

        await migrator.MigrateAsync(BeforeLedger);

        (await ScalarAsync<string?>(ledger, "SELECT to_regclass('public.economy_events')::text;")).Should().BeNull();
        (await RowsAsync(ledger, $"""
            SELECT 'timeline ' || "TimelineKind" || ' sequence ' || "Sequence" || ' state ' || coalesce("State"::text, '-')
                   || ' percentage ' || coalesce("Percentage"::text, '-') || ' evidence ' || "EvidenceHash"
            FROM public.economy_ad_reward_session_timeline WHERE "SessionId" = '{sessionId}'
            ORDER BY "TimelineKind", "Sequence";
            """)).Should().Equal(
            "timeline 1 sequence 1 state 1 percentage - evidence timeline-evidence",
            "timeline 1 sequence 2 state 3 percentage - evidence timeline-evidence",
            "timeline 1 sequence 3 state 4 percentage - evidence posted-evidence",
            "timeline 2 sequence 1 state - percentage 25 evidence timeline-evidence",
            "timeline 2 sequence 2 state - percentage 50 evidence timeline-evidence");
        (await RowsAsync(ledger, $"""
            SELECT 'id ' || "Id" || ' version ' || "BountyVersion"
                   || ' recorded ' || to_char("RecordedAt" AT TIME ZONE 'UTC', 'YYYY-MM-DD HH24:MI:SS')
            FROM public.economy_bounty_expiration_events WHERE "BountyId" = '{bountyId}';
            """)).Should().Equal($"id {expirationId} version 2 recorded 2026-11-26 12:01:00");
        (await RowsAsync(ledger, AuditChainRows(
                "public.economy_admin_withdrawal_audit_events", "Kind",
                $" AND \"RunId\" = '{runId}' AND \"TenantId\" = '{runTenantId}'")))
            .Should().Equal(expectedChain);

        // Re-applying the consolidation reads the restored rows back into the ledger.
        await migrator.MigrateAsync();
        (await ScalarAsync<long>(ledger,
            $"SELECT count(*) FROM public.economy_events WHERE \"SessionId\" = '{sessionId}';")).Should().Be(5);
    }

    [DockerFact]
    public async Task ImmutabilityCoversExactlyTheStreamsWhoseTablesRejectedMutation()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_event_ledger_trigger");
        await using var context = CreateContext(database.ConnectionString);
        await context.GetService<IMigrator>().MigrateAsync();

        var bountyId = Guid.NewGuid();
        var runId = Guid.NewGuid();
        await using var connection = await OpenAsync(database.ConnectionString);
        await ExecuteAsync(connection, Bounty(bountyId));
        await CreateWithdrawalRunAsync(connection, runId, Guid.NewGuid());
        // Expirations were never immutable; admin withdrawal audit entries were.
        await ExecuteAsync(connection, ExpirationRow(Guid.NewGuid(), bountyId, legacy: false));
        await ExecuteAsync(connection, AuditChain(runId, Now, legacy: false));

        // One trigger for the seven formerly immutable streams: source stamp,
        // funding hold, risk review, legacy cutover, admin withdrawal audit
        // and, since Tier 3 folded them in, projection reconciliation and risk
        // audit evidence.
        var trigger = Regex.Replace(await ScalarAsync<string>(connection, """
            SELECT pg_get_triggerdef(t.oid) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid
            WHERE c.relname = 'economy_events' AND NOT t.tgisinternal;
            """), @"\s+", "");
        trigger.Should().Contain("BEFOREDELETEORUPDATEONpublic.economy_events");
        trigger.Should().Contain("WHEN((old.\"StreamKind\"=ANY(ARRAY[1,4,5,7,11,13,15])))");

        (await FailsAsync(connection, LedgerMutation("UPDATE", "SET \"Evidence\" = 'rewritten'", 11, runId, bountyId)))
            .Should().Be(("42501", "immutable economy relation economy_events rejects UPDATE"));
        (await FailsAsync(connection, LedgerMutation("DELETE", "", 11, runId, bountyId)))
            .Should().Be(("42501", "immutable economy relation economy_events rejects DELETE"));

        await ExecuteAsync(connection, LedgerMutation("UPDATE", "SET \"BountyVersion\" = 3", 12, runId, bountyId));
        await ExecuteAsync(connection, LedgerMutation("DELETE", "", 12, runId, bountyId));
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 12;"))
            .Should().Be(0);
    }

    [DockerFact]
    public async Task AdRewardStreamsKeepIndependentUniqueSequences()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_event_ledger_streams");
        await using var context = CreateContext(database.ConnectionString);
        await context.GetService<IMigrator>().MigrateAsync();

        var sessionId = Guid.NewGuid();
        await using var connection = await OpenAsync(database.ConnectionString);
        await ExecuteAsync(connection, Session(sessionId));

        // A milestone and the "Issued" lifecycle event are both Sequence = 1.
        await ExecuteAsync(connection, LedgerStreamRow(9, sessionId, "\"Kind\"", "1", "issued-evidence"));
        await ExecuteAsync(connection, LedgerStreamRow(10, sessionId, "\"Percentage\"", "25", "milestone-evidence"));

        (await FailsAsync(connection, LedgerStreamRow(10, sessionId, "\"Percentage\"", "50", "milestone-evidence")))
            .Should().Be((
                "23505",
                "duplicate key value violates unique constraint \"ux_economy_events_ad_reward_milestone_sequence\""));
        (await FailsAsync(connection, LedgerStreamRow(9, sessionId, "\"Kind\"", "3", "issued-evidence")))
            .Should().Be((
                "23505",
                "duplicate key value violates unique constraint \"ux_economy_events_ad_reward_session_sequence\""));
    }

    private static string LedgerStreamRow(int kind, Guid sessionId, string payloadColumn, string payloadValue, string evidence) =>
        $"""
        INSERT INTO public.economy_events ("Id", "StreamKind", "SessionId", "Sequence", {payloadColumn}, "EvidenceHash", "OccurredAt")
        VALUES ('{Guid.NewGuid()}', {kind}, '{sessionId}', 1, {payloadValue}, '{evidence}', '{Now:O}');
        """;

    private static string LedgerMutation(string verb, string mutation, int kind, Guid runId, Guid bountyId)
    {
        var row = kind switch
        {
            11 => $"\"StreamKind\" = 11 AND \"RunId\" = '{runId}'",
            12 => $"\"StreamKind\" = 12 AND \"BountyId\" = '{bountyId}'",
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        return verb switch
        {
            "UPDATE" => $"UPDATE public.economy_events {mutation} WHERE {row};",
            "DELETE" => $"DELETE FROM public.economy_events WHERE {row};",
            _ => throw new ArgumentOutOfRangeException(nameof(verb))
        };
    }

    private static string AuditChainRows(string table, string kindColumn, string filter) =>
        $"""
        SELECT 'sequence ' || "Sequence" || ' kind ' || "{kindColumn}" || ' previous ' || "PreviousHash" || ' hash ' || "Hash"
        FROM {table} WHERE 1 = 1{filter} ORDER BY "Sequence";
        """;

    private static string Session(Guid id) =>
        $"""
        INSERT INTO public.economy_ad_reward_sessions (
            "Id", "TenantId", "UserId", "WalletId", "Network", "PolicyVersion", "CreativeId", "DeviceRiskHash",
            "IpRiskHash", "AsnRiskHash", "NonceHash", "TokenHash", "TokenKeyId", "RequiredDurationTicks", "State",
            "StartIdempotencyKeyHash", "StartRequestHash", "IssuedAt", "ExpiresAt", "UpdatedAt", "Version")
        VALUES ('{id}', '{TenantId}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 'network-a', 1, 'creative', 'device', 'ip',
            'asn', 'nonce', 'token', 'key', 300000000, 1, 'start-key', 'start-request',
            '{Now.AddMinutes(-5):O}', '{Now.AddMinutes(5):O}', '{Now.AddMinutes(-5):O}', 1);
        """;

    private static string TimelineRow(Guid sessionId, int timelineKind, long sequence, string payloadValues) =>
        $"""
        INSERT INTO public.economy_ad_reward_session_timeline (
            "Id", "TimelineKind", "SessionId", "Sequence", "State", "Percentage", "EvidenceHash", "OccurredAt")
        VALUES ('{Guid.NewGuid()}', {timelineKind}, '{sessionId}', {sequence}, {payloadValues}, 'timeline-evidence', '{Now:O}');
        """;

    private static string Bounty(Guid id) =>
        $"""
        INSERT INTO public.economy_bounties (
            "Id", "TenantId", "PosterId", "PosterWalletId", "EscrowWalletId", "Currency", "AmountUnits",
            "ReclaimFeePpm", "RequiresPrerequisite", "MinimumReputation", "RequiresInstructorVerification", "Status",
            "IdempotencyKey", "PostedAt", "ExpiresAt", "Version")
        VALUES ('{id}', '{TenantId}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', '{Guid.NewGuid()}', 1, 100, 10000, false, 0,
            false, 3, 'bounty-{id:N}', '{Now.AddDays(-1):O}', '{Now:O}', 2);
        """;

    // Before the consolidation an expiration is recorded in its own table;
    // afterwards it is the first entry of the bounty's stream in the ledger.
    private static string ExpirationRow(Guid id, Guid bountyId, bool legacy) => legacy
        ? $"""
          INSERT INTO public.economy_bounty_expiration_events ("Id", "BountyId", "ExpiresAt", "RecordedAt", "BountyVersion")
          VALUES ('{id}', '{bountyId}', '{Now:O}', '{Now.AddMinutes(1):O}', 2);
          """
        : $"""
          INSERT INTO public.economy_events (
              "Id", "StreamKind", "BountyId", "ExpiresAt", "Sequence", "OccurredAt", "BountyVersion")
          VALUES ('{id}', 12, '{bountyId}', '{Now:O}', 1, '{Now:O}', 2);
          """;

    // The audit chain of a withdrawal run: created, then approved.
    private static string AuditChain(Guid runId, DateTimeOffset at, bool legacy) => legacy
        ? $"""
          INSERT INTO public.economy_admin_withdrawal_audit_events (
              "TenantId", "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")
          SELECT "TenantId", "Id", 1, 'created', NULL, 'audit-created', '{at:O}', repeat('0', 64), 'hash-1'
          FROM public.economy_operations WHERE "Id" = '{runId}';
          INSERT INTO public.economy_admin_withdrawal_audit_events (
              "TenantId", "RunId", "Sequence", "Kind", "ActorId", "Evidence", "OccurredAt", "PreviousHash", "Hash")
          SELECT "TenantId", "Id", 2, 'approved', NULL, 'audit-approved', '{at:O}', 'hash-1', 'hash-2'
          FROM public.economy_operations WHERE "Id" = '{runId}';
          """
        : $"""
          INSERT INTO public.economy_events (
              "Id", "StreamKind", "TenantId", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt",
              "PreviousHash", "Hash")
          SELECT gen_random_uuid(), 11, "TenantId", "Id", 1, 'created', NULL, 'audit-created', '{at:O}', repeat('0', 64), 'hash-1'
          FROM public.economy_operations WHERE "Id" = '{runId}';
          INSERT INTO public.economy_events (
              "Id", "StreamKind", "TenantId", "RunId", "Sequence", "KindText", "ActorId", "Evidence", "OccurredAt",
              "PreviousHash", "Hash")
          SELECT gen_random_uuid(), 11, "TenantId", "Id", 2, 'approved', NULL, 'audit-approved', '{at:O}', 'hash-1', 'hash-2'
          FROM public.economy_operations WHERE "Id" = '{runId}';
          """;

    private static async Task CreateWithdrawalRunAsync(NpgsqlConnection connection, Guid runId, Guid tenantId)
    {
        var walletId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_wallets ("Id", "OwnerId", "TenantId", "State", "CreatedAt")
            VALUES ('{walletId}', '{actorId}', '{tenantId}', 1, '{Now:O}');
            """);
        await ExecuteAsync(connection, "SET ROLE gameguild_economy_writer;");
        try
        {
            await ExecuteAsync(connection, $"""
                SELECT economy_private.create_admin_withdrawal_run_v2(
                    '{runId}', '{tenantId}', 'withdrawal:2026-11', 'request-hash', '2026-11-01',
                    '{actorId}', '{walletId}', 750, 'reserve:hard:primary', 'destination-hash',
                    1, 1, 1, 1, 1, 1, 1, NULL, NULL, '{Now:O}', '{Now:O}');
                """);
        }
        finally
        {
            await ExecuteAsync(connection, "RESET ROLE;");
        }
    }

    private static ApplicationDbContext CreateContext(string connectionString) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options);

    private static async Task<NpgsqlConnection> OpenAsync(string connectionString)
    {
        var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        return connection;
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
        return value is null or DBNull ? default! : (T)value;
    }

    // Every selected row as one "column value column value ..." line, so rows
    // compare as plain strings.
    private static async Task<List<string>> RowsAsync(NpgsqlConnection connection, string sql)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var rows = new List<string>();
        while (await reader.ReadAsync())
        {
            rows.Add(reader.GetString(0));
        }

        return rows;
    }

    private static async Task<(string SqlState, string Message)> FailsAsync(NpgsqlConnection connection, string sql)
    {
        try
        {
            await ExecuteAsync(connection, sql);
        }
        catch (PostgresException exception)
        {
            return (exception.SqlState, exception.MessageText);
        }

        return (string.Empty, string.Empty);
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
