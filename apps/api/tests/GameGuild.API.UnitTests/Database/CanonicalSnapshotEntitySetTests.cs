using FluentAssertions;

namespace GameGuild.API.UnitTests.Database;

public sealed class CanonicalSnapshotEntitySetTests
{
    [Fact]
    public void SnapshotAndDesigner_PreserveTask2AssessmentMetadataSemantics()
    {
        var root = FindRepositoryRoot();
        var files = new[]
        {
            Path.Combine(root, "apps", "api", "Source", "GameGuild.API", "Database", "Migrations", "ApplicationDbContextModelSnapshot.cs"),
            Path.Combine(root, "apps", "api", "Source", "GameGuild.API", "Database", "Migrations", "20260716010751_AddAssignmentDeliveryAndGradingContracts.Designer.cs")
        };

        foreach (var file in files)
        {
            var metadata = File.ReadAllText(file);
            foreach (var property in new[]
            {
                "AllowLateSubmissions", "AssessmentGroupId", "DueAt", "LateSubmissionDeadline",
                "PresentationMode", "SubmissionModalities", "SubmittedModalities", "IsLate",
                "TextPayload", "FilePayload", "UrlPayload", "CodePayload", "MediaPayload",
                "ProjectPayload", "StructuredAnswerPayload"
            })
            {
                metadata.Should().Contain($"(\"{property}\")");
            }

            metadata.Should().Contain("CK_Assessments_DeliverySchedule");
            metadata.Should().Contain("CK_Assessments_SubmissionModalities");
            metadata.Should().Contain("CK_AssessmentSubmissions_SubmittedModalities");
            metadata.Should().Contain("CK_AssessmentSubmissions_PayloadConsistency");
            metadata.Should().Contain("(\\\"TextPayload\\\" IS NULL OR (\\\"SubmittedModalities\\\" & 1) <> 0)");
            metadata.Should().Contain("HasIndex(\"AssessmentId\", \"ContentId\", \"CueId\")");
            metadata.Should().Contain("HasOne(\"GameGuild.Learning.Assessments.Assessment\", \"Assessment\")");
            metadata.Should().Contain("WithMany(\"InteractiveVideoCues\")");
        }
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
