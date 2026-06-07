using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using GameGuild.Assets;
using GameGuild.Assets.Controllers;
using GameGuild.Assets.Moderation;
using GameGuild.Assets.Security;
using GameGuild.Assets.Storage;

namespace GameGuild.Assets.UnitTests;

public class DtoAndValidatorTests
{
    // ═══════════════════════════════════════════════════════════════════
    // Controller constructors
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AssetsCdnController_CanBeConstructed()
    {
        var ctrl = new AssetsCdnController(
            Mock.Of<IAssetAccessService>(),
            Mock.Of<IAssetStorageService>(),
            Mock.Of<IAssetContentRepository>(),
            Mock.Of<IAssetReferenceRepository>());
        ctrl.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Service constructors
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void CommerceOrderValidationService_CanBeConstructed()
    {
        var svc = new CommerceOrderValidationService(Mock.Of<GameGuild.Commerce.Orders.IOrderRepository>());
        svc.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Positional Records (DTOs)
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void StorageTestResult_CanBeConstructed()
    {
        var r = new StorageTestResult(true, true, true, true, true);
        r.Success.Should().BeTrue();
        r.CanRead.Should().BeTrue();
        r.CanWrite.Should().BeTrue();
        r.CanDelete.Should().BeTrue();
        r.BucketExists.Should().BeTrue();
        r.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void StorageTestResult_WithOptionalParams()
    {
        var r = new StorageTestResult(false, false, false, false, false,
            ErrorMessage: "connection failed", Latency: TimeSpan.FromMilliseconds(500));
        r.Success.Should().BeFalse();
        r.ErrorMessage.Should().Be("connection failed");
        r.Latency.Should().NotBeNull();
    }

    [Fact]
    public void SecureUploadResult_Success()
    {
        var r = new SecureUploadResult(true, Guid.NewGuid(), Guid.NewGuid());
        r.Success.Should().BeTrue();
        r.AssetReferenceId.Should().NotBeNull();
        r.AssetContentId.Should().NotBeNull();
        r.Error.Should().BeNull();
    }

    [Fact]
    public void SecureUploadResult_Failure()
    {
        var r = new SecureUploadResult(false, null, null,
            Error: "virus detected",
            Status: SecureUploadStatus.Rejected);
        r.Success.Should().BeFalse();
        r.Error.Should().Be("virus detected");
    }

    [Fact]
    public void GarbageCollectionResult_CanBeConstructed()
    {
        var r = new GarbageCollectionResult(100, 50, 40, 10, TimeSpan.FromMinutes(5),
            new List<string> { "processed batch 1", "processed batch 2" });
        r.ItemsProcessed.Should().Be(100);
        r.ItemsDeleted.Should().Be(50);
        r.ItemsSkipped.Should().Be(40);
        r.Errors.Should().Be(10);
        r.Duration.Should().Be(TimeSpan.FromMinutes(5));
        r.Messages.Should().HaveCount(2);
    }

    [Fact]
    public void StorageUploadResult_CanBeConstructed()
    {
        var r = new StorageUploadResult("my-bucket", "assets/file.png");
        r.BucketName.Should().Be("my-bucket");
        r.ObjectKey.Should().Be("assets/file.png");
    }

    [Fact]
    public void StorageUploadResult_WithOptionalParams()
    {
        var r = new StorageUploadResult("bucket", "key", ETag: "abc123", SizeBytes: 1024);
        r.ETag.Should().Be("abc123");
        r.SizeBytes.Should().Be(1024);
    }

    [Fact]
    public void StorageMetadata_CanBeConstructed()
    {
        var r = new StorageMetadata(2048, "image/png", "etag-1", DateTime.UtcNow);
        r.SizeBytes.Should().Be(2048);
        r.MimeType.Should().Be("image/png");
        r.ETag.Should().Be("etag-1");
    }

    [Fact]
    public void DownloadWindowValidationResult_Valid()
    {
        var r = new DownloadWindowValidationResult(true);
        r.IsValid.Should().BeTrue();
        r.Error.Should().BeNull();
    }

    [Fact]
    public void DownloadWindowValidationResult_Invalid()
    {
        var r = new DownloadWindowValidationResult(false, Error: "expired",
            ExpiresAt: DateTime.UtcNow, OrderId: Guid.NewGuid());
        r.IsValid.Should().BeFalse();
        r.Error.Should().Be("expired");
        r.ExpiresAt.Should().NotBeNull();
        r.OrderId.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Options / Config
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void AutoModerationOptions_CanBeInstantiated()
    {
        var opts = new AutoModerationOptions();
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // Validators
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void GetUserAssetsValidator_CanBeInstantiated()
    {
        var v = new GameGuild.Assets.Queries.GetUserAssetsValidator();
        v.Should().NotBeNull();
    }

    [Fact]
    public void GetAssetValidator_CanBeInstantiated()
    {
        var v = new GameGuild.Assets.Queries.GetAssetValidator();
        v.Should().NotBeNull();
    }

    [Fact]
    public void GetAssetsByParentValidator_CanBeInstantiated()
    {
        var v = new GameGuild.Assets.Queries.GetAssetsByParentValidator();
        v.Should().NotBeNull();
    }
}
