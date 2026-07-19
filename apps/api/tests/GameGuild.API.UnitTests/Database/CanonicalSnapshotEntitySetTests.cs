using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.API.UnitTests.Database;

public sealed class CanonicalSnapshotEntitySetTests
{
    [Fact]
    public void Snapshot_Preserves_All_AssignmentDeliveryDesignerMetadata()
    {
        var snapshot = CreateSnapshotModel();
        var designer = new AddAssignmentDeliveryAndGradingContracts().TargetModel;

        foreach (var entityName in Task2EntityNames)
        {
            var snapshotContract = BuildEntityContract(snapshot, entityName);
            var designerContract = BuildEntityContract(designer, entityName);

            snapshotContract.Name.Should().Be(designerContract.Name);
            snapshotContract.Table.Should().Be(designerContract.Table);
            snapshotContract.Schema.Should().Be(designerContract.Schema);

            foreach (var property in designerContract.Properties)
                snapshotContract.Properties.Should().ContainEquivalentOf(property,
                    $"the current snapshot must preserve the migration-era property {property.Name} on {entityName}");

            foreach (var constraint in designerContract.CheckConstraints)
                snapshotContract.CheckConstraints.Should().ContainEquivalentOf(constraint,
                    $"the current snapshot must preserve the migration-era constraint {constraint.Name} on {entityName}");

            foreach (var index in designerContract.Indexes)
                snapshotContract.Indexes.Should().ContainEquivalentOf(index,
                    $"the current snapshot must preserve the migration-era index {index.Name} on {entityName}");

            foreach (var foreignKey in designerContract.ForeignKeys)
                snapshotContract.ForeignKeys.Should().ContainEquivalentOf(foreignKey,
                    $"the current snapshot must preserve migration-era relationships on {entityName}");
        }
    }

    [Fact]
    public void SnapshotAndDesigner_HaveIdenticalProjectChannelMetadata()
    {
        var snapshot = CreateSnapshotModel();
        var designer = new AddProjectChannelContracts().TargetModel;

        foreach (var entityName in ProjectChannelEntityNames)
        {
            BuildEntityContract(snapshot, entityName).Should().BeEquivalentTo(
                BuildEntityContract(designer, entityName),
                options => options.WithStrictOrdering(),
                $"the snapshot and designer must retain identical complete metadata for {entityName}");
        }
    }

    [Fact]
    public void Snapshot_Maps_Only_The_Canonical_Project_Aggregate_To_Projects()
    {
        var projectEntities = CreateSnapshotModel()
            .GetEntityTypes()
            .Where(entity => string.Equals(entity.GetTableName(), "projects", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        projectEntities.Should().ContainSingle();
        projectEntities[0].Name.Should().Be("GameGuild.Projects.Project");
        projectEntities.Should().NotContain(entity => entity.Name == "GameGuild.Projects.ProjectLegacy");
    }

    private static IModel CreateSnapshotModel()
    {
        var snapshotType = typeof(ApplicationDbContext).Assembly.GetType(
            "GameGuild.API.Database.Migrations.ApplicationDbContextModelSnapshot",
            throwOnError: true)!;
        return ((ModelSnapshot)Activator.CreateInstance(snapshotType, nonPublic: true)!).Model;
    }

    private static EntityContract BuildEntityContract(IModel model, string entityName)
    {
        var entity = model.FindEntityType(entityName);
        entity.Should().NotBeNull($"{entityName} must exist");
        var resolvedEntity = entity!;
        return new EntityContract(
            entityName,
            resolvedEntity.GetTableName(),
            resolvedEntity.GetSchema(),
            resolvedEntity.GetProperties().OrderBy(property => property.Name).Select(property => new PropertyContract(
                property.Name,
                property.ClrType.FullName,
                property.IsNullable,
                property.GetColumnType(),
                property.GetMaxLength(),
                property.GetPrecision(),
                property.GetScale(),
                property.GetDefaultValue()?.ToString(),
                property.GetDefaultValueSql(),
                property.ValueGenerated.ToString(),
                property.IsConcurrencyToken,
                property.IsUnicode())).ToArray(),
            resolvedEntity.GetCheckConstraints().OrderBy(constraint => constraint.Name).Select(constraint =>
                new CheckConstraintContract(constraint.Name!, constraint.Sql!)).ToArray(),
            resolvedEntity.GetIndexes().OrderBy(index => index.Name).Select(index => new IndexContract(
                index.Name,
                index.Properties.Select(property => property.Name).ToArray(),
                index.IsUnique,
                index.GetFilter())).ToArray(),
            resolvedEntity.GetForeignKeys().OrderBy(foreignKey => string.Join(",", foreignKey.Properties.Select(property => property.Name))).Select(foreignKey =>
                new ForeignKeyContract(
                    foreignKey.Properties.Select(property => property.Name).ToArray(),
                    foreignKey.PrincipalEntityType.Name,
                    foreignKey.PrincipalKey.Properties.Select(property => property.Name).ToArray(),
                    foreignKey.DeleteBehavior.ToString(),
                    foreignKey.IsRequired,
                    foreignKey.DependentToPrincipal?.Name,
                    foreignKey.PrincipalToDependent?.Name)).ToArray());
    }

    private static readonly string[] Task2EntityNames =
    [
        "GameGuild.Learning.Assessments.Assessment",
        "GameGuild.Learning.Assessments.AssessmentSubmission",
        "GameGuild.Learning.Assessments.InteractiveVideoAssessmentCue"
    ];

    private static readonly string[] ProjectChannelEntityNames =
    [
        "GameGuild.Projects.ProjectStoreProduct",
        "GameGuild.TestingLab.SessionProject"
    ];

    private sealed record EntityContract(
        string Name,
        string? Table,
        string? Schema,
        IReadOnlyList<PropertyContract> Properties,
        IReadOnlyList<CheckConstraintContract> CheckConstraints,
        IReadOnlyList<IndexContract> Indexes,
        IReadOnlyList<ForeignKeyContract> ForeignKeys);

    private sealed record PropertyContract(
        string Name,
        string? ClrType,
        bool IsNullable,
        string? ColumnType,
        int? MaxLength,
        int? Precision,
        int? Scale,
        string? DefaultValue,
        string? DefaultValueSql,
        string ValueGenerated,
        bool IsConcurrencyToken,
        bool? IsUnicode);

    private sealed record CheckConstraintContract(string Name, string Sql);
    private sealed record IndexContract(string? Name, IReadOnlyList<string> Properties, bool IsUnique, string? Filter);
    private sealed record ForeignKeyContract(
        IReadOnlyList<string> Properties,
        string PrincipalEntity,
        IReadOnlyList<string> PrincipalKey,
        string DeleteBehavior,
        bool IsRequired,
        string? DependentNavigation,
        string? PrincipalNavigation);

    [Fact]
    public void Snapshot_PreservesMigrationBackedEntitiesAndExcludesUnmigratedDrift()
    {
        var root = FindRepositoryRoot();
        var snapshot = File.ReadAllText(Path.Combine(
            root,
            "apps", "api", "Source", "GameGuild.API", "Database", "Migrations", "ApplicationDbContextModelSnapshot.cs"));
        var entities = System.Text.RegularExpressions.Regex
            .Matches(snapshot, "modelBuilder\\.Entity\\(\\\"([^\\\"]+)\\\"")
            .Select(match => match.Groups[1].Value)
            .ToHashSet(StringComparer.Ordinal);

        entities.Should().Contain(new[]
        {
            "GameGuild.Features.FeatureFlagDependencyLink",
            "GameGuild.Identity.Authentication.ApiKey",
            "GameGuild.Tags.Tag",
            "GameGuild.Tags.TagRelationship",
            "GameGuild.Tags.CertificateTag",
            "GameGuild.Tags.TagProficiency",
            "GameGuild.Commerce.Products.SupportTicket",
            "GameGuild.Commerce.Products.SupportTicketMessage",
            "GameGuild.Learning.Assessments.AssessmentGroup",
            "GameGuild.Learning.Assessments.InteractiveVideoAssessmentCue",
            "GameGuild.Notifications.Notification",
            "GameGuild.Notifications.NotificationPreference",
            "GameGuild.Notifications.NotificationTemplate",
            "GameGuild.Projects.ProjectStoreProduct",
            "GameGuild.Compliance.Audit.AuditLog",
            "GameGuild.Commerce.Orders.Order",
            "GameGuild.Identity.Authorization.PermissionTemplate"
        });
        entities.Should().NotContain(new[]
        {
            "GameGuild.Analytics.AnalyticsEvent",
            "GameGuild.Assets.AssetContent",
            "GameGuild.Localization.Language"
        });
    }

    [Fact]
    public void ComplianceAuditMigration_IsScopedToTheAuditLogTable()
    {
        var up = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var down = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");
        var migration = new TestableAddComplianceAuditLog();

        migration.BuildUp(up);
        migration.BuildDown(down);

        up.Operations.OfType<CreateTableOperation>()
            .Should().ContainSingle(operation => operation.Name == "AuditLogs");
        up.Operations.OfType<CreateIndexOperation>()
            .Should().HaveCount(7).And.OnlyContain(operation => operation.Table == "AuditLogs");
        down.Operations.OfType<DropTableOperation>()
            .Should().ContainSingle(operation => operation.Name == "AuditLogs");
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(Directory.GetCurrentDirectory()); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "apps", "api", "Source", "GameGuild.API")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }

    private sealed class TestableAddComplianceAuditLog : AddComplianceAuditLog
    {
        public void BuildUp(MigrationBuilder migrationBuilder) => Up(migrationBuilder);
        public void BuildDown(MigrationBuilder migrationBuilder) => Down(migrationBuilder);
    }
}
