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
        content.ReferenceCount.Should().Be(0);
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
        content.VirusScanCompletedAt.Should().NotBeNull();
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
        content.VirusScanCompletedAt.Should().NotBeNull();
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
        content.ModerationCompletedAt.Should().NotBeNull();
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
    public void SetModerationStatus_WithReviewer_ShouldPersistAuditContext()
    {
        var content = CreateTestContent();
        var reviewerId = Guid.NewGuid();

        content.SetModerationStatus(
            ModerationStatus.Blocked,
            reviewerId,
            ["policy"],
            "  Policy violation reviewed by admin.  ");

        content.ModerationStatus.Should().Be(ModerationStatus.Blocked);
        content.ModerationReviewedBy.Should().Be(reviewerId);
        content.ModerationReviewedAt.Should().Be(content.ModerationCompletedAt);
        content.ModerationReviewNotes.Should().Be("Policy violation reviewed by admin.");
        content.ModerationLabelsList.Should().ContainSingle("policy");
    }

    [Fact]
    public void Kind_ForImageMimeType_ShouldBeImage()
    {
        // Arrange
        var content = CreateTestContent("image/jpeg");

        // Act & Assert
        content.Kind.Should().Be(AssetKind.Image);
    }

    [Fact]
    public void Kind_ForPdfMimeType_ShouldBeDocument()
    {
        // Arrange
        var content = CreateTestContent("application/pdf");

        // Act & Assert
        content.Kind.Should().Be(AssetKind.Document);
    }

    [Fact]
    public void Kind_ForVideoMimeType_ShouldBeVideo()
    {
        // Arrange
        var content = CreateTestContent("video/mp4");

        // Act & Assert
        content.Kind.Should().Be(AssetKind.Video);
    }

    [Fact]
    public void Kind_ForAudioMimeType_ShouldBeAudio()
    {
        // Arrange
        var content = CreateTestContent("audio/mpeg");

        // Act & Assert
        content.Kind.Should().Be(AssetKind.Audio);
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
