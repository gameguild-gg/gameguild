namespace GameGuild.Assets.UnitTests;

public class TransformationSpecTests
{
    [Fact]
    public void ToCanonicalString_WithDefaultValues_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec(100, 200, ImageFit.Contain, ImageFormat.Original, 85);

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Be("w100h200fcontainOq85");
    }

    [Fact]
    public void ToCanonicalString_WithWidthOnly_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec(100, null, ImageFit.Contain, ImageFormat.Jpeg, 90);

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("w100");
        result.Should().NotContain("h");
    }

    [Fact]
    public void ToCanonicalString_WithHeightOnly_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec(null, 200, ImageFit.Contain, ImageFormat.Png, 80);

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("h200");
        result.Should().NotContain("w");
    }

    [Fact]
    public void Parse_WithValidString_ShouldReturnCorrectSpec()
    {
        // Arrange
        var original = new TransformationSpec(100, 200, ImageFit.Cover, ImageFormat.Webp, 75);
        var canonical = original.ToCanonicalString();

        // Act
        var parsed = TransformationSpec.Parse(canonical);

        // Assert
        parsed.Should().NotBeNull();
        parsed!.Width.Should().Be(100);
        parsed.Height.Should().Be(200);
        parsed.Fit.Should().Be(ImageFit.Cover);
        parsed.Format.Should().Be(ImageFormat.Webp);
        parsed.Quality.Should().Be(75);
    }

    [Fact]
    public void Parse_WithInvalidString_ShouldReturnNull()
    {
        // Act
        var parsed = TransformationSpec.Parse("invalid");

        // Assert
        parsed.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNullString_ShouldReturnNull()
    {
        // Act
        var parsed = TransformationSpec.Parse(null);

        // Assert
        parsed.Should().BeNull();
    }

    [Fact]
    public void IsIdentity_WithNoDimensions_ShouldReturnTrue()
    {
        // Arrange
        var spec = new TransformationSpec(null, null, ImageFit.Contain, ImageFormat.Original, 85);

        // Act & Assert
        spec.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_WithDimensions_ShouldReturnFalse()
    {
        // Arrange
        var spec = new TransformationSpec(100, null, ImageFit.Contain, ImageFormat.Original, 85);

        // Act & Assert
        spec.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_WithinBounds_ShouldReturnTrue()
    {
        // Arrange
        var spec = new TransformationSpec(1000, 1000, ImageFit.Contain, ImageFormat.Original, 85);

        // Act
        var result = spec.IsWithinLimits(2000, 2000);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsWithinLimits_ExceedingWidth_ShouldReturnFalse()
    {
        // Arrange
        var spec = new TransformationSpec(3000, 1000, ImageFit.Contain, ImageFormat.Original, 85);

        // Act
        var result = spec.IsWithinLimits(2000, 2000);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_ExceedingHeight_ShouldReturnFalse()
    {
        // Arrange
        var spec = new TransformationSpec(1000, 3000, ImageFit.Contain, ImageFormat.Original, 85);

        // Act
        var result = spec.IsWithinLimits(2000, 2000);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_WithNullDimensions_ShouldReturnTrue()
    {
        // Arrange
        var spec = new TransformationSpec(null, null, ImageFit.Contain, ImageFormat.Original, 85);

        // Act
        var result = spec.IsWithinLimits(2000, 2000);

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(ImageFit.Contain)]
    [InlineData(ImageFit.Cover)]
    [InlineData(ImageFit.Fill)]
    [InlineData(ImageFit.Inside)]
    [InlineData(ImageFit.Outside)]
    public void ToCanonicalString_AllFitModes_ShouldWork(ImageFit fit)
    {
        // Arrange
        var spec = new TransformationSpec(100, 100, fit, ImageFormat.Original, 85);

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData(ImageFormat.Original)]
    [InlineData(ImageFormat.Jpeg)]
    [InlineData(ImageFormat.Png)]
    [InlineData(ImageFormat.Webp)]
    [InlineData(ImageFormat.Avif)]
    public void ToCanonicalString_AllFormats_ShouldWork(ImageFormat format)
    {
        // Arrange
        var spec = new TransformationSpec(100, 100, ImageFit.Contain, format, 85);

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
