using GameGuild.Assets.VirusScan;
using GameGuild.Assets.Deduplication;

namespace GameGuild.Assets.UnitTests;

#region AssetContent Additional Tests

public class AssetContentAdditionalTests
{
    private static AssetContent CreateContent(string mimeType = "image/png")
    {
        return new AssetContent(
            "test-bucket", "objects/test.png", "abc123hash",
            mimeType, 1024, 100, 100);
    }

    // DetermineKindFromMimeType tests (uncovered branches)
    [Fact]
    public void Kind_ForArchiveMimeType_ZipShouldBeArchive()
    {
        var content = CreateContent("application/zip");
        content.Kind.Should().Be(AssetKind.Archive);
    }

    [Fact]
    public void Kind_ForArchiveMimeType_RarShouldBeArchive()
    {
        var content = CreateContent("application/x-rar-compressed");
        content.Kind.Should().Be(AssetKind.Archive);
    }

    [Fact]
    public void Kind_ForArchiveMimeType_7zShouldBeArchive()
    {
        var content = CreateContent("application/x-7z-compressed");
        content.Kind.Should().Be(AssetKind.Archive);
    }

    [Fact]
    public void Kind_ForDocumentWithDocumentInMimeType()
    {
        var content = CreateContent("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        content.Kind.Should().Be(AssetKind.Document);
    }

    [Fact]
    public void Kind_ForUnknownMimeType_ShouldBeOther()
    {
        var content = CreateContent("application/octet-stream");
        content.Kind.Should().Be(AssetKind.Other);
    }

    // ModerationLabelsList
    [Fact]
    public void ModerationLabelsList_WhenNull_ShouldReturnEmpty()
    {
        var content = CreateContent();
        content.ModerationLabels = null;
        content.ModerationLabelsList.Should().BeEmpty();
    }

    [Fact]
    public void ModerationLabelsList_WhenEmpty_ShouldReturnEmpty()
    {
        var content = CreateContent();
        content.ModerationLabels = "";
        content.ModerationLabelsList.Should().BeEmpty();
    }

    [Fact]
    public void ModerationLabelsList_WithLabels_ShouldDeserialize()
    {
        var content = CreateContent();
        content.ModerationLabels = "[\"explicit\",\"violence\"]";
        content.ModerationLabelsList.Should().Equal("explicit", "violence");
    }

    // SetModerationLabels
    [Fact]
    public void SetModerationLabels_ShouldSerializeToJson()
    {
        var content = CreateContent();
        content.SetModerationLabels(new[] { "spam", "hate" });
        content.ModerationLabels.Should().Contain("spam");
        content.ModerationLabels.Should().Contain("hate");
    }

    // IsSafeToServe
    [Fact]
    public void IsSafeToServe_WhenCleanAndApproved_ShouldBeTrue()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.Approved;
        content.IsSafeToServe.Should().BeTrue();
    }

    [Fact]
    public void IsSafeToServe_WhenCleanAndApprovedWithWarning_ShouldBeTrue()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.ApprovedWithWarning;
        content.IsSafeToServe.Should().BeTrue();
    }

    [Fact]
    public void IsSafeToServe_WhenInfected_ShouldBeFalse()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Infected;
        content.ModerationStatus = ModerationStatus.Approved;
        content.IsSafeToServe.Should().BeFalse();
    }

    [Fact]
    public void IsSafeToServe_WhenRejected_ShouldBeFalse()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.Rejected;
        content.IsSafeToServe.Should().BeFalse();
    }

    [Fact]
    public void IsSafeToServe_WhenPending_ShouldBeFalse()
    {
        var content = CreateContent();
        content.IsSafeToServe.Should().BeFalse(); // defaults: Pending/Pending
    }

    // IsPendingProcessing
    [Fact]
    public void IsPendingProcessing_WhenBothPending_ShouldBeTrue()
    {
        var content = CreateContent();
        content.IsPendingProcessing.Should().BeTrue();
    }

    [Fact]
    public void IsPendingProcessing_WhenVirusScanScanning_ShouldBeTrue()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Scanning;
        content.ModerationStatus = ModerationStatus.Approved;
        content.IsPendingProcessing.Should().BeTrue();
    }

    [Fact]
    public void IsPendingProcessing_WhenModerationProcessing_ShouldBeTrue()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.Processing;
        content.IsPendingProcessing.Should().BeTrue();
    }

    [Fact]
    public void IsPendingProcessing_WhenAllComplete_ShouldBeFalse()
    {
        var content = CreateContent();
        content.VirusScanStatus = VirusScanStatus.Clean;
        content.ModerationStatus = ModerationStatus.Approved;
        content.IsPendingProcessing.Should().BeFalse();
    }

    // SetModerationStatus (overload with labels)
    [Fact]
    public void SetModerationStatus_WithLabels_ShouldSetLabelsAndTimestamp()
    {
        var content = CreateContent();
        content.SetModerationStatus(ModerationStatus.Rejected, new[] { "explicit" } as IEnumerable<string>);
        content.ModerationStatus.Should().Be(ModerationStatus.Rejected);
        content.ModerationCompletedAt.Should().NotBeNull();
        content.ModerationLabelsList.Should().Contain("explicit");
    }

    [Fact]
    public void SetModerationStatus_WithoutLabels_ShouldNotChangeLabels()
    {
        var content = CreateContent();
        content.SetModerationStatus(ModerationStatus.Approved, (IEnumerable<string>?)null);
        content.ModerationStatus.Should().Be(ModerationStatus.Approved);
        content.ModerationCompletedAt.Should().NotBeNull();
    }

    // SetModerationStatus admin overload
    [Fact]
    public void SetModerationStatus_AdminOverload_WithLabels_ShouldSet()
    {
        var content = CreateContent();
        var reviewerId = Guid.NewGuid();
        content.SetModerationStatus(ModerationStatus.Blocked, reviewerId, new[] { "hate" }, "Blocked by admin");
        content.ModerationStatus.Should().Be(ModerationStatus.Blocked);
        content.ModerationCompletedAt.Should().NotBeNull();
        content.ModerationLabelsList.Should().Contain("hate");
    }

    [Fact]
    public void SetModerationStatus_AdminOverload_NullLabels_ShouldNotCrash()
    {
        var content = CreateContent();
        content.SetModerationStatus(ModerationStatus.Approved, Guid.NewGuid(), null, null);
        content.ModerationStatus.Should().Be(ModerationStatus.Approved);
    }

    // MarkAsNonDeletable / MarkAsDeletable
    [Fact]
    public void MarkAsNonDeletable_ShouldSetFalseAndClearDeletion()
    {
        var content = CreateContent();
        content.MarkedForDeletionAt = DateTime.UtcNow;
        content.MarkAsNonDeletable("legal hold");
        content.IsDeletable.Should().BeFalse();
        content.MarkedForDeletionAt.Should().BeNull();
    }

    [Fact]
    public void MarkAsDeletable_WithZeroReferences_ShouldMarkForDeletion()
    {
        var content = CreateContent();
        content.ReferenceCount = 0;
        content.MarkAsDeletable();
        content.IsDeletable.Should().BeTrue();
        content.MarkedForDeletionAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkAsDeletable_WithReferences_ShouldNotMarkForDeletion()
    {
        var content = CreateContent();
        content.ReferenceCount = 3;
        content.MarkAsDeletable();
        content.IsDeletable.Should().BeTrue();
        content.MarkedForDeletionAt.Should().BeNull();
    }
}

#endregion

#region AssetReference Additional Tests

public class AssetReferenceAdditionalTests
{
    private static AssetReference CreateRef()
    {
        return new AssetReference(
            Guid.NewGuid(), Guid.NewGuid(), "Test Asset",
            AssetAccessPolicy.Private, null, null);
    }

    // TagsList
    [Fact]
    public void TagsList_WhenNull_ShouldReturnEmpty()
    {
        var r = CreateRef();
        r.Tags = null;
        r.TagsList.Should().BeEmpty();
    }

    [Fact]
    public void TagsList_WhenEmpty_ShouldReturnEmpty()
    {
        var r = CreateRef();
        r.Tags = "";
        r.TagsList.Should().BeEmpty();
    }

    [Fact]
    public void TagsList_WithTags_ShouldDeserialize()
    {
        var r = CreateRef();
        r.Tags = "[\"tag1\",\"tag2\"]";
        r.TagsList.Should().Equal("tag1", "tag2");
    }

    // SetTags
    [Fact]
    public void SetTags_ShouldSerializeToJson()
    {
        var r = CreateRef();
        r.SetTags(new[] { "gamedev", "pixel-art" });
        r.Tags.Should().Contain("gamedev");
        r.Tags.Should().Contain("pixel-art");
    }

    // IsDownloadWindowValid
    [Fact]
    public void IsDownloadWindowValid_NoExpiry_ShouldBeTrue()
    {
        var r = CreateRef();
        r.DownloadWindowExpiresAt = null;
        r.IsDownloadWindowValid.Should().BeTrue();
    }

    [Fact]
    public void IsDownloadWindowValid_FutureExpiry_ShouldBeTrue()
    {
        var r = CreateRef();
        r.DownloadWindowExpiresAt = DateTime.UtcNow.AddHours(1);
        r.IsDownloadWindowValid.Should().BeTrue();
    }

    [Fact]
    public void IsDownloadWindowValid_PastExpiry_ShouldBeFalse()
    {
        var r = CreateRef();
        r.DownloadWindowExpiresAt = DateTime.UtcNow.AddHours(-1);
        r.IsDownloadWindowValid.Should().BeFalse();
    }

    // AddLocalization
    [Fact]
    public void AddLocalization_ShouldAddToCollection()
    {
        var r = CreateRef();
        var lang = new GameGuild.Localization.Language { Id = Guid.NewGuid() };
        var loc = r.AddLocalization("AltText", "A test image", lang);
        loc.Should().NotBeNull();
        loc.FieldName.Should().Be("AltText");
        loc.Content.Should().Be("A test image");
        loc.ResourceType.Should().Be("AssetReference");
        r.Localizations.Should().ContainSingle();
    }

    [Fact]
    public void AddLocalization_NullFieldName_ShouldThrow()
    {
        var r = CreateRef();
        var lang = new GameGuild.Localization.Language { Id = Guid.NewGuid() };
        var act = () => r.AddLocalization(null!, "content", lang);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLocalization_NullContent_ShouldThrow()
    {
        var r = CreateRef();
        var lang = new GameGuild.Localization.Language { Id = Guid.NewGuid() };
        var act = () => r.AddLocalization("field", null!, lang);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddLocalization_NullLanguage_ShouldThrow()
    {
        var r = CreateRef();
        var act = () => r.AddLocalization("field", "content", null!);
        act.Should().Throw<ArgumentNullException>();
    }
}

#endregion

#region VirusScanOptions Tests

public class VirusScanOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var opts = new VirusScanOptions();
        opts.Enabled.Should().BeTrue();
        opts.Mode.Should().Be(VirusScanMode.Hybrid);
        opts.ClamAvHost.Should().Be("localhost");
        opts.ClamAvPort.Should().Be(3310);
        opts.TimeoutSeconds.Should().Be(60);
        opts.MaxScanSizeBytes.Should().Be(100 * 1024 * 1024);
        opts.QuarantineInfected.Should().BeTrue();
        opts.QuarantineBucket.Should().Be("quarantine");
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        VirusScanOptions.SectionName.Should().Be("Assets:VirusScan");
    }

    [Fact]
    public void SyncScanMimeTypes_ShouldContainHighRiskTypes()
    {
        var opts = new VirusScanOptions();
        opts.SyncScanMimeTypes.Should().Contain("application/x-msdownload");
        opts.SyncScanMimeTypes.Should().Contain("application/zip");
        opts.SyncScanMimeTypes.Should().Contain("application/javascript");
        opts.SyncScanMimeTypes.Should().Contain("application/octet-stream");
        opts.SyncScanMimeTypes.Should().Contain("application/x-java-archive");
    }

    [Theory]
    [InlineData(VirusScanMode.Sync, 0)]
    [InlineData(VirusScanMode.Async, 1)]
    [InlineData(VirusScanMode.Hybrid, 2)]
    public void VirusScanMode_EnumValues(VirusScanMode mode, int expected)
    {
        ((int)mode).Should().Be(expected);
    }
}

#endregion

#region DeduplicationOptions Tests

public class DeduplicationOptionsTests
{
    [Fact]
    public void Defaults_ShouldBeReasonable()
    {
        var opts = new DeduplicationOptions();
        opts.Enabled.Should().BeTrue();
        opts.EnablePerceptualHashing.Should().BeTrue();
        opts.PerceptualHashThreshold.Should().Be(5);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        DeduplicationOptions.SectionName.Should().Be("Assets:Deduplication");
    }
}

#endregion

#region AssetReport Additional Tests

public class AssetReportAdditionalTests
{
    private static AssetReport CreateReport()
    {
        return new AssetReport(Guid.NewGuid(), Guid.NewGuid(), ReportReason.Inappropriate, "Test report");
    }

    [Fact]
    public void IsPending_WhenPending_ShouldBeTrue()
    {
        var report = CreateReport();
        report.Status = ReportStatus.Pending;
        report.IsPending.Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenUnderReview_ShouldBeTrue()
    {
        var report = CreateReport();
        report.Status = ReportStatus.UnderReview;
        report.IsPending.Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenResolved_ShouldBeFalse()
    {
        var report = CreateReport();
        report.Status = ReportStatus.Resolved;
        report.IsPending.Should().BeFalse();
    }

    [Fact]
    public void IsPending_WhenDismissed_ShouldBeFalse()
    {
        var report = CreateReport();
        report.Status = ReportStatus.Dismissed;
        report.IsPending.Should().BeFalse();
    }
}

#endregion
