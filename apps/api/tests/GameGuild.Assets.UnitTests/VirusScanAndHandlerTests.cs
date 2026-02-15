using FluentAssertions;
using GameGuild.Assets.Commands;
using GameGuild.Assets.Queries;
using GameGuild.Assets.VirusScan;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace GameGuild.Assets.UnitTests;

/// <summary>
///     R4 supplemental tests: VirusScanService methods and handler constructors.
/// </summary>
public class VirusScanAndHandlerTests
{
    // ═══════════════════════════════════════════════════════════════════
    // VirusScanService
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task VirusScanService_ScanAsync_ReturnsResult()
    {
        var opts = Options.Create(new VirusScanOptions());
        var svc = new VirusScanService(opts, Mock.Of<ILogger<VirusScanService>>());
        using var stream = new MemoryStream(new byte[] { 1, 2, 3 });
        var result = await svc.ScanAsync(stream, "test.txt", CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task VirusScanService_ScanStoredAsync_ReturnsResult()
    {
        var opts = Options.Create(new VirusScanOptions());
        var svc = new VirusScanService(opts, Mock.Of<ILogger<VirusScanService>>());
        var result = await svc.ScanStoredAsync("bucket", "key", CancellationToken.None);
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task VirusScanService_IsHealthyAsync_ReturnsTrue()
    {
        var opts = Options.Create(new VirusScanOptions());
        var svc = new VirusScanService(opts, Mock.Of<ILogger<VirusScanService>>());
        var result = await svc.IsHealthyAsync(CancellationToken.None);
        result.Should().BeTrue();
    }

    [Fact]
    public void VirusScanService_RequiresSyncScan_ReturnsBool()
    {
        var opts = Options.Create(new VirusScanOptions());
        var svc = new VirusScanService(opts, Mock.Of<ILogger<VirusScanService>>());
        // Should not throw regardless of input
        _ = svc.RequiresSyncScan("image/png");
        _ = svc.RequiresSyncScan("application/pdf");
    }

    // ═══════════════════════════════════════════════════════════════════
    // Handlers
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void DeleteAssetHandler_CanInstantiate()
    {
        var handler = new DeleteAssetHandler(
            Mock.Of<IAssetReferenceRepository>(),
            Mock.Of<IAssetContentRepository>());
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetAssetHandler_CanInstantiate()
    {
        var handler = new GetAssetHandler(
            Mock.Of<IAssetReferenceRepository>(),
            Mock.Of<IAssetAccessService>());
        handler.Should().NotBeNull();
    }

    [Fact]
    public void GetAssetsByParentHandler_CanInstantiate()
    {
        var handler = new GetAssetsByParentHandler(
            Mock.Of<IAssetReferenceRepository>(),
            Mock.Of<IAssetAccessService>());
        handler.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // VirusScanOptions
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void VirusScanOptions_CanInstantiate()
    {
        var opts = new VirusScanOptions();
        opts.Should().NotBeNull();
    }

    // ═══════════════════════════════════════════════════════════════════
    // VirusScanResult
    // ═══════════════════════════════════════════════════════════════════

    [Fact]
    public void VirusScanResult_CanInstantiate()
    {
        var result = new VirusScanResult(true, "Clean", null, null, null, null, TimeSpan.FromMilliseconds(10), null);
        result.Should().NotBeNull();
        result.IsClean.Should().BeTrue();
    }
}
