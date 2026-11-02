using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.Compliance.Audit;
using GameGuild.Identity.Authentication;
using GameGuild.Identity.Users;
using GameGuild.Projects;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Options;
using Moq;

namespace GameGuild.API.UnitTests.Database;

public sealed class EntityChangeAuditCaptureTests
{
    [Fact]
    public async Task ModifiedEntity_FromProjectsModule_ProducesAuditRowWithBeforeAndAfterValues()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        var project = new Project { Title = "Before title", Slug = "before-slug" };
        context.Add(project);
        await context.SaveChangesAsync();

        project.Title = "After title";
        await context.SaveChangesAsync();

        var record = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Modified");
        record.EntityName.Should().Be("Project");
        record.KeyId.Should().Be(project.Id.ToString());
        record.BeforeValues.Should().Contain("Before title").And.NotContain("After title");
        record.AfterValues.Should().Contain("After title").And.NotContain("Before title");
    }

    [Fact]
    public async Task ModifiedEntity_FromIdentityModule_ProducesAuditRowWithActorAndTenantContext()
    {
        var options = NewInMemoryOptions();
        var operationAccessor = new UseCaseOperationContextAccessor();
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var correlationId = Guid.NewGuid();
        await using var context = new ApplicationDbContext(options, null, operationAccessor);
        var user = new User { Email = "audited@example.com", Name = "Before name" };
        context.Add(user);
        await context.SaveChangesAsync();

        using (operationAccessor.Begin(new UseCaseOperationContext(
            "operation-code", "TestCommand", tenantId, actorId, correlationId, null)))
        {
            user.Name = "After name";
            await context.SaveChangesAsync();
        }

        var record = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Modified");
        record.EntityName.Should().Be("User");
        record.TenantId.Should().Be(tenantId);
        record.ActorId.Should().Be(actorId);
        record.CorrelationId.Should().Be(correlationId.ToString());
        record.BeforeValues.Should().Contain("Before name");
        record.AfterValues.Should().Contain("After name");
    }

    [Fact]
    public async Task SensitiveProperties_AreRedactedInAuditSnapshots()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        var user = new User { Email = "secret-user@example.com", Name = "Secret user", PasswordHash = "bcrypt-secret-hash-value" };
        context.Add(user);
        await context.SaveChangesAsync();

        var added = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Added");
        added.AfterValues.Should().Contain("[REDACTED]").And.NotContain("bcrypt-secret-hash-value");

        user.PasswordHash = "bcrypt-rotated-secret-hash";
        await context.SaveChangesAsync();

        var modified = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Modified");
        modified.BeforeValues.Should().Contain("[REDACTED]");
        modified.AfterValues.Should().Contain("[REDACTED]").And.NotContain("bcrypt-rotated-secret-hash");
    }

    [Fact]
    public async Task AuditStoreEntities_AreExcludedFromCapture()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        context.Add(new AuditLog { ActionType = "audit.read", ResourceType = "AuditLog" });
        await context.SaveChangesAsync();

        (await AuditRecords(context).CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AddedAndDeletedChangeKinds_AreCapturedWithRespectiveSnapshots()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        var project = new Project { Title = "Lifecycle project", Slug = "lifecycle-project" };
        context.Add(project);
        await context.SaveChangesAsync();

        var added = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Added");
        added.EntityName.Should().Be("Project");
        added.KeyId.Should().Be(project.Id.ToString());
        added.BeforeValues.Should().BeNull();
        added.AfterValues.Should().Contain("Lifecycle project");

        context.Remove(project);
        await context.SaveChangesAsync();

        var deleted = await AuditRecords(context).SingleAsync(record => record.ChangeKind == "Deleted");
        deleted.KeyId.Should().Be(project.Id.ToString());
        deleted.BeforeValues.Should().Contain("Lifecycle project");
        deleted.AfterValues.Should().BeNull();
    }

    [Fact]
    public async Task BulkChanges_ProduceOneAuditRowPerEntity()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        var projects = Enumerable.Range(1, 3)
            .Select(index => new Project { Title = $"Bulk project {index}", Slug = $"bulk-project-{index}" })
            .ToList();
        context.AddRange(projects);
        await context.SaveChangesAsync();

        (await AuditRecords(context).CountAsync(record => record.ChangeKind == "Added")).Should().Be(3);

        foreach (var project in projects)
        {
            project.Title = $"{project.Title} updated";
        }

        await context.SaveChangesAsync();

        (await AuditRecords(context).CountAsync(record => record.ChangeKind == "Modified")).Should().Be(3);
    }

    [Fact]
    public async Task SaveChanges_ReturnsBusinessAffectedRows_ExcludingStagedAuditRecords()
    {
        var options = NewInMemoryOptions();
        await using var context = new ApplicationDbContext(options);
        context.Add(new Project { Title = "Counted project", Slug = "counted-project" });

        var affectedRows = await context.SaveChangesAsync();

        affectedRows.Should().Be(1);
        (await AuditRecords(context).CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RolledBackTransaction_LeavesNoAuditRows()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new ApplicationDbContext(options);
        await CreateTableAsync(context, connection, typeof(Project));
        await CreateTableAsync(context, connection, typeof(EntityChangeAuditRecord));

        await using var transaction = await context.Database.BeginTransactionAsync();
        context.Add(new Project { Title = "Rolled back project", Slug = "rolled-back-project" });
        await context.SaveChangesAsync();
        (await AuditRecords(context).CountAsync()).Should().Be(1);

        await transaction.RollbackAsync();

        (await AuditRecords(context).CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task ConfiguredExclusionList_SkipsOnlyListedEntityTypes()
    {
        var options = NewInMemoryOptions();
        var captureOptions = Options.Create(new EntityChangeAuditOptions
        {
            ExcludedEntityTypes = { "Project" }
        });
        await using var context = new ApplicationDbContext(
            options, null, new UseCaseOperationContextAccessor(), entityChangeAuditOptions: captureOptions);
        context.Add(new Project { Title = "Excluded project", Slug = "excluded-project" });
        context.Add(new User { Email = "captured@example.com", Name = "Captured user" });
        await context.SaveChangesAsync();

        var records = await AuditRecords(context).ToListAsync();
        records.Should().ContainSingle(record => record.EntityName == "User");
        records.Should().NotContain(record => record.EntityName == "Project");
    }

    [Fact]
    public async Task Capture_IsEnabledByDefault_AndCanBeExplicitlyOptedOut()
    {
        // Default (no configuration bound): capture stays on — a missing config section can
        // never silently drop universal coverage.
        var defaultOptions = NewInMemoryOptions();
        await using (var defaultContext = new ApplicationDbContext(defaultOptions, null, new UseCaseOperationContextAccessor()))
        {
            defaultContext.Add(new Project { Title = "Default captured", Slug = "default-captured" });
            await defaultContext.SaveChangesAsync();
            (await AuditRecords(defaultContext).CountAsync()).Should().Be(1);
        }

        var optedOutOptions = NewInMemoryOptions();
        var disabledOptions = Options.Create(new EntityChangeAuditOptions { Enabled = false });
        await using (var optedOutContext = new ApplicationDbContext(
            optedOutOptions, null, new UseCaseOperationContextAccessor(), entityChangeAuditOptions: disabledOptions))
        {
            optedOutContext.Add(new Project { Title = "Not captured", Slug = "not-captured" });
            await optedOutContext.SaveChangesAsync();
            (await AuditRecords(optedOutContext).CountAsync()).Should().Be(0);
        }
    }

    [Fact]
    public async Task PermissionFamilyEntity_CapturedOnceWithoutDuplicatingThePermissionAuditPath()
    {
        var options = NewInMemoryOptions();
        var operationAccessor = new UseCaseOperationContextAccessor();
        var auditService = new Mock<IAuditService>();
        auditService
            .Setup(service => service.LogAsync(It.IsAny<CreateAuditLogRequest>()))
            .Returns(Task.CompletedTask);
        await using var context = new ApplicationDbContext(options, null, operationAccessor, auditService.Object);

        var role = new Role("UniversalCaptureRole", "Role for universal capture test", null) { Id = Guid.NewGuid() };
        var assignment = new UserRole(Guid.NewGuid(), role.Id, Guid.NewGuid()) { Id = Guid.NewGuid(), Role = role };
        context.Add(role);
        context.Add(assignment);
        await context.SaveChangesAsync();

        // The universal store records the entity change exactly once per entity: one Role row
        // (added) and one UserRole row (added), with no duplicates from the permission path.
        var entityRecords = await AuditRecords(context).ToListAsync();
        entityRecords.Where(record => record.EntityName == "UserRole").Should().ContainSingle();
        entityRecords.Where(record => record.EntityName == "Role").Should().ContainSingle();
        entityRecords.Should().NotContain(record => record.EntityName.Contains("AuditLog", StringComparison.OrdinalIgnoreCase));

        // The dedicated permission path emitted its grouped central AuditLog record(s) for the
        // permission-family mutation (a separate store — no double recording): every central
        // record is a Permission-family entry, and none of them duplicates the per-entity
        // change history the universal store owns.
        auditService.Invocations.Should().NotBeEmpty();
        auditService.Invocations
            .Select(invocation => (CreateAuditLogRequest)invocation.Arguments[0])
            .Should().OnlyContain(request => request.ResourceType == "Permission");
    }

    private static DbContextOptions<ApplicationDbContext> NewInMemoryOptions() =>
        new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    private static IQueryable<EntityChangeAuditRecord> AuditRecords(ApplicationDbContext context) =>
        context.Set<EntityChangeAuditRecord>().IgnoreQueryFilters();

    // The full application model carries PostgreSQL-specific DDL, so the rollback proof
    // creates only the tables it mutates, deriving column names from the EF model.
    private static async Task CreateTableAsync(ApplicationDbContext context, SqliteConnection connection, Type clrType)
    {
        var entityType = context.Model.FindEntityType(clrType)
            ?? throw new InvalidOperationException($"{clrType.Name} is not mapped");
        var tableName = entityType.GetTableName()
            ?? throw new InvalidOperationException($"{clrType.Name} has no table mapping");
        var columns = context.Model.GetEntityTypes()
            .Where(candidate => candidate.GetTableName() == tableName)
            .SelectMany(candidate => candidate.GetProperties())
            .GroupBy(property => property.GetColumnName(StoreObjectIdentifier.Table(tableName, entityType.GetSchema())))
            .Where(group => group.Key is not null)
            .Select(group => group.First())
            .ToList();
        var columnDefinitions = columns.Select(property => property.IsPrimaryKey()
            ? $"\"{property.Name}\" TEXT PRIMARY KEY"
            : $"\"{property.Name}\" {(IsIntegerColumn(property.ClrType) ? "INTEGER" : "TEXT")}");
        var command = connection.CreateCommand();
        command.CommandText = $"CREATE TABLE \"{tableName}\" ({string.Join(", ", columnDefinitions)})";
        await command.ExecuteNonQueryAsync();
    }

    private static bool IsIntegerColumn(Type clrType) =>
        clrType == typeof(bool) || clrType == typeof(byte) || clrType == typeof(short)
        || clrType == typeof(int) || clrType == typeof(long);
}
