using GameGuild.Assets.Commands;

namespace GameGuild.Assets.UnitTests.Commands;

public class GenerateAccessUrlCommandHandlerTests
{
    private readonly Mock<IAssetAccessService> _accessServiceMock;
    private readonly GenerateAccessUrlHandler _handler;

    public GenerateAccessUrlCommandHandlerTests()
    {
        _accessServiceMock = new Mock<IAssetAccessService>();
        _handler = new GenerateAccessUrlHandler(_accessServiceMock.Object);
    }

    [Fact]
    public async Task Handle_GeneratesAccessUrl_ReturnsResponse()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new GenerateAccessUrlCommand(
            assetReferenceId,
            userId,
            tenantId,
            Transformation: null,
            DirectStorageUrl: false);

        var accessUrl = new AssetAccessUrl(
            "https://cdn.example.com/asset/123",
            "token123",
            DateTimeOffset.UtcNow.AddHours(1),
            "image/png");

        _accessServiceMock
            .Setup(x => x.GenerateAccessUrlAsync(
                assetReferenceId,
                userId,
                tenantId,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Url.Should().Be(accessUrl.Url);
        result.Token.Should().Be(accessUrl.Token);
        result.ExpiresAt.Should().Be(accessUrl.ExpiresAt);
        result.MimeType.Should().Be(accessUrl.MimeType);
    }

    [Fact]
    public async Task Handle_GeneratesDirectStorageUrl_ReturnsResponse()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new GenerateAccessUrlCommand(
            assetReferenceId,
            userId,
            tenantId,
            Transformation: null,
            DirectStorageUrl: true);

        var accessUrl = new AssetAccessUrl(
            "https://s3.example.com/bucket/asset?signed=true",
            "",
            DateTimeOffset.UtcNow.AddHours(1),
            "image/png");

        _accessServiceMock
            .Setup(x => x.GenerateDirectStorageUrlAsync(
                assetReferenceId,
                userId,
                tenantId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Url.Should().Be(accessUrl.Url);
        result.Token.Should().BeNull(); // Empty token becomes null
        result.MimeType.Should().Be(accessUrl.MimeType);
    }

    [Fact]
    public async Task Handle_AccessUrlGenerationFails_ReturnsNull()
    {
        // Arrange
        var command = new GenerateAccessUrlCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Transformation: null,
            DirectStorageUrl: false);

        _accessServiceMock
            .Setup(x => x.GenerateAccessUrlAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<TransformationSpec?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAccessUrl?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_DirectStorageUrlGenerationFails_ReturnsNull()
    {
        // Arrange
        var command = new GenerateAccessUrlCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Transformation: null,
            DirectStorageUrl: true);

        _accessServiceMock
            .Setup(x => x.GenerateDirectStorageUrlAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetAccessUrl?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithTransformation_PassesToService()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var transformation = new TransformationSpec { Width = 100, Height = 100, Format = ImageFormat.Webp, Quality = 90 };
        var command = new GenerateAccessUrlCommand(
            assetReferenceId,
            userId,
            tenantId,
            Transformation: transformation,
            DirectStorageUrl: false);

        var accessUrl = new AssetAccessUrl(
            "https://cdn.example.com/asset/123?w=100&h=100",
            "token123",
            DateTimeOffset.UtcNow.AddHours(1),
            "image/webp");

        _accessServiceMock
            .Setup(x => x.GenerateAccessUrlAsync(
                assetReferenceId,
                userId,
                tenantId,
                transformation,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.MimeType.Should().Be("image/webp");
        _accessServiceMock.Verify(x => x.GenerateAccessUrlAsync(
            assetReferenceId,
            userId,
            tenantId,
            transformation,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NullUserId_StillGeneratesUrl()
    {
        // Arrange
        var assetReferenceId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var command = new GenerateAccessUrlCommand(
            assetReferenceId,
            UserId: null,
            TenantId: tenantId,
            Transformation: null,
            DirectStorageUrl: false);

        var accessUrl = new AssetAccessUrl(
            "https://cdn.example.com/public/asset",
            "token123",
            DateTimeOffset.UtcNow.AddHours(1),
            "image/png");

        _accessServiceMock
            .Setup(x => x.GenerateAccessUrlAsync(
                assetReferenceId,
                null,
                tenantId,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Url.Should().Be(accessUrl.Url);
    }

    [Fact]
    public async Task Handle_TokenIsWhitespace_ReturnsToken()
    {
        // Arrange
        var command = new GenerateAccessUrlCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Transformation: null,
            DirectStorageUrl: false);

        var accessUrl = new AssetAccessUrl(
            "https://cdn.example.com/asset",
            "   ", // whitespace token - handler uses IsNullOrEmpty, so whitespace passes through
            DateTimeOffset.UtcNow.AddHours(1),
            "image/png");

        _accessServiceMock
            .Setup(x => x.GenerateAccessUrlAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid?>(),
                It.IsAny<Guid?>(),
                It.IsAny<TransformationSpec?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessUrl);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert - handler uses string.IsNullOrEmpty, not IsNullOrWhiteSpace
        result.Should().NotBeNull();
        result!.Token.Should().Be("   ");
    }
}
