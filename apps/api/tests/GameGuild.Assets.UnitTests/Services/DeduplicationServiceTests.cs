using FluentAssertions;
using GameGuild.Assets.Deduplication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Xunit;

namespace GameGuild.Assets.UnitTests.Services;

public class DeduplicationServiceTests
{
    private readonly Mock<IAssetContentRepository> _contentRepositoryMock;
    private readonly Mock<ILogger<DeduplicationService>> _loggerMock;
    private readonly DeduplicationOptions _options;
    private readonly DeduplicationService _service;

    public DeduplicationServiceTests()
    {
        _contentRepositoryMock = new Mock<IAssetContentRepository>();
        _loggerMock = new Mock<ILogger<DeduplicationService>>();
        _options = new DeduplicationOptions { Enabled = true, EnablePerceptualHashing = true };
        var optionsMock = Options.Create(_options);
        _service = new DeduplicationService(_contentRepositoryMock.Object, optionsMock, _loggerMock.Object);
    }

    [Fact]
    public async Task ComputeContentHashAsync_ComputesSHA256Hash()
    {
        // Arrange
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));

        // Act
        var hash = await _service.ComputeContentHashAsync(content);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64); // SHA-256 produces 64 hex characters
        hash.Should().MatchRegex("^[a-f0-9]+$"); // Only lowercase hex
    }

    [Fact]
    public async Task ComputeContentHashAsync_ResetsStreamPosition()
    {
        // Arrange
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("test content"));

        // Act
        await _service.ComputeContentHashAsync(content);

        // Assert
        content.Position.Should().Be(0);
    }

    [Fact]
    public async Task ComputeContentHashAsync_ProducesSameHashForSameContent()
    {
        // Arrange
        var content1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("identical content"));
        var content2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("identical content"));

        // Act
        var hash1 = await _service.ComputeContentHashAsync(content1);
        var hash2 = await _service.ComputeContentHashAsync(content2);

        // Assert
        hash1.Should().Be(hash2);
    }

    [Fact]
    public async Task ComputeContentHashAsync_ProducesDifferentHashForDifferentContent()
    {
        // Arrange
        var content1 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content one"));
        var content2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("content two"));

        // Act
        var hash1 = await _service.ComputeContentHashAsync(content1);
        var hash2 = await _service.ComputeContentHashAsync(content2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_ReturnsNull_WhenDisabled()
    {
        // Arrange
        _options.EnablePerceptualHashing = false;
        var content = new MemoryStream();

        // Act
        var hash = await _service.ComputePerceptualHashAsync(content, "image/png");

        // Assert
        hash.Should().BeNull();
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_ReturnsNull_ForNonImageMimeType()
    {
        // Arrange
        var content = new MemoryStream();

        // Act
        var hash = await _service.ComputePerceptualHashAsync(content, "application/pdf");

        // Assert
        hash.Should().BeNull();
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_DecodesBenignPngAndPreservesDeterminismAndStreamPosition()
    {
        using var content = CreateBenignPng(false);
        var first = await _service.ComputePerceptualHashAsync(content, "image/png");
        first.Should().NotBeNullOrEmpty();
        first.Should().MatchRegex("^[a-f0-9]{16}$");
        content.Position.Should().Be(0);

        var repeated = await _service.ComputePerceptualHashAsync(content, "image/png");
        repeated.Should().Be(first);
        content.Position.Should().Be(0);
    }

    [Fact]
    public async Task ComputePerceptualHashAsync_DistinguishesMirroredImagePatterns()
    {
        using var left = CreateBenignPng(false);
        using var right = CreateBenignPng(true);
        var first = await _service.ComputePerceptualHashAsync(left, "image/png");
        var second = await _service.ComputePerceptualHashAsync(right, "image/png");
        first.Should().NotBeNullOrEmpty();
        second.Should().NotBeNullOrEmpty().And.NotBe(first);
        left.Position.Should().Be(0);
        right.Position.Should().Be(0);
    }

    private static MemoryStream CreateBenignPng(bool mirror)
    {
        using var image = new Image<Rgba32>(8, 8);
        for (var row = 0; row < 8; row++)
        {
            for (var column = 0; column < 8; column++)
            {
                var value = (column < 4) == mirror ? byte.MaxValue : byte.MinValue;
                image[column, row] = new Rgba32(value, value, value);
            }
        }
        var stream = new MemoryStream();
        image.SaveAsPng(stream);
        stream.Position = 0;
        return stream;
    }

    [Fact]
    public async Task FindExistingContentAsync_ReturnsNull_WhenNoMatchFound()
    {
        // Arrange
        var contentHash = "abc123";
        _contentRepositoryMock.Setup(r => r.GetByContentHashAsync(contentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AssetContent?)null);

        // Act
        var result = await _service.FindExistingContentAsync(contentHash);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task FindExistingContentAsync_ReturnsAssetId_WhenMatchFound()
    {
        // Arrange
        var contentHash = "abc123";
        var assetContent = new AssetContent(
            "test-bucket",
            "test/key.jpg",
            contentHash,
            "image/jpeg",
            1024,
            800,
            600);
        
        _contentRepositoryMock.Setup(r => r.GetByContentHashAsync(contentHash, It.IsAny<CancellationToken>()))
            .ReturnsAsync(assetContent);

        // Act
        var result = await _service.FindExistingContentAsync(contentHash);

        // Assert
        result.Should().Be(assetContent.Id);
    }
}
