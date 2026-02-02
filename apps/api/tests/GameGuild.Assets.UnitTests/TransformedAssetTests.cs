using FluentAssertions;
using Xunit;

namespace GameGuild.Assets.UnitTests;

/// <summary>
/// Unit tests for TransformedAsset entity
/// </summary>
public class TransformedAssetTests
{
    [Fact]
    public void DefaultConstructor_ShouldCreateInstanceWithDefaults()
    {
        // Arrange & Act
        var asset = new TransformedAsset();

        // Assert
        asset.SourceContentId.Should().Be(Guid.Empty);
        asset.TransformationSpec.Should().BeEmpty();
        asset.BucketName.Should().BeEmpty();
        asset.ObjectKey.Should().BeEmpty();
        asset.MimeType.Should().BeEmpty();
        asset.SizeBytes.Should().Be(0);
    }

    [Fact]
    public void Properties_WhenSet_ShouldRetainValues()
    {
        // Arrange
        var sourceContentId = Guid.NewGuid();
        var transformationSpec = "w=200&h=200&fit=cover";
        var bucketName = "assets-transformed";
        var objectKey = "transformed/abc123.webp";
        var mimeType = "image/webp";
        var sizeBytes = 50000L;

        // Act
        var asset = new TransformedAsset
        {
            SourceContentId = sourceContentId,
            TransformationSpec = transformationSpec,
            BucketName = bucketName,
            ObjectKey = objectKey,
            MimeType = mimeType,
            SizeBytes = sizeBytes
        };

        // Assert
        asset.SourceContentId.Should().Be(sourceContentId);
        asset.TransformationSpec.Should().Be(transformationSpec);
        asset.BucketName.Should().Be(bucketName);
        asset.ObjectKey.Should().Be(objectKey);
        asset.MimeType.Should().Be(mimeType);
        asset.SizeBytes.Should().Be(sizeBytes);
    }
}

/// <summary>
/// Unit tests for AssetKind enum
/// </summary>
public class AssetKindTests
{
    [Theory]
    [InlineData(AssetKind.Image)]
    [InlineData(AssetKind.Video)]
    [InlineData(AssetKind.Audio)]
    [InlineData(AssetKind.Document)]
    [InlineData(AssetKind.Archive)]
    [InlineData(AssetKind.Other)]
    public void AssetKind_ShouldHaveExpectedValues(AssetKind kind)
    {
        // Assert
        Enum.IsDefined(typeof(AssetKind), kind).Should().BeTrue();
    }

    [Fact]
    public void AssetKind_ShouldHaveCorrectCount()
    {
        // Assert
        Enum.GetValues<AssetKind>().Should().HaveCount(6);
    }
}

/// <summary>
/// Unit tests for VirusScanStatus enum
/// </summary>
public class VirusScanStatusTests
{
    [Theory]
    [InlineData(VirusScanStatus.Pending)]
    [InlineData(VirusScanStatus.Scanning)]
    [InlineData(VirusScanStatus.Clean)]
    [InlineData(VirusScanStatus.Infected)]
    [InlineData(VirusScanStatus.ScanFailed)]
    public void VirusScanStatus_ShouldHaveExpectedValues(VirusScanStatus status)
    {
        // Assert
        Enum.IsDefined(typeof(VirusScanStatus), status).Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for ModerationStatus enum
/// </summary>
public class ModerationStatusTests
{
    [Theory]
    [InlineData(ModerationStatus.Pending)]
    [InlineData(ModerationStatus.Approved)]
    [InlineData(ModerationStatus.Blocked)]
    [InlineData(ModerationStatus.NeedsReview)]
    public void ModerationStatus_ShouldHaveExpectedValues(ModerationStatus status)
    {
        // Assert
        Enum.IsDefined(typeof(ModerationStatus), status).Should().BeTrue();
    }
}

/// <summary>
/// Unit tests for AssetAccessPolicy enum
/// </summary>
public class AssetAccessPolicyTests
{
    [Theory]
    [InlineData(AssetAccessPolicy.Private)]
    [InlineData(AssetAccessPolicy.Public)]
    [InlineData(AssetAccessPolicy.Unlisted)]
    [InlineData(AssetAccessPolicy.Authenticated)]
    public void AssetAccessPolicy_ShouldHaveExpectedValues(AssetAccessPolicy policy)
    {
        // Assert
        Enum.IsDefined(typeof(AssetAccessPolicy), policy).Should().BeTrue();
    }
}
