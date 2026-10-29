using GameGuild.API.Database;
using GameGuild.API.IntegrationTests.Infrastructure;
using GameGuild.Compliance.Audit;
using GameGuild.CQRS;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Tenants;
using GameGuild.TestSupport.Finance.Economy;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Internal;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;

namespace GameGuild.API.IntegrationTests;

[Collection(ApiPostgreSqlCollection.Name)]
public sealed class AuditSnapshotMigrationPostgreSqlTests(ApiPostgreSqlFixture fixture)
{
    private const string MigrationId = "20261024010000_AddMutationValueSnapshotsAndChainOfCustody";

    private static readonly Type[] AuditStores =
    [
        typeof(MutationValueSnapshot), typeof(MutationValuePayloadChunk),
        typeof(MutationValueSnapshotArchive),
        typeof(ValueSnapshotRetentionPolicy), typeof(ValueSnapshotRetentionExecution),
        typeof(ValueSnapshotRollbackPlan), typeof(CustodyEvidenceItem), typeof(CustodyEvent),
        typeof(EvidenceCustodian), typeof(EvidenceTransfer), typeof(LegalHold),
        typeof(LegalHoldNotification), typeof(CustodyAccessLog)
    ];

    [Fact]
    public async Task UpgradeRepairsDefaultCaptureAndRollbackPreservesExistingRecords()
    {
        using var hostScope = fixture.Factory.Services.CreateScope();
        var services = hostScope.ServiceProvider;
        _ = services.GetRequiredService<ApplicationDbContext>().Model;
        var operation = services.GetRequiredService<IUseCaseOperationContextAccessor>();
        await using var database = await EconomyPostgreSqlTestDatabase.CreateAsync("audit_snapshot_migration");
        // Historical upgrades intentionally precede the current model. The dedicated snapshot
        // test below checks the new audit stores against the native host model independently.
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(database.ConnectionString)
            .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning))
            .Options;
        await using var schema = new ApplicationDbContext(options);
        var migrations = schema.Database.GetMigrations().ToList();
        Assert.Contains(MigrationId, migrations);
        var predecessor = migrations[migrations.IndexOf(MigrationId) - 1];
        var migrator = schema.GetService<IMigrator>();
        await migrator.MigrateAsync(predecessor);

        var marker = "audit-schema-" + Guid.NewGuid().ToString("N");
        schema.Set<DataProtectionKey>().Add(new DataProtectionKey { FriendlyName = marker, Xml = "<test />" });
        await schema.SaveChangesAsync();

        // Reproduce the production failure on the fully migrated previous schema using the
        // same snapshot-enabled constructor as the host, not the options-only test shortcut.
        await using (var before = CreateCaptureContext(options, operation))
        {
            before.Set<Tenant>().Add(CreateTenant());
            var error = await Assert.ThrowsAsync<DbUpdateException>(() => before.SaveChangesAsync());
            var postgres = Assert.IsType<PostgresException>(error.InnerException);
            Assert.Equal(PostgresErrorCodes.UndefinedTable, postgres.SqlState);
            Assert.Contains("MutationValueSnapshots", postgres.MessageText, StringComparison.Ordinal);
        }

        await migrator.MigrateAsync(MigrationId);
        await AssertStoresReadableAsync(schema);
        var persisted = CreateTenant();
        await using (var after = CreateCaptureContext(options, operation))
        {
            after.Set<Tenant>().Add(persisted);
            Assert.Equal(1, await after.SaveChangesAsync());
        }

        var snapshot = Assert.Single(await schema.Set<MutationValueSnapshot>().IgnoreQueryFilters()
            .Where(row => row.EntityName == nameof(Tenant) && row.KeyId == persisted.Id.ToString()).ToListAsync());
        Assert.Equal("Added", snapshot.ChangeKind);
        Assert.NotNull(snapshot.AfterPayload);
        Assert.NotNull(snapshot.AfterStoredSha256);

        var rolledBack = CreateTenant();
        await using (var after = CreateCaptureContext(options, operation))
        {
            await using var transaction = await after.Database.BeginTransactionAsync();
            after.Set<Tenant>().Add(rolledBack);
            await after.SaveChangesAsync();
            await transaction.RollbackAsync();
        }
        Assert.False(await schema.Set<Tenant>().IgnoreQueryFilters().AnyAsync(row => row.Id == rolledBack.Id));
        Assert.False(await schema.Set<MutationValueSnapshot>().IgnoreQueryFilters()
            .AnyAsync(row => row.KeyId == rolledBack.Id.ToString()));

        // Down removes only these new stores. Existing business and data-protection records survive.
        await migrator.MigrateAsync(predecessor);
        Assert.True(await schema.Set<DataProtectionKey>().AnyAsync(row => row.FriendlyName == marker));
        Assert.True(await schema.Set<Tenant>().IgnoreQueryFilters().AnyAsync(row => row.Id == persisted.Id));
        foreach (var type in AuditStores)
        {
            var table = schema.Model.FindEntityType(type)!.GetTableName()!;
            Assert.Equal(0, await schema.Database.SqlQuery<int>($"""
                SELECT COUNT(*)::int AS "Value" FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = {table}
                """).SingleAsync());
        }
        await migrator.MigrateAsync(MigrationId);
        await AssertStoresReadableAsync(schema);
    }

    [Fact]
    public async Task FreshMigratedDatabaseContainsEveryMappedAuditColumn()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        await AssertStoresReadableAsync(scope.ServiceProvider.GetRequiredService<ApplicationDbContext>());
    }

    /// <summary>
    ///     Focused PostgreSQL regression (issues #176): a legal hold keeps held snapshots' complete
    ///     values on the hot rows, and once the hold lifts, the archive tier persists a durable,
    ///     readable archive copy before stripping the hot representation.
    /// </summary>
    [Fact]
    public async Task RetentionKeepsHeldValuesAndArchivesReadablyOnPostgres()
    {
        var tenant = Guid.NewGuid();
        var before = """{"Title":"Held","Secret":"before"}""";
        var after = """{"Title":"Held","Secret":"after"}""";
        await using var scope = fixture.Factory.Services.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Set<MutationValueSnapshot>().Add(new MutationValueSnapshot
        {
            Id = Guid.NewGuid(),
            TenantId = tenant,
            EntityName = nameof(Tenant),
            KeyId = "held-1",
            ChangeKind = "Modified",
            BeforePayload = before,
            AfterPayload = after,
            BeforeStoredSha256 = "b-hash",
            AfterStoredSha256 = "a-hash",
            StructuredDiffJson = """[{"Field":"Secret","Before":"before","After":"after"}]""",
            DiffCount = 1,
            StorageMode = "Delta",
            OccurredAtUtc = DateTimeOffset.UtcNow.AddDays(-30)
        });
        await context.SaveChangesAsync();

        var actors = services.GetRequiredService<IActorContextAccessor>();
        actors.SetActorContext(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = Guid.NewGuid().ToString(),
            TenantId = tenant,
            IsAuthenticated = true,
            Roles = new HashSet<string> { "TenantAdmin" },
            Permissions = new HashSet<string>()
        });
        try
        {
            var retention = services.GetRequiredService<IValueSnapshotRetentionService>();
            await retention.ConfigurePolicyAsync(new ConfigureValueSnapshotRetentionRequest
            {
                RetentionDays = 90,
                ArchiveAfterDays = 7,
                LegalHoldUntilUtc = DateTime.UtcNow.AddDays(10)
            }, default);

            var held = await retention.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), default);
            Assert.True(held.LegalHoldActive);
            Assert.Equal(0, held.ArchivedCount);
            var heldRow = await context.Set<MutationValueSnapshot>().AsNoTracking()
                .SingleAsync(row => row.TenantId == tenant);
            Assert.Equal(before, heldRow.BeforePayload);
            Assert.Equal(after, heldRow.AfterPayload);
            Assert.Null(heldRow.ArchivedAtUtc);
            Assert.False(await context.Set<MutationValueSnapshotArchive>().AnyAsync(row => row.TenantId == tenant));

            // Hold lifts: the same pass now archives — but only after the durable copy persisted.
            await retention.ConfigurePolicyAsync(new ConfigureValueSnapshotRetentionRequest
            {
                RetentionDays = 90,
                ArchiveAfterDays = 7
            }, default);
            var archived = await retention.EnforceAsync(new EnforceValueSnapshotRetentionRequest(), default);
            Assert.Equal(1, archived.ArchivedCount);

            var stripped = await context.Set<MutationValueSnapshot>().AsNoTracking()
                .SingleAsync(row => row.TenantId == tenant);
            Assert.NotNull(stripped.ArchivedAtUtc);
            Assert.Null(stripped.BeforePayload);
            Assert.Null(stripped.AfterPayload);

            var archiveCopy = await context.Set<MutationValueSnapshotArchive>().AsNoTracking()
                .SingleAsync(row => row.TenantId == tenant);
            Assert.Equal(stripped.Id, archiveCopy.SnapshotId);
            Assert.Equal(before, archiveCopy.BeforePayload);
            Assert.Equal(after, archiveCopy.AfterPayload);

            var query = services.GetRequiredService<IValueSnapshotQueryService>();
            var detail = await query.GetAsync(stripped.Id, default);
            Assert.NotNull(detail);
            Assert.Equal(after, detail.AfterPayload);
            var search = await query.SearchAsync(new ValueSnapshotSearchRequest
            {
                EntityName = nameof(Tenant),
                ValuePattern = "after"
            }, default);
            Assert.Equal(1, search.TotalCount);
        }
        finally
        {
            actors.ClearActorContext();
        }
    }

    [Fact]
    public void FullNativeModelMatchesCommittedSnapshot()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        var initialized = db.GetService<IModelRuntimeInitializer>().Initialize(snapshot, designTime: true);
        var current = db.GetService<IDesignTimeModel>().Model;
        var differences = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(initialized.GetRelationalModel(), current.GetRelationalModel());
        // Preserve the failing operations in TRX instead of reporting only a boolean. This
        // diagnoses the full host model while the audit-specific test checks the new stores.
        var diagnostic = System.Text.Json.JsonSerializer.Serialize(differences.Select(DescribeOperation),
            new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        Assert.True(differences.Count == 0, "Native model differs from the committed snapshot:\n" + diagnostic);
    }

    private static object DescribeOperation(MigrationOperation operation) => new
    {
        Operation = operation.GetType().Name,
        Properties = operation.GetType().GetProperties()
            .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
            .ToDictionary(property => property.Name, property => DescribeValue(property.GetValue(operation))),
        Annotations = operation.GetAnnotations().ToDictionary(annotation => annotation.Name,
            annotation => DescribeValue(annotation.Value))
    };

    private static object? DescribeValue(object? value) => value switch
    {
        null => null,
        Type type => type.FullName,
        MigrationOperation operation => DescribeOperation(operation),
        IEnumerable<MigrationOperation> operations => operations.Select(DescribeOperation).ToArray(),
        IEnumerable<string> names => names.ToArray(),
        string or bool or int or long or decimal or double => value,
        _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
    };

    [Fact]
    public void CommittedSnapshotMatchesNativeAuditStorePropertiesAndIndexes()
    {
        using var scope = fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot!.Model;
        foreach (var type in AuditStores)
        {
            var current = db.GetService<IDesignTimeModel>().Model.FindEntityType(type)!;
            var stored = snapshot.FindEntityType(type.FullName!)!;
            Assert.NotNull(stored);
            Assert.Equal(current.GetTableName(), stored.GetTableName());
            Assert.Equal(current.GetSchema(), stored.GetSchema());
            Assert.Equal(Properties(current), Properties(stored));
            Assert.Equal(Indexes(current), Indexes(stored));
        }
    }

    private static string[] Properties(IEntityType entity) => entity.GetProperties()
        .Select(property => string.Join("|", property.Name, property.GetColumnType(), property.IsNullable,
            property.GetMaxLength(), property.IsConcurrencyToken, property.ValueGenerated))
        .Order(StringComparer.Ordinal).ToArray();

    private static string[] Indexes(IEntityType entity) => entity.GetIndexes()
        .Select(index => string.Join(",", index.Properties.Select(property => property.Name)) + ":" + index.IsUnique)
        .Order(StringComparer.Ordinal).ToArray();

    private static ApplicationDbContext CreateCaptureContext(DbContextOptions<ApplicationDbContext> options,
        IUseCaseOperationContextAccessor operation) => new(options, publisher: null,
        useCaseOperationContextAccessor: operation, valueSnapshotOptions: Options.Create(new ValueSnapshotOptions()));

    private static Tenant CreateTenant()
    {
        var marker = Guid.NewGuid().ToString("N");
        return new Tenant { Id = Guid.NewGuid(), Name = "Audit schema " + marker, Slug = "audit-schema-" + marker,
            AdminEmail = "schema-" + marker + "@example.test", IsActive = true };
    }

    private static async Task AssertStoresReadableAsync(ApplicationDbContext db)
    {
        // Historical-upgrade scenarios migrate to an explicit boundary that can predate the
        // migrations introducing newer audit stores; only stores whose creating migration is
        // applied at the current boundary are expected to be readable.
        var applied = (await db.Database.GetAppliedMigrationsAsync()).ToHashSet();
        var assembly = db.GetService<IMigrationsAssembly>();
        var creatingMigrations = assembly.Migrations
            .Select(pair => (Id: pair.Key, Instance: (Migration) Activator.CreateInstance(pair.Value.AsType())!))
            .SelectMany(pair => pair.Instance.UpOperations.OfType<CreateTableOperation>()
                .Select(operation => (Table: operation.Name, pair.Id)))
            .GroupBy(entry => entry.Table, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(entry => entry.Id).ToList(),
                StringComparer.Ordinal);

        foreach (var type in AuditStores)
        {
            var entity = db.Model.FindEntityType(type)!;
            var table = entity.GetTableName()!;
            if (creatingMigrations.TryGetValue(table, out var migrations)
                && migrations.All(id => !applied.Contains(id)))
            {
                continue;
            }

            var store = StoreObjectIdentifier.Table(table, entity.GetSchema());
            var columns = string.Join(", ", entity.GetProperties().Select(property =>
                '"' + property.GetColumnName(store)!.Replace("\"", "\"\"", StringComparison.Ordinal) + '"'));
            // Identifiers come exclusively from the fixed audit-store model, never request input.
            var sql = $"SELECT {columns} FROM \"{table}\" LIMIT 0";
            await db.Database.ExecuteSqlRawAsync(sql);
        }
    }
}
