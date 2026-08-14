using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Assets.Controllers;
using GameGuild.Assets.Queries;

namespace GameGuild.Assets.UnitTests;

public sealed class M3AssetQueryCoverageTests
{
    [Fact]
    public async Task AssetStatisticsRetentionAndExportHandlers_ShouldUsePersistedAssets()
    {
        await using var db = CreateDbContext();
        var candidate = CreateContent(Guid.NewGuid(), "application/pdf", 100);
        candidate.ReferenceCount = 0;
        candidate.MarkAsDeletable();
        candidate.MarkedForDeletionAt = SystemClock.UtcNow.AddDays(-2);
        candidate.SetVirusScanStatus(VirusScanStatus.Pending);
        candidate.SetModerationStatus(ModerationStatus.Pending);
        var legalHold = CreateContent(Guid.NewGuid(), "image/png", 200);
        legalHold.MarkAsNonDeletable("audit");
        legalHold.SetVirusScanStatus(VirusScanStatus.Scanning);
        legalHold.SetModerationStatus(ModerationStatus.Processing);
        var blocked = CreateContent(Guid.NewGuid(), "video/mp4", 300);
        blocked.SetVirusScanStatus(VirusScanStatus.Infected);
        blocked.SetModerationStatus(ModerationStatus.Rejected);
        var reference = CreateReference(Guid.NewGuid(), candidate);
        reference.AccessCount = 7;
        db.Set<AssetContent>().AddRange(candidate, legalHold, blocked);
        db.Set<AssetReference>().Add(reference);
        await db.SaveChangesAsync();

        var statistics = await new GetAssetStatisticsHandler(db).Handle(new GetAssetStatisticsQuery(), CancellationToken.None);
        var retention = await new GetAssetRetentionReportHandler(db).Handle(new GetAssetRetentionReportQuery(GracePeriodHours: 0, Limit: 20_000), CancellationToken.None);
        var csv = await new ExportAssetStatisticsHandler(db).Handle(new ExportAssetStatisticsQuery("csv"), CancellationToken.None);
        var pdf = await new ExportAssetStatisticsHandler(db).Handle(new ExportAssetStatisticsQuery("pdf"), CancellationToken.None);

        statistics.TotalAssets.Should().Be(1);
        statistics.TotalContentObjects.Should().Be(3);
        statistics.TotalBytes.Should().Be(600);
        statistics.DocumentAssets.Should().Be(1);
        statistics.ImageAssets.Should().Be(1);
        statistics.VideoAssets.Should().Be(1);
        statistics.TotalAccesses.Should().Be(7);
        statistics.PendingVirusScans.Should().Be(2);
        statistics.PendingModeration.Should().Be(2);
        statistics.BlockedOrRejected.Should().Be(1);
        statistics.LegalHoldContent.Should().Be(1);
        statistics.RetentionCandidates.Should().Be(1);
        retention.GracePeriodHours.Should().Be(1);
        retention.Limit.Should().Be(10_000);
        retention.Candidates.Should().Be(1);
        retention.OnLegalHold.Should().Be(1);
        retention.MarkedForDeletion.Should().Be(1);
        retention.CandidateBytes.Should().Be(100);
        retention.Items.Single().AssetContentId.Should().Be(candidate.Id);
        csv.ContentType.Should().Be("text/csv");
        csv.FileName.Should().EndWith(".csv");
        csv.Content.Should().NotBeEmpty();
        pdf.ContentType.Should().Be("application/pdf");
        pdf.FileName.Should().EndWith(".pdf");
        pdf.Content.Should().StartWith((byte)'%');
    }

    [Fact]
    public async Task SearchAssetsHandler_ShouldFilterAndHideDeniedAssets()
    {
        await using var db = CreateDbContext();
        var tenantId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var visible = CreateReference(Guid.NewGuid(), CreateContent(Guid.NewGuid(), "image/png", 100), userId, "Property", parentId);
        visible.DisplayName = "front elevation";
        visible.OriginalFilename = "front.png";
        visible.Description = "street-facing photo";
        visible.SetTags(["listing", "front"]);
        visible.RecordAccess();
        var denied = CreateReference(Guid.NewGuid(), CreateContent(Guid.NewGuid(), "image/png", 100), userId, "Property", parentId);
        denied.DisplayName = "front hidden";
        db.Set<AssetContent>().AddRange(visible.Content, denied.Content);
        db.Set<AssetReference>().AddRange(visible, denied);
        await db.SaveChangesAsync();
        var access = new Mock<IAssetAccessService>();
        access.Setup(service => service.ValidateAccessAsync(visible.Id, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));
        access.Setup(service => service.ValidateAccessAsync(denied.Id, userId, tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired));

        var result = await new SearchAssetsHandler(db, access.Object).Handle(
            new SearchAssetsQuery("front", userId, tenantId, AssetKind.Image, "Property", parentId, Skip: -10, Take: 500),
            CancellationToken.None);

        result.TotalMatched.Should().Be(2);
        result.Returned.Should().Be(1);
        result.Items.Single().AssetReferenceId.Should().Be(visible.Id);
    }

    [Fact]
    public async Task GetAssetPreviewHandler_ShouldReturnNullForMissingDeniedOrMissingUrl()
    {
        var assetId = Guid.NewGuid();
        var reference = CreateReference(assetId, CreateContent(Guid.NewGuid(), "application/pdf", 100));
        var referenceRepo = new Mock<IAssetReferenceRepository>();
        var access = new Mock<IAssetAccessService>();
        var extraction = new Mock<IAssetTextExtractionService>();
        referenceRepo.SetupSequence(repo => repo.GetByIdWithContentAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetReference?)null)
            .ReturnsAsync(reference)
            .ReturnsAsync(reference);
        access.SetupSequence(service => service.ValidateAccessAsync(assetId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(false, AssetAccessDeniedReason.OwnershipRequired))
            .ReturnsAsync(new AssetAccessValidation(true, null));
        access.Setup(service => service.GenerateAccessUrlAsync(assetId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAccessUrl?)null);
        var handler = new GetAssetPreviewHandler(referenceRepo.Object, access.Object, extraction.Object);

        var missing = await handler.Handle(new GetAssetPreviewQuery(assetId, null, null), CancellationToken.None);
        var denied = await handler.Handle(new GetAssetPreviewQuery(assetId, null, null), CancellationToken.None);
        var noUrl = await handler.Handle(new GetAssetPreviewQuery(assetId, null, null), CancellationToken.None);

        missing.Should().BeNull();
        denied.Should().BeNull();
        noUrl.Should().BeNull();
    }

    [Theory]
    [InlineData("application/pdf", AssetKind.Document, "pdf", true)]
    [InlineData("application/octet-stream", AssetKind.Other, "download", false)]
    [InlineData("application/json", AssetKind.Other, "text", true)]
    [InlineData("application/xml", AssetKind.Other, "text", true)]
    [InlineData("text/csv", AssetKind.Other, "text", true)]
    public async Task GetAssetPreviewHandler_ShouldResolvePreviewModes(string mimeType, AssetKind kind, string expectedMode, bool canInline)
    {
        var assetId = Guid.NewGuid();
        var reference = CreateReference(assetId, CreateContent(Guid.NewGuid(), mimeType, 100, kind));
        var referenceRepo = new Mock<IAssetReferenceRepository>();
        var access = new Mock<IAssetAccessService>();
        var extraction = new Mock<IAssetTextExtractionService>();
        referenceRepo.Setup(repo => repo.GetByIdWithContentAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        access.Setup(service => service.ValidateAccessAsync(assetId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));
        access.Setup(service => service.GenerateAccessUrlAsync(assetId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessUrl("https://assets.local/file", "token", DateTimeOffset.UtcNow.AddMinutes(5), mimeType));
        extraction.Setup(service => service.ExtractAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedAssetTextResult(string.Empty, mimeType, "direct", false, false, ["empty"]));
        var handler = new GetAssetPreviewHandler(referenceRepo.Object, access.Object, extraction.Object);

        var result = await handler.Handle(new GetAssetPreviewQuery(assetId, null, null, IncludeExtractedText: true, TextPreviewLength: 0), CancellationToken.None);

        result.Should().NotBeNull();
        result!.PreviewMode.Should().Be(expectedMode);
        result.CanInlinePreview.Should().Be(canInline);
        if (expectedMode == "text")
        {
            result.ExtractedTextPreview.Should().BeNull();
            result.Warnings.Should().Contain("empty");
        }
    }

    [Fact]
    public async Task GetAssetPreviewHandler_ShouldReturnUntrimmedTextWhenWithinLimit()
    {
        var assetId = Guid.NewGuid();
        var reference = CreateReference(assetId, CreateContent(Guid.NewGuid(), "text/plain", 100));
        var referenceRepo = new Mock<IAssetReferenceRepository>();
        var access = new Mock<IAssetAccessService>();
        var extraction = new Mock<IAssetTextExtractionService>();
        referenceRepo.Setup(repo => repo.GetByIdWithContentAsync(assetId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reference);
        access.Setup(service => service.ValidateAccessAsync(assetId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessValidation(true, null));
        access.Setup(service => service.GenerateAccessUrlAsync(assetId, null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AssetAccessUrl("https://assets.local/file.txt", "token", DateTimeOffset.UtcNow.AddMinutes(5), "text/plain"));
        extraction.Setup(service => service.ExtractAsync(reference, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ExtractedAssetTextResult("short", "text/plain", "direct", false, false, []));
        var handler = new GetAssetPreviewHandler(referenceRepo.Object, access.Object, extraction.Object);

        var result = await handler.Handle(new GetAssetPreviewQuery(assetId, null, null, TextPreviewLength: 20), CancellationToken.None);

        result.Should().NotBeNull();
        result!.ExtractedTextPreview.Should().Be("short");
        result.IsTextTruncated.Should().BeFalse();
    }

    [Fact]
    public void AssetQueryContracts_ShouldExposeConstructorAssignedValues()
    {
        var referenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var searchQuery = new SearchAssetsQuery("query", Guid.NewGuid(), Guid.NewGuid(), AssetKind.Document, "Lease", parentId, 3, 7);
        var searchResult = new AssetSearchResult(referenceId, contentId, "display", "file.pdf", "Lease", parentId, "application/pdf", AssetKind.Document, 123, 4, DateTime.UtcNow, DateTime.UtcNow);
        var searchResponse = new AssetSearchResponse(2, 1, [searchResult]);
        var statistics = new AssetStatisticsResponse(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12);
        var exportQuery = new ExportAssetStatisticsQuery("pdf");
        var export = new AssetStatisticsExportResponse("assets.pdf", "application/pdf", [1, 2, 3]);
        var retentionQuery = new GetAssetRetentionReportQuery(48, 20);
        var candidate = new AssetRetentionCandidateResponse(contentId, "bucket", "key", "application/pdf", 123, DateTime.UtcNow);
        var retention = new AssetRetentionReportResponse(48, 20, 1, 0, 1, 123, [candidate]);
        var bulkUrlRequest = new BulkAssetAccessUrlRequest([referenceId], DirectStorageUrl: true);
        var bulkDeleteRequest = new BulkDeleteAssetsRequest([referenceId]);

        searchQuery.Take.Should().Be(7);
        searchResponse.Items.Single().Should().Be(searchResult);
        statistics.TotalBytes.Should().Be(3);
        exportQuery.Format.Should().Be("pdf");
        export.Content.Should().Equal(1, 2, 3);
        retentionQuery.Limit.Should().Be(20);
        retention.Items.Single().Should().Be(candidate);
        bulkUrlRequest.DirectStorageUrl.Should().BeTrue();
        bulkDeleteRequest.AssetIds.Should().Contain(referenceId);
    }

    private static AssetsQueryTestDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AssetsQueryTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AssetsQueryTestDbContext(options);
    }

    private static AssetReference CreateReference(
        Guid id,
        AssetContent content,
        Guid? ownerId = null,
        string? parentResourceType = "Document",
        Guid? parentResourceId = null)
    {
        var reference = new AssetReference(
            content.Id,
            ownerId ?? Guid.NewGuid(),
            "Document",
            AssetAccessPolicy.Public,
            parentResourceType,
            parentResourceId ?? Guid.NewGuid());
        typeof(AssetReference).GetProperty(nameof(AssetReference.Id))?.SetValue(reference, id);
        reference.Content = content;
        reference.OriginalFilename = "document.pdf";
        return reference;
    }

    private static AssetContent CreateContent(Guid id, string mimeType, long sizeBytes, AssetKind? kind = null)
    {
        var content = new AssetContent(
            "assets",
            $"documents/{id:N}",
            id.ToString("N"),
            mimeType,
            sizeBytes,
            kind == AssetKind.Image ? 640 : null,
            kind == AssetKind.Image ? 480 : null);
        typeof(AssetContent).GetProperty(nameof(AssetContent.Id))?.SetValue(content, id);
        content.SetVirusScanStatus(VirusScanStatus.Clean);
        content.SetModerationStatus(ModerationStatus.Approved);

        return content;
    }

    private sealed class AssetsQueryTestDbContext(DbContextOptions<AssetsQueryTestDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AssetContent>();
            modelBuilder.Entity<AssetReference>()
                .HasOne(reference => reference.Content)
                .WithMany(content => content.References)
                .HasForeignKey(reference => reference.AssetContentId);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
