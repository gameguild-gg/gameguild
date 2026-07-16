using FluentAssertions;

namespace GameGuild.API.UnitTests.Database;

public sealed class CanonicalSnapshotEntitySetTests
{
    [Fact]
    public void SnapshotAndDesigner_PreserveEntityScopedTask2MetadataSemantics()
    {
        var root = FindRepositoryRoot();
        var snapshot = File.ReadAllText(Path.Combine(
            root, "apps", "api", "Source", "GameGuild.API", "Database", "Migrations", "ApplicationDbContextModelSnapshot.cs"));
        var designer = File.ReadAllText(Path.Combine(
            root, "apps", "api", "Source", "GameGuild.API", "Database", "Migrations", "20260716010751_AddAssignmentDeliveryAndGradingContracts.Designer.cs"));

        AssertEntityMetadataMatches(snapshot, designer, "GameGuild.Learning.Assessments.Assessment",
        [
            "b.Property<bool>(\"AllowLateSubmissions\")",
            "b.Property<Guid?>(\"AssessmentGroupId\")",
            "b.Property<DateTime?>(\"DueAt\")",
            "b.Property<DateTime?>(\"LateSubmissionDeadline\")",
            "b.Property<int>(\"PresentationMode\")",
            "b.Property<int>(\"SubmissionModalities\")",
            "b.HasIndex(\"AssessmentGroupId\")",
            "b.HasIndex(\"CourseId\")",
            "CK_Assessments_DeliverySchedule",
            "CK_Assessments_PresentationMode",
            "CK_Assessments_SubmissionModalities",
            "b.HasOne(\"GameGuild.Learning.Assessments.AssessmentGroup\", \"AssessmentGroup\")",
            ".HasForeignKey(\"AssessmentGroupId\")",
            ".OnDelete(DeleteBehavior.SetNull)",
            "b.Navigation(\"AssessmentGroup\")",
            "b.Navigation(\"InteractiveVideoCues\")"
        ]);

        AssertEntityMetadataMatches(snapshot, designer, "GameGuild.Learning.Assessments.AssessmentSubmission",
        [
            "b.Property<int>(\"SubmittedModalities\")",
            "b.Property<bool>(\"IsLate\")",
            "b.Property<string>(\"TextPayload\")",
            "b.Property<string>(\"FilePayload\")",
            "b.Property<string>(\"UrlPayload\")",
            "b.Property<string>(\"CodePayload\")",
            "b.Property<string>(\"MediaPayload\")",
            "b.Property<string>(\"ProjectPayload\")",
            "b.Property<string>(\"StructuredAnswerPayload\")",
            "b.HasIndex(\"AssessmentId\")",
            "b.HasIndex(\"EnrollmentId\")",
            "b.HasIndex(\"UserId\")",
            "CK_AssessmentSubmissions_SubmittedModalities",
            "CK_AssessmentSubmissions_PayloadConsistency",
            "(\\\"TextPayload\\\" IS NULL OR (\\\"SubmittedModalities\\\" & 1) <> 0)"
        ]);

        AssertEntityMetadataMatches(snapshot, designer, "GameGuild.Learning.Assessments.InteractiveVideoAssessmentCue",
        [
            "b.Property<Guid>(\"AssessmentId\")",
            "b.Property<Guid>(\"ContentId\")",
            "b.Property<string>(\"CueId\")",
            "b.HasIndex(\"ContentId\")",
            "b.HasIndex(\"AssessmentId\", \"ContentId\", \"CueId\")",
            ".IsUnique()",
            "b.HasOne(\"GameGuild.Learning.Assessments.Assessment\", \"Assessment\")",
            ".WithMany(\"InteractiveVideoCues\")",
            ".HasForeignKey(\"AssessmentId\")",
            "b.Navigation(\"Assessment\")"
        ]);
    }

    private static void AssertEntityMetadataMatches(string snapshot, string designer, string entityType, IReadOnlyCollection<string> requiredSemantics)
    {
        var snapshotEntity = string.Join(Environment.NewLine, ExtractEntityBlocks(snapshot, entityType));
        var designerEntity = string.Join(Environment.NewLine, ExtractEntityBlocks(designer, entityType));

        snapshotEntity.Should().NotBeNullOrWhiteSpace($"{entityType} must be represented in the snapshot");
        designerEntity.Should().NotBeNullOrWhiteSpace($"{entityType} must be represented in the migration designer");

        foreach (var semantic in requiredSemantics)
        {
            snapshotEntity.Should().Contain(semantic, $"{entityType} snapshot metadata must preserve {semantic}");
            designerEntity.Should().Contain(semantic, $"{entityType} designer metadata must preserve {semantic}");
        }
    }

    private static IReadOnlyList<string> ExtractEntityBlocks(string metadata, string entityType)
    {
        var marker = $"modelBuilder.Entity(\"{entityType}\", b =>";
        var blocks = new List<string>();
        var searchStart = 0;

        while (metadata.IndexOf(marker, searchStart, StringComparison.Ordinal) is var markerIndex && markerIndex >= 0)
        {
            var openingBraceIndex = metadata.IndexOf('{', markerIndex);
            var depth = 0;
            for (var index = openingBraceIndex; index < metadata.Length; index++)
            {
                if (metadata[index] == '{') depth++;
                if (metadata[index] != '}') continue;

                depth--;
                if (depth != 0) continue;

                blocks.Add(metadata[markerIndex..(index + 1)]);
                searchStart = index + 1;
                break;
            }
        }

        return blocks;
    }

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
