using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Database.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GameGuild.API.UnitTests.Database;

public sealed class CanonicalSnapshotEntitySetTests
{
    [Fact]
    public void SnapshotAndDesigner_HaveIdenticalCompleteTask2EntityMetadata()
    {
        var snapshot = CreateSnapshotModel();
        var designer = new AddAssignmentDeliveryAndGradingContracts().TargetModel;

        foreach (var entityName in Task2EntityNames)
        {
            var snapshotContract = BuildEntityContract(snapshot, entityName);
            var designerContract = BuildEntityContract(designer, entityName);
            snapshotContract.Should().BeEquivalentTo(designerContract, options => options.WithStrictOrdering(),
                $"the snapshot and designer must retain identical complete metadata for {entityName}");
        }
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
    public void Snapshot_PreservesMigrationBackedEntitiesAndExcludesAuditedDrift()
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
            "GameGuild.Notifications.NotificationTemplate"
        });
        entities.Should().NotContain(new[]
        {
            "GameGuild.Analytics.AnalyticsEvent",
            "GameGuild.Assets.AssetContent",
            "GameGuild.Localization.Language",
            "GameGuild.Compliance.Audit.AuditLog",
            "GameGuild.Commerce.Orders.Order",
            "GameGuild.Identity.Authorization.PermissionTemplate"
        });
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
}
