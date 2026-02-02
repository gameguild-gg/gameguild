using FluentAssertions;
using Xunit;

namespace GameGuild.Assets.UnitTests.Services;

/// <summary>
/// Unit tests for AssetUploadConfiguration class
/// </summary>
public class AssetUploadConfigurationTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrectlySet()
    {
        // Arrange & Act
        var config = new AssetUploadConfiguration();

        // Assert
        config.MaxFileSizeBytes.Should().Be(100 * 1024 * 1024); // 100 MB
        config.ChunkedUploadThreshold.Should().Be(5 * 1024 * 1024); // 5 MB
        config.ChunkSizeBytes.Should().Be(5 * 1024 * 1024); // 5 MB
        config.AllowedMimeTypes.Should().BeEmpty();
        config.ChunkedUploadExpiryMinutes.Should().Be(60);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Assert
        AssetUploadConfiguration.SectionName.Should().Be("Assets:Upload");
    }

    [Theory]
    [InlineData(1024)]
    [InlineData(50 * 1024 * 1024)]
    [InlineData(500 * 1024 * 1024)]
    public void MaxFileSizeBytes_ShouldAcceptValidValues(long maxSize)
    {
        // Arrange
        var config = new AssetUploadConfiguration();

        // Act
        config.MaxFileSizeBytes = maxSize;

        // Assert
        config.MaxFileSizeBytes.Should().Be(maxSize);
    }

    [Fact]
    public void AllowedMimeTypes_ShouldAcceptArray()
    {
        // Arrange
        var config = new AssetUploadConfiguration();
        var mimeTypes = new[] { "image/jpeg", "image/png", "application/pdf" };

        // Act
        config.AllowedMimeTypes = mimeTypes;

        // Assert
        config.AllowedMimeTypes.Should().BeEquivalentTo(mimeTypes);
    }

    [Theory]
    [InlineData(1024 * 1024)]
    [InlineData(10 * 1024 * 1024)]
    public void ChunkSizeBytes_ShouldAcceptValidValues(int chunkSize)
    {
        // Arrange
        var config = new AssetUploadConfiguration();

        // Act
        config.ChunkSizeBytes = chunkSize;

        // Assert
        config.ChunkSizeBytes.Should().Be(chunkSize);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(120)]
    [InlineData(1440)]
    public void ChunkedUploadExpiryMinutes_ShouldAcceptValidValues(int minutes)
    {
        // Arrange
        var config = new AssetUploadConfiguration();

        // Act
        config.ChunkedUploadExpiryMinutes = minutes;

        // Assert
        config.ChunkedUploadExpiryMinutes.Should().Be(minutes);
    }
}

/// <summary>
/// Unit tests for AssetUploadResult record
/// </summary>
public class AssetUploadResultTests
{
    [Fact]
    public void Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var referenceId = Guid.NewGuid();
        var contentId = Guid.NewGuid();

        // Act
        var result = new AssetUploadResult(true, referenceId, contentId, null);

        // Assert
        result.Success.Should().BeTrue();
        result.AssetReferenceId.Should().Be(referenceId);
        result.AssetContentId.Should().Be(contentId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var errorMessage = "File size exceeds maximum allowed";

        // Act
        var result = new AssetUploadResult(false, null, null, errorMessage);

        // Assert
        result.Success.Should().BeFalse();
        result.AssetReferenceId.Should().BeNull();
        result.AssetContentId.Should().BeNull();
        result.Error.Should().Be(errorMessage);
    }
}

/// <summary>
/// Unit tests for ChunkedUploadSession record
/// </summary>
public class ChunkedUploadSessionTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var uploadId = "upload-123";
        var objectKey = "multipart/abc123";
        var userId = Guid.NewGuid();
        var fileName = "large-file.zip";
        var mimeType = "application/zip";
        var totalSize = 100L * 1024 * 1024;
        var totalChunks = 20;
        var expiresAt = DateTime.UtcNow.AddHours(1);

        // Act
        var session = new ChunkedUploadSession(
            uploadId, objectKey, userId, fileName, mimeType,
            totalSize, totalChunks, expiresAt);

        // Assert
        session.UploadId.Should().Be(uploadId);
        session.ObjectKey.Should().Be(objectKey);
        session.UserId.Should().Be(userId);
        session.FileName.Should().Be(fileName);
        session.MimeType.Should().Be(mimeType);
        session.TotalSize.Should().Be(totalSize);
        session.TotalChunks.Should().Be(totalChunks);
        session.ExpiresAt.Should().Be(expiresAt);
        session.UploadedChunks.Should().Be(0);
    }

    [Fact]
    public void WithRecord_ShouldAllowImmutableUpdates()
    {
        // Arrange
        var session = new ChunkedUploadSession(
            "upload-123", "key", Guid.NewGuid(), "file.zip",
            "application/zip", 1000, 10, DateTime.UtcNow.AddHours(1));

        // Act
        var updated = session with { UploadedChunks = 5 };

        // Assert
        updated.UploadedChunks.Should().Be(5);
        session.UploadedChunks.Should().Be(0); // Original unchanged
    }
}

/// <summary>
/// Unit tests for StorageUploadResult record
/// </summary>
public class StorageUploadResultTests
{
    [Fact]
    public void Constructor_ShouldInitializeProperties()
    {
        // Arrange
        var bucketName = "test-bucket";
        var objectKey = "content/ab/cd/file.jpg";

        // Act
        var result = new StorageUploadResult(bucketName, objectKey);

        // Assert
        result.BucketName.Should().Be(bucketName);
        result.ObjectKey.Should().Be(objectKey);
    }
}

/// <summary>
/// Unit tests for StorageMetadata record
/// </summary>
public class StorageMetadataTests
{
    [Fact]
    public void Constructor_ShouldInitializeAllProperties()
    {
        // Arrange
        var sizeBytes = 1024L;
        var mimeType = "image/jpeg";
        var eTag = "\"abc123\"";
        var lastModified = DateTime.UtcNow;

        // Act
        var metadata = new StorageMetadata(sizeBytes, mimeType, eTag, lastModified);

        // Assert
        metadata.SizeBytes.Should().Be(sizeBytes);
        metadata.MimeType.Should().Be(mimeType);
        metadata.ETag.Should().Be(eTag);
        metadata.LastModified.Should().Be(lastModified);
    }
}
