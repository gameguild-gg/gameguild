using FluentAssertions;
using GameGuild.Assets.Extensions;
using GameGuild.Assets.Security;
using GameGuild.Assets.Storage;
using GameGuild.Commerce.Orders;
using Moq;
using Xunit;
using AssetOrderStatus = GameGuild.Assets.Security.OrderStatus;

namespace GameGuild.Assets.UnitTests;

/// <summary>
/// R5 tests targeting StorageUploadResult, StorageMetadata, CommerceOrderValidationService
/// to push coverage past 75%.
/// </summary>
public class StorageRecordAndPlaceholderTests
{
    // ─── StorageUploadResult ───────────────────────────────────────────

    [Fact]
    public void StorageUploadResult_CanCreate_WithRequiredParams()
    {
        var result = new StorageUploadResult("my-bucket", "objects/key.png");
        result.BucketName.Should().Be("my-bucket");
        result.ObjectKey.Should().Be("objects/key.png");
        result.ETag.Should().BeNull();
        result.SizeBytes.Should().BeNull();
    }

    [Fact]
    public void StorageUploadResult_CanCreate_WithAllParams()
    {
        var result = new StorageUploadResult("bucket", "key", "etag-123", 1024L);
        result.BucketName.Should().Be("bucket");
        result.ObjectKey.Should().Be("key");
        result.ETag.Should().Be("etag-123");
        result.SizeBytes.Should().Be(1024L);
    }

    [Fact]
    public void StorageUploadResult_With_CreatesModifiedCopy()
    {
        var original = new StorageUploadResult("b1", "k1");
        var modified = original with { BucketName = "b2", SizeBytes = 2048L };
        modified.BucketName.Should().Be("b2");
        modified.ObjectKey.Should().Be("k1");
        modified.SizeBytes.Should().Be(2048L);
    }

    [Fact]
    public void StorageUploadResult_Equality_Works()
    {
        var a = new StorageUploadResult("b", "k", "e", 10);
        var b = new StorageUploadResult("b", "k", "e", 10);
        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void StorageUploadResult_ToString_ContainsFields()
    {
        var r = new StorageUploadResult("buck", "obj");
        r.ToString().Should().Contain("buck");
        r.ToString().Should().Contain("obj");
    }

    [Fact]
    public void StorageUploadResult_Deconstruct_Works()
    {
        var r = new StorageUploadResult("b", "k", "e", 42L);
        var (bucket, key, etag, size) = r;
        bucket.Should().Be("b");
        key.Should().Be("k");
        etag.Should().Be("e");
        size.Should().Be(42L);
    }

    // ─── StorageMetadata ──────────────────────────────────────────────

    [Fact]
    public void StorageMetadata_CanCreate_WithAllParams()
    {
        var now = DateTime.UtcNow;
        var meta = new StorageMetadata(4096L, "image/png", "etag-456", now);
        meta.SizeBytes.Should().Be(4096L);
        meta.MimeType.Should().Be("image/png");
        meta.ETag.Should().Be("etag-456");
        meta.LastModified.Should().Be(now);
    }

    [Fact]
    public void StorageMetadata_With_CreatesModifiedCopy()
    {
        var meta = new StorageMetadata(100, "text/plain", "e1", DateTime.UtcNow);
        var modified = meta with { MimeType = "application/json", SizeBytes = 200 };
        modified.MimeType.Should().Be("application/json");
        modified.SizeBytes.Should().Be(200);
        modified.ETag.Should().Be("e1");
    }

    [Fact]
    public void StorageMetadata_Equality_Works()
    {
        var now = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var a = new StorageMetadata(100, "text/plain", "e", now);
        var b = new StorageMetadata(100, "text/plain", "e", now);
        a.Should().Be(b);
    }

    [Fact]
    public void StorageMetadata_ToString_ContainsMimeType()
    {
        var meta = new StorageMetadata(0, "video/mp4", "e", DateTime.UtcNow);
        meta.ToString().Should().Contain("video/mp4");
    }

    [Fact]
    public void StorageMetadata_Deconstruct_Works()
    {
        var now = DateTime.UtcNow;
        var meta = new StorageMetadata(512, "app/zip", "etag", now);
        var (size, mime, etag, lastMod) = meta;
        size.Should().Be(512);
        mime.Should().Be("app/zip");
        etag.Should().Be("etag");
        lastMod.Should().Be(now);
    }

    // ─── PlaceholderOrderValidationService ────────────────────────────

    [Fact]
    public async Task CommerceOrderValidationService_GetOrderStatusAsync_ReturnsFulfilled()
    {
        var orderId = Guid.NewGuid();
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: true));
        var svc = new CommerceOrderValidationService(repo.Object);

        var status = await svc.GetOrderStatusAsync(orderId);

        status.Should().Be(AssetOrderStatus.Fulfilled);
    }

    [Fact]
    public async Task CommerceOrderValidationService_IsOrderValidForDownloadAsync_ReturnsTrue()
    {
        var orderId = Guid.NewGuid();
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: true));
        var svc = new CommerceOrderValidationService(repo.Object);

        var valid = await svc.IsOrderValidForDownloadAsync(orderId);

        valid.Should().BeTrue();
    }

    [Fact]
    public async Task CommerceOrderValidationService_GetOrderStatus_WithCancellationToken()
    {
        var orderId = Guid.NewGuid();
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: true));
        var svc = new CommerceOrderValidationService(repo.Object);
        using var cts = new CancellationTokenSource();

        var status = await svc.GetOrderStatusAsync(orderId, cts.Token);

        status.Should().NotBeNull();
    }

    [Fact]
    public async Task CommerceOrderValidationService_IsOrderValid_WithCancellationToken()
    {
        var orderId = Guid.NewGuid();
        var repo = new Mock<IOrderRepository>();
        repo.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateOrder(orderId, fulfilled: true));
        var svc = new CommerceOrderValidationService(repo.Object);
        using var cts = new CancellationTokenSource();

        var result = await svc.IsOrderValidForDownloadAsync(orderId, cts.Token);

        result.Should().BeTrue();
    }

    private static Order CreateOrder(Guid id, bool fulfilled = false)
    {
        var order = Order.Create(Guid.NewGuid(), $"idem-{Guid.NewGuid():N}", Guid.NewGuid());
        typeof(Order).GetProperty(nameof(Order.Id))!.SetValue(order, id);
        order.MarkAsPaidPendingFulfillment(Guid.NewGuid());
        if (fulfilled)
        {
            order.MarkAsFulfilled();
        }

        return order;
    }

    // ─── TenantValidationResult ───────────────────────────────────────

    [Fact]
    public void TenantValidationResult_Valid_HasNoError()
    {
        var result = new TenantValidationResult(true, null, Guid.NewGuid());
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
        result.ResolvedTenantId.Should().NotBeNull();
    }

    [Fact]
    public void TenantValidationResult_Invalid_HasError()
    {
        var result = new TenantValidationResult(false, "Tenant mismatch");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Tenant mismatch");
        result.ResolvedTenantId.Should().BeNull();
    }

    [Fact]
    public void TenantValidationResult_Equality()
    {
        var tid = Guid.NewGuid();
        var a = new TenantValidationResult(true, null, tid);
        var b = new TenantValidationResult(true, null, tid);
        a.Should().Be(b);
    }

    [Fact]
    public void TenantValidationResult_Deconstruct()
    {
        var tid = Guid.NewGuid();
        var r = new TenantValidationResult(true, "ok", tid);
        var (isValid, error, resolved) = r;
        isValid.Should().BeTrue();
        error.Should().Be("ok");
        resolved.Should().Be(tid);
    }

    // ─── AssetsModule ────────────────────────────────────────────────

    [Fact]
    public void AssetsModule_Name_IsAssets()
    {
        var module = new AssetsModule();
        module.Name.Should().Be("Assets");
    }
}
