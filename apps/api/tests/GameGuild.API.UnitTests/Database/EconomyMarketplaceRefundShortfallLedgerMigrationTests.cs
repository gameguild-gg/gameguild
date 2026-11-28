using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Finance.Economy.Persistence;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;

namespace GameGuild.API.UnitTests.Database;

// #1092 Tier 3: economy_marketplace_refund_debts is the
// MarketplaceRefundShortfall stream (StreamKind = 16) of economy_events, and
// the marketplace refund function appends to the ledger.
[Collection(PostgreSqlTestCollection.Name)]
public sealed class EconomyMarketplaceRefundShortfallLedgerMigrationTests
{
    private const string BeforeConsolidation = "20261128112714_ConsolidateEconomyTier3RiskAuditEvidence";
    private const string LegacyTable = "public.economy_marketplace_refund_debts";
    private const string Writer = "post_marketplace_refund_v1";

    private static readonly Guid Tenant = new("a6000000-0000-0000-0000-0000000000c1");
    private static readonly Guid Settlement = new("a6000000-0000-0000-0000-000000000010");
    private static readonly Guid SellerWallet = new("a6000000-0000-0000-0000-0000000000a2");
    private static readonly Guid Refund = new("a6000000-0000-0000-0000-000000000011");
    private static readonly Guid FirstShortfall = new("a6000000-0000-0000-0000-000000000021");
    private static readonly Guid SecondShortfall = new("a6000000-0000-0000-0000-000000000022");
    private static readonly Guid ShortfallAfterUpgrade = new("a6000000-0000-0000-0000-000000000023");

    private const string LegacyRows = """
        SELECT "Id" || '|' || "TenantId" || '|' || "RefundId" || '|' || "SettlementId" || '|' ||
               "ResponsibleWalletId" || '|' || "Currency" || '|' || "AmountUnits" || '|' ||
               "EvidenceHash" || '|' || "RecordedAt"
        FROM public.economy_marketplace_refund_debts
        ORDER BY "RecordedAt", "Id"
        """;

    private const string LedgerRows = """
        SELECT "Id" || '|' || "TenantId" || '|' || "RefundId" || '|' || "SettlementId" || '|' ||
               "ResponsibleWalletId" || '|' || "DebtCurrency" || '|' || "DebtAmountUnits" || '|' ||
               "EvidenceHash" || '|' || "OccurredAt"
        FROM public.economy_events
        WHERE "StreamKind" = 16
        ORDER BY "OccurredAt", "Id"
        """;

    // What the writer's body looks like, so a round trip can be held to the
    // exact text it started from.
    private const string WriterDigest = $"SELECT md5(pg_get_functiondef('economy_private.{Writer}'::regproc));";

    private const string WriterAddressesTheLegacyTable = $"""
        SELECT prosrc ~ 'economy_marketplace_refund_debts' FROM pg_proc WHERE proname = '{Writer}';
        """;

    private const string WriterAppendsToTheLedger = $"""
        SELECT prosrc ~ 'INSERT INTO public\.economy_events \('
           AND prosrc ~ '"RefundId", "SettlementId", "ResponsibleWalletId",\s+"DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt"\)'
           AND prosrc ~ '\(row->>''id''\)::uuid, 16, 0, p_tenant_id, p_refund_id, p_settlement_id,'
        FROM pg_proc WHERE proname = '{Writer}';
        """;

    private const string FunctionsNamingTheLegacyTable = """
        SELECT count(*) FROM pg_proc p
        JOIN pg_namespace n ON n.oid = p.pronamespace
        WHERE n.nspname = 'economy_private' AND p.prosrc ~ 'economy_marketplace_refund_debts';
        """;

    [DockerFact]
    public async Task RefundShortfallsCarryIntoTheLedgerAndBackWithTheWriterRetargeted()
    {
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("economy_refund_shortfall_ledger");
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
        await ExecuteAsync(connection, """
            INSERT INTO public.economy_marketplace_settlements (
                "Id", "OrderId", "ProductId", "BuyerId", "BuyerWalletId", "SellerId", "SellerWalletId",
                "PlatformFeeWalletId", "PolicyVersion", "CurrencyMode", "Status", "IdempotencyKey", "EntitlementId",
                "RefundHoldUntil", "SettledAt", "UpdatedAt", "Version", "CapabilityReceiptHash", "CapabilityReceiptId",
                "EntitlementStatus", "EvidenceHashes", "FiatCurrencySnapshot", "JournalHash", "JournalSequence",
                "JurisdictionCode", "KillSwitchEpoch", "OrderLineItemId", "OrderSnapshotHash", "PostingId",
                "PriceVersionSnapshot", "ProductPricingVersionId", "Quantity", "RefundedQuantity", "ReserveVersion",
                "RiskDecisionId", "TenantId", "UnitPriceSnapshot")
            VALUES ('a6000000-0000-0000-0000-000000000010', 'a6000000-0000-0000-0000-000000000001',
                    'a6000000-0000-0000-0000-000000000002', 'a6000000-0000-0000-0000-000000000003',
                    'a6000000-0000-0000-0000-0000000000a1', 'a6000000-0000-0000-0000-000000000004',
                    'a6000000-0000-0000-0000-0000000000a2', 'a6000000-0000-0000-0000-0000000000a3',
                    1, 1, 2, 'settlement-key', 'a6000000-0000-0000-0000-000000000006',
                    '2026-11-01T10:10:00Z', '2026-11-01T10:00:00Z', '2026-11-01T10:05:00Z', 2, 'receipt-hash',
                    'a6000000-0000-0000-0000-000000000007', 2, '["settlement-evidence"]', 'USD', 'journal-hash', 7,
                    'US', 0, 'a6000000-0000-0000-0000-000000000008', 'snapshot-hash',
                    'a6000000-0000-0000-0000-000000000009', 1, 'a6000000-0000-0000-0000-00000000000a', 1, 1,
                    1, 'a6000000-0000-0000-0000-00000000000b', 'a6000000-0000-0000-0000-0000000000c1', 1.0);
            INSERT INTO public.economy_marketplace_refunds (
                "Id", "SettlementId", "BuyerId", "IdempotencyKey", "IsFullRefund", "EntitlementRevoked",
                "FirstJournalSequence", "RefundedAt", "CapabilityReceiptHash", "CapabilityReceiptId", "EvidenceHashes",
                "JournalHash", "JurisdictionCode", "KillSwitchEpoch", "MarketplacePolicyVersion", "PolicyVersion",
                "PostingId", "Quantity", "ReasonCode", "ReasonHash", "RefundedQuantity", "ReserveVersion",
                "RiskDecisionId", "TenantId")
            VALUES ('a6000000-0000-0000-0000-000000000011', 'a6000000-0000-0000-0000-000000000010',
                    'a6000000-0000-0000-0000-000000000003', 'refund-key', true, true, 8, '2026-11-01T10:05:00Z',
                    'refund-receipt-hash', 'a6000000-0000-0000-0000-00000000000c', '["refund-evidence"]',
                    'refund-journal-hash', 'US', 0, 1, 1, 'a6000000-0000-0000-0000-00000000000d', 1,
                    'CUSTOMER_REQUEST', 'reason-hash', 1, 1, 'a6000000-0000-0000-0000-00000000000e',
                    'a6000000-0000-0000-0000-0000000000c1');

            -- One refund that left its seller short in two currencies.
            INSERT INTO public.economy_marketplace_refund_debts (
                "Id", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId", "Currency", "AmountUnits",
                "EvidenceHash", "RecordedAt")
            VALUES ('a6000000-0000-0000-0000-000000000021', 'a6000000-0000-0000-0000-0000000000c1',
                    'a6000000-0000-0000-0000-000000000011', 'a6000000-0000-0000-0000-000000000010',
                    'a6000000-0000-0000-0000-0000000000a2', 1, 90, 'reason-hash', '2026-11-01T10:05:00Z'),
                   ('a6000000-0000-0000-0000-000000000022', 'a6000000-0000-0000-0000-0000000000c1',
                    'a6000000-0000-0000-0000-000000000011', 'a6000000-0000-0000-0000-000000000010',
                    'a6000000-0000-0000-0000-0000000000a2', 2, 40, 'reason-hash', '2026-11-01T10:05:00Z');
            """);
        var legacyRows = await ReadRowsAsync(connection, LegacyRows);
        legacyRows.Should().HaveCount(2);
        var writerDigestBefore = await ScalarAsync<string>(connection, WriterDigest);
        (await ScalarAsync<bool>(connection, WriterAddressesTheLegacyTable)).Should().BeTrue();

        await migrator.MigrateAsync();

        // The table is gone and both shortfalls are rows of the ledger, unchanged.
        (await ScalarAsync<string?>(connection, $"SELECT to_regclass('{LegacyTable}')::text;")).Should().BeNull();
        (await ReadRowsAsync(connection, LedgerRows)).Should().Equal(legacyRows);
        (await ScalarAsync<long>(connection, """
            SELECT count(*) FROM public.economy_events
            WHERE "StreamKind" = 16 AND "Sequence" <> 0;
            """)).Should().Be(0);
        (await ScalarAsync<string[]>(connection, """
            SELECT array_agg(indexdef ORDER BY indexname) FROM pg_indexes
            WHERE schemaname = 'public' AND tablename = 'economy_events'
              AND indexname LIKE 'ix_economy_events_refund_shortfall%';
            """)).Should().BeEquivalentTo(
            "CREATE INDEX ix_economy_events_refund_shortfall_refund ON public.economy_events USING btree (\"RefundId\", \"OccurredAt\") WHERE (\"StreamKind\" = 16)",
            "CREATE INDEX ix_economy_events_refund_shortfall_wallet ON public.economy_events USING btree (\"TenantId\", \"ResponsibleWalletId\", \"OccurredAt\") WHERE (\"StreamKind\" = 16)");
        (await ScalarAsync<char>(connection, """
            SELECT confdeltype FROM pg_constraint
            WHERE conname = 'FK_economy_events_economy_marketplace_refunds_RefundId';
            """)).Should().Be('r');

        // The writer appends to the ledger as kind 16, and nothing in
        // economy_private addresses the retired table any more.
        (await ScalarAsync<bool>(connection, WriterAppendsToTheLedger)).Should().BeTrue();
        (await ScalarAsync<bool>(connection, WriterAddressesTheLegacyTable)).Should().BeFalse();
        (await ScalarAsync<long>(connection, FunctionsNamingTheLegacyTable)).Should().Be(0);

        // The model reads the carried rows.
        var carried = await context.Set<EconomyEventRow>()
            .Where(row => row.StreamKind == EconomyEventStreamKind.MarketplaceRefundShortfall)
            .OrderBy(row => row.DebtCurrency)
            .ToListAsync();
        carried.Select(row => (row.Id, row.TenantId, row.RefundId, row.SettlementId, row.ResponsibleWalletId, row.DebtCurrency, row.DebtAmountUnits, row.EvidenceHash))
            .Should().Equal(
                (FirstShortfall, Tenant, Refund, Settlement, SellerWallet, 1, 90L, "reason-hash"),
                (SecondShortfall, Tenant, Refund, Settlement, SellerWallet, 2, 40L, "reason-hash"));
        carried.Should().OnlyContain(row => row.Sequence == 0 && row.OccurredAt == new DateTimeOffset(2026, 11, 1, 10, 5, 0, TimeSpan.Zero));

        // A shortfall recorded after the upgrade, exactly as the retargeted
        // writer records it.
        await ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                "DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")
            VALUES ('{ShortfallAfterUpgrade}', 16, 0, '{Tenant}', '{Refund}', '{Settlement}', '{SellerWallet}',
                    1, 25, 'reason-hash', '2026-11-01T10:06:00Z');
            """);

        // The stream's shape is enforced: a shortfall without its amount, one
        // that is not positive, one carrying an ordinal, one against a refund
        // that does not exist, and another stream carrying this stream's
        // columns are all rejected.
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events ("Id", "StreamKind", "Sequence", "TenantId", "RefundId", "OccurredAt")
            VALUES ('a6000000-0000-0000-0000-000000000024', 16, 0, '{Tenant}', '{Refund}', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                "DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")
            VALUES ('a6000000-0000-0000-0000-000000000025', 16, 0, '{Tenant}', '{Refund}', '{Settlement}',
                    '{SellerWallet}', 1, 0, 'reason-hash', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                "DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")
            VALUES ('a6000000-0000-0000-0000-000000000026', 16, 1, '{Tenant}', '{Refund}', '{Settlement}',
                    '{SellerWallet}', 1, 25, 'reason-hash', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events (
                "Id", "StreamKind", "Sequence", "TenantId", "RefundId", "SettlementId", "ResponsibleWalletId",
                "DebtCurrency", "DebtAmountUnits", "EvidenceHash", "OccurredAt")
            VALUES ('a6000000-0000-0000-0000-000000000027', 16, 0, '{Tenant}',
                    'a6000000-0000-0000-0000-0000000000ff', '{Settlement}', '{SellerWallet}', 1, 25, 'reason-hash', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.ForeignKeyViolation);
        (await Assert.ThrowsAsync<PostgresException>(() => ExecuteAsync(connection, $"""
            INSERT INTO public.economy_events ("Id", "StreamKind", "Sequence", "SessionId", "Kind", "EvidenceHash",
                "RefundId", "OccurredAt")
            VALUES ('a6000000-0000-0000-0000-000000000028', 9, 1, 'a6000000-0000-0000-0000-0000000000e1', 1, 'hash',
                    '{Refund}', now());
            """))).SqlState.Should().Be(PostgresErrorCodes.CheckViolation);

        // Shortfalls were never immutable: their table had no trigger, so
        // kind 16 stays out of the kind-scoped one and a row can be amended.
        await ExecuteAsync(connection, $"""
            UPDATE public.economy_events SET "DebtAmountUnits" = 30
            WHERE "StreamKind" = 16 AND "Id" = '{ShortfallAfterUpgrade}';
            """);
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 16 AND \"DebtAmountUnits\" = 30;"))
            .Should().Be(1);

        await ExecuteAsRoleAsync(connection, "gameguild_economy_runtime",
            "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 16;");

        await migrator.MigrateAsync(BeforeConsolidation);

        // Every shortfall is back in the legacy table — including the one
        // recorded through the ledger after the upgrade, as amended — under
        // the table's own shape: the column order the migration chain gave
        // it, its constraint, its indexes and its grants. No trigger: it
        // never had one.
        (await ScalarAsync<string>(connection, $"SELECT to_regclass('{LegacyTable}')::text;"))
            .Should().Be("economy_marketplace_refund_debts");
        var restoredRows = await ReadRowsAsync(connection, LegacyRows);
        restoredRows.Should().HaveCount(3);
        restoredRows.Take(2).Should().Equal(legacyRows);
        restoredRows[2].Should().Contain($"{ShortfallAfterUpgrade}|").And.Contain("|1|30|reason-hash|");
        (await ScalarAsync<long>(connection, "SELECT count(*) FROM public.economy_events WHERE \"StreamKind\" = 16;"))
            .Should().Be(0);
        (await ScalarAsync<string>(connection, """
            SELECT string_agg(attname, ',' ORDER BY attnum) FROM pg_attribute
            WHERE attrelid = 'public.economy_marketplace_refund_debts'::regclass AND attnum > 0 AND NOT attisdropped;
            """)).Should().Be("Id,TenantId,RefundId,SettlementId,ResponsibleWalletId,Currency,AmountUnits,EvidenceHash,RecordedAt");
        (await ScalarAsync<string[]>(connection, """
            SELECT array_agg(name ORDER BY name) FROM (
                SELECT 'constraint:' || conname AS name
                FROM pg_constraint
                WHERE conrelid = 'public.economy_marketplace_refund_debts'::regclass
                UNION ALL
                SELECT 'index:' || indexname AS name
                FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = 'economy_marketplace_refund_debts'
                  AND indexname <> 'PK_economy_marketplace_refund_debts'
                UNION ALL
                SELECT 'trigger:' || tgname AS name
                FROM pg_trigger
                WHERE tgrelid = 'public.economy_marketplace_refund_debts'::regclass AND NOT tgisinternal
            ) names;
            """)).Should().BeEquivalentTo(
            "constraint:FK_economy_marketplace_refund_debts_economy_marketplace_refund~",
            "constraint:FK_economy_marketplace_refund_debts_economy_marketplace_settle~",
            "constraint:PK_economy_marketplace_refund_debts",
            "constraint:ck_economy_marketplace_refund_debts_amount",
            "index:IX_economy_marketplace_refund_debts_RefundId",
            "index:IX_economy_marketplace_refund_debts_SettlementId",
            "index:IX_economy_marketplace_refund_debts_TenantId_ResponsibleWallet~");
        (await ScalarAsync<string>(connection, """
            SELECT pg_get_constraintdef(oid) FROM pg_constraint WHERE conname = 'ck_economy_marketplace_refund_debts_amount';
            """)).Should().Be("CHECK ((\"AmountUnits\" > 0))");
        (await ScalarAsync<bool>(connection, """
            SELECT has_table_privilege('gameguild_economy_runtime', 'public.economy_marketplace_refund_debts', 'SELECT')
                AND has_table_privilege('gameguild_economy_procedure_owner', 'public.economy_marketplace_refund_debts', 'INSERT')
                AND NOT has_table_privilege('gameguild_economy_runtime', 'public.economy_marketplace_refund_debts', 'INSERT');
            """)).Should().BeTrue();

        // The writer is back to the exact text it had.
        (await ScalarAsync<string>(connection, WriterDigest)).Should().Be(writerDigestBefore);
        (await ScalarAsync<bool>(connection, WriterAddressesTheLegacyTable)).Should().BeTrue();

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
