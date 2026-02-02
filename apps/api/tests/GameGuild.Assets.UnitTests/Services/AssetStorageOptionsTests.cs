using FluentAssertions;
using Xunit;

namespace GameGuild.Assets.UnitTests.Services;

/// <summary>
/// Unit tests for AssetStorageOptions configuration class
/// </summary>
public class AssetStorageOptionsTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrectlySet()
    {
        // Arrange & Act
        var options = new AssetStorageOptions();

        // Assert
        options.BucketName.Should().Be("assets");
        options.TransformedBucketName.Should().Be("assets-transformed");
        options.ServiceUrl.Should().BeEmpty();
        options.AccessKey.Should().BeEmpty();
        options.SecretKey.Should().BeEmpty();
        options.Region.Should().Be("us-east-1");
        options.ForcePathStyle.Should().BeTrue();
        options.PresignedUrlExpiryMinutes.Should().Be(60);
    }

    [Fact]
    public void SectionName_ShouldBeCorrect()
    {
        // Assert
        AssetStorageOptions.SectionName.Should().Be("Assets:Storage");
    }

    [Theory]
    [InlineData("my-bucket")]
    [InlineData("production-assets")]
    [InlineData("dev-bucket-123")]
    public void BucketName_ShouldAcceptValidValues(string bucketName)
    {
        // Arrange
        var options = new AssetStorageOptions();

        // Act
        options.BucketName = bucketName;

        // Assert
        options.BucketName.Should().Be(bucketName);
    }

    [Theory]
    [InlineData("https://s3.amazonaws.com")]
    [InlineData("http://localhost:9000")]
    [InlineData("https://minio.example.com")]
    public void ServiceUrl_ShouldAcceptValidUrls(string serviceUrl)
    {
        // Arrange
        var options = new AssetStorageOptions();

        // Act
        options.ServiceUrl = serviceUrl;

        // Assert
        options.ServiceUrl.Should().Be(serviceUrl);
    }

    [Theory]
    [InlineData("us-east-1")]
    [InlineData("eu-west-1")]
    [InlineData("ap-southeast-1")]
    public void Region_ShouldAcceptValidRegions(string region)
    {
        // Arrange
        var options = new AssetStorageOptions();

        // Act
        options.Region = region;

        // Assert
        options.Region.Should().Be(region);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(30)]
    [InlineData(120)]
    public void PresignedUrlExpiryMinutes_ShouldAcceptPositiveValues(int minutes)
    {
        // Arrange
        var options = new AssetStorageOptions();

        // Act
        options.PresignedUrlExpiryMinutes = minutes;

        // Assert
        options.PresignedUrlExpiryMinutes.Should().Be(minutes);
    }

    [Fact]
    public void ForcePathStyle_ShouldBeConfigurable()
    {
        // Arrange
        var options = new AssetStorageOptions();

        // Act
        options.ForcePathStyle = false;

        // Assert
        options.ForcePathStyle.Should().BeFalse();
    }
}
