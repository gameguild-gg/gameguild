using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Commerce.Billing;
using GameGuild.Commerce.Payments;
using GameGuild.Commerce.Products;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class CommerceMigrationDiscoveryPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    private const string SyncMigration = "20261026185918_ReconcileBranchSyncModel";

    private static readonly Type[] SyncStores =
        [typeof(SubscriptionChangeSaga), typeof(SubscriptionChangeSagaStep), typeof(FraudBlacklistEntry)];

    [Fact]
    public async Task MigratedDatabaseContainsThePreviouslyUndiscoveredCommerceSchema()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToArray();
        const string promoMigration = "20261022124500_AddPromoPlanEligibilityConversionSubscriptionApplication";
        Assert.Contains("20261020120000_AddRefundKindAndDisputeFees", applied);
        Assert.Contains(promoMigration, applied);
        Assert.Contains("20261022150000_AddDisputeEvidenceSubmissionTracking", applied);
        Assert.Contains(SyncMigration, applied);

        Type[] stores = [typeof(Refund), typeof(PaymentDispute), typeof(DisputeEvidence),
            typeof(PromoCode), typeof(PromoCodeUse), typeof(PromoCodeValidationAttempt), .. SyncStores];
        await AssertStoresReadableAsync(db, stores);

        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[promoMigration], db.Database.ProviderName!);
        var indexes = migration.UpOperations.OfType<CreateIndexOperation>().ToArray();
        Assert.Equal(7, indexes.Length);
        await AssertIndexesExistAsync(db, indexes);
        var sync = assembly.CreateMigration(assembly.Migrations[SyncMigration], db.Database.ProviderName!);
        await AssertIndexesExistAsync(db, sync.UpOperations.OfType<CreateIndexOperation>());
    }

    [Fact]
    public async Task SagaAndBlacklistUpgradeAndRollbackPreserveExistingData()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        _ = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Model;
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("sync_commerce_migration");
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(database.ConnectionString)
            // This test deliberately visits the historical predecessor; the native model
            // snapshot is checked independently against the full host in the same suite.
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var db = new ApplicationDbContext(options);
        var migrations = db.Database.GetMigrations().ToList();
        Assert.Contains(SyncMigration, migrations);
        var predecessor = migrations[migrations.IndexOf(SyncMigration) - 1];
        var migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);
        var marker = "sync-migration-" + Guid.NewGuid().ToString("N");
        db.Set<DataProtectionKey>().Add(new DataProtectionKey { FriendlyName = marker, Xml = "<test />" });
        await db.SaveChangesAsync();

        await migrator.MigrateAsync(SyncMigration);
        await AssertStoresReadableAsync(db, SyncStores);
        Assert.True(await db.Set<DataProtectionKey>().AnyAsync(row => row.FriendlyName == marker));
        var assembly = db.GetService<IMigrationsAssembly>();
        var migration = assembly.CreateMigration(assembly.Migrations[SyncMigration], db.Database.ProviderName!);
        await AssertIndexesExistAsync(db, migration.UpOperations.OfType<CreateIndexOperation>());

        await migrator.MigrateAsync(predecessor);
        Assert.True(await db.Set<DataProtectionKey>().AnyAsync(row => row.FriendlyName == marker));
        foreach (var type in SyncStores)
        {
            var table = db.Model.FindEntityType(type)!.GetTableName()!;
            Assert.Equal(0, await db.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = {table}
                """).SingleAsync());
        }
        await migrator.MigrateAsync(SyncMigration);
        await AssertStoresReadableAsync(db, SyncStores);
        Assert.True(await db.Set<DataProtectionKey>().AnyAsync(row => row.FriendlyName == marker));
    }

    private static async Task AssertStoresReadableAsync(ApplicationDbContext db, IEnumerable<Type> stores)
    {
        var sqlGeneration = db.GetService<ISqlGenerationHelper>();
        foreach (var type in stores)
        {
            var entity = db.Model.FindEntityType(type)!;
            var table = entity.GetTableName()!;
            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            var columns = string.Join(", ", entity.GetProperties().Select(property =>
                sqlGeneration.DelimitIdentifier(property.GetColumnName(store)!)));
            // SQL parameters cannot represent identifiers. Quote fixed model identifiers with
            // the provider helper; LIMIT 0 still makes PostgreSQL bind every mapped column.
            var sql = $"SELECT {columns} FROM {sqlGeneration.DelimitIdentifier(table, entity.GetSchema())} LIMIT 0";
            await db.Database.ExecuteSqlRawAsync(sql);
        }

    }

    private static async Task AssertIndexesExistAsync(ApplicationDbContext db, IEnumerable<CreateIndexOperation> indexes)
    {
        foreach (var index in indexes)
        {
            var entity = db.Model.GetEntityTypes().Single(type => type.GetTableName() == index.Table);
            Assert.Contains(entity.GetIndexes(), modelIndex => modelIndex.GetDatabaseName() == index.Name);
            Assert.Equal(1, await db.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM pg_indexes
                WHERE schemaname = 'public' AND tablename = {index.Table} AND indexname = {index.Name}
                """).SingleAsync());
        }
    }
}
