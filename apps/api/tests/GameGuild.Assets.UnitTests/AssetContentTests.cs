namespace GameGuild.Assets.UnitTests;

public class AssetContentTests
{
    [Fact]
    public void Constructor_WithValidParameters_ShouldCreateInstance()
    {
        // Arrange & Act
        var content = new AssetContent(
            "test-bucket",
            "content/ab/cd/abcd1234.jpg",
            "abcd1234567890abcd1234567890abcd1234567890abcd1234567890abcd1234",
            "image/jpeg",
            1024,
            800,
            600);

        // Assert
        content.BucketName.Should().Be("test-bucket");
        content.ObjectKey.Should().Be("content/ab/cd/abcd1234.jpg");
        content.ContentHash.Should().Be("abcd1234567890abcd1234567890abcd1234567890abcd1234567890abcd1234");
        content.MimeType.Should().Be("image/jpeg");
        content.SizeBytes.Should().Be(1024);
        content.Width.Should().Be(800);
        content.Height.Should().Be(600);
        content.VirusScanStatus.Should().Be(VirusScanStatus.Pending);
        content.ModerationStatus.Should().Be(ModerationStatus.Pending);
        content.ReferenceCount.Should().Be(1);
        content.IsDeletable.Should().BeTrue();
    }

    [Fact]
    public void Constructor_WithoutDimensions_ShouldHaveNullDimensions()
    {
        // Arrange & Act
        var content = new AssetContent(
            "test-bucket",
            "content/ab/cd/abcd1234.pdf",
            "abcd1234567890abcd1234567890abcd1234567890abcd1234567890abcd1234",
            "application/pdf",
            2048,
            null,
            null);

        // Assert
        content.Width.Should().BeNull();
        content.Height.Should().BeNull();
    }

    [Fact]
    public void SetVirusScanStatus_ToClean_ShouldUpdateStatus()
    {
        // Arrange
        var content = CreateTestContent();

        // Act
        content.SetVirusScanStatus(VirusScanStatus.Clean, "No threats detected");

        // Assert
        content.VirusScanStatus.Should().Be(VirusScanStatus.Clean);
        content.VirusScanResult.Should().Be("No threats detected");
    }

    [Fact]
    public void SetVirusScanStatus_ToInfected_ShouldUpdateStatus()
    {
        // Arrange
        var content = CreateTestContent();

        // Act
        content.SetVirusScanStatus(VirusScanStatus.Infected, "Malware detected: Trojan.Generic");

        // Assert
        content.VirusScanStatus.Should().Be(VirusScanStatus.Infected);
        content.VirusScanResult.Should().Contain("Malware");
    }

    [Fact]
    public void SetModerationStatus_ToApproved_ShouldUpdateStatus()
    {
        // Arrange
        var content = CreateTestContent();

        // Act
        content.SetModerationStatus(ModerationStatus.Approved);

        // Assert
        content.ModerationStatus.Should().Be(ModerationStatus.Approved);
    }

    [Fact]
    public void SetModerationStatus_ToBlocked_ShouldUpdateStatus()
    {
        // Arrange
        var content = CreateTestContent();

        // Act
        content.SetModerationStatus(ModerationStatus.Blocked);

        // Assert
        content.ModerationStatus.Should().Be(ModerationStatus.Blocked);
    }

    [Fact]
    public void IsImage_WithImageMimeType_ShouldReturnTrue()
    {
        // Arrange
        var content = CreateTestContent("image/jpeg");

        // Act & Assert
        content.IsImage.Should().BeTrue();
    }

    [Fact]
    public void IsImage_WithNonImageMimeType_ShouldReturnFalse()
    {
        // Arrange
        var content = CreateTestContent("application/pdf");

        // Act & Assert
        content.IsImage.Should().BeFalse();
    }

    [Fact]
    public void IsVideo_WithVideoMimeType_ShouldReturnTrue()
    {
        // Arrange
        var content = CreateTestContent("video/mp4");

        // Act & Assert
        content.IsVideo.Should().BeTrue();
    }

    [Fact]
    public void IsVideo_WithNonVideoMimeType_ShouldReturnFalse()
    {
        // Arrange
        var content = CreateTestContent("image/png");

        // Act & Assert
        content.IsVideo.Should().BeFalse();
    }

    [Fact]
    public void IsAudio_WithAudioMimeType_ShouldReturnTrue()
    {
        // Arrange
        var content = CreateTestContent("audio/mpeg");

        // Act & Assert
        content.IsAudio.Should().BeTrue();
    }

    private static AssetContent CreateTestContent(string mimeType = "image/jpeg")
    {
        return new AssetContent(
            "test-bucket",
            "content/ab/cd/test.jpg",
            "abcd1234567890abcd1234567890abcd1234567890abcd1234567890abcd1234",
            mimeType,
            1024,
            800,
            600);
    }
}
