namespace GameGuild.Assets.UnitTests;

public class TransformationSpecTests
{
    [Fact]
    public void ToCanonicalString_WithDefaultValues_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Contain,
            Format = ImageFormat.Original,
            Quality = 85
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("w=100");
        result.Should().Contain("h=200");
        result.Should().Contain("fit=contain");
        result.Should().Contain("q=85");
    }

    [Fact]
    public void ToCanonicalString_WithWidthOnly_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100,
            Fit = ImageFit.Contain,
            Format = ImageFormat.Jpeg,
            Quality = 90
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("w=100");
        result.Should().NotContain("h=");
    }

    [Fact]
    public void ToCanonicalString_WithHeightOnly_ShouldReturnCorrectFormat()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Height = 200,
            Fit = ImageFit.Contain,
            Format = ImageFormat.Png,
            Quality = 80
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("h=200");
        result.Should().NotContain("w=");
    }

    [Fact]
    public void Parse_WithValidString_ShouldReturnCorrectSpec()
    {
        // Arrange
        var original = new TransformationSpec
        {
            Width = 100,
            Height = 200,
            Fit = ImageFit.Cover,
            Format = ImageFormat.Webp,
            Quality = 75
        };
        var canonical = original.ToCanonicalString();

        // Act
        var parsed = TransformationSpec.Parse(canonical);

        // Assert
        parsed.Should().NotBeNull();
        parsed.Width.Should().Be(100);
        parsed.Height.Should().Be(200);
        parsed.Fit.Should().Be(ImageFit.Cover);
        parsed.Format.Should().Be(ImageFormat.Webp);
        parsed.Quality.Should().Be(75);
    }

    [Fact]
    public void Parse_WithEmptyString_ShouldReturnEmptySpec()
    {
        // Act
        var parsed = TransformationSpec.Parse("");

        // Assert
        parsed.Should().NotBeNull();
        parsed.Width.Should().BeNull();
        parsed.Height.Should().BeNull();
    }

    [Fact]
    public void Parse_WithNullString_ShouldReturnEmptySpec()
    {
        // Act
        var parsed = TransformationSpec.Parse(null!);

        // Assert
        parsed.Should().NotBeNull();
        parsed.Width.Should().BeNull();
    }

    [Fact]
    public void IsIdentity_WithNoDimensions_ShouldReturnTrue()
    {
        // Arrange
        var spec = new TransformationSpec();

        // Act & Assert
        spec.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_WithDimensions_ShouldReturnFalse()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100,
            Fit = ImageFit.Contain,
            Format = ImageFormat.Original,
            Quality = 85
        };

        // Act & Assert
        spec.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void Parse_WithBlur_ShouldSetBlurProperties()
    {
        // Arrange & Act
        var parsed = TransformationSpec.Parse("blur=15,gray=1");

        // Assert
        parsed.Blur.Should().BeTrue();
        parsed.BlurRadius.Should().Be(15);
        parsed.Grayscale.Should().BeTrue();
    }

    [Fact]
    public void ToCanonicalString_WithBlur_ShouldIncludeBlur()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100,
            Blur = true,
            BlurRadius = 20
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("blur=20");
    }

    [Fact]
    public void ToCanonicalString_WithGrayscale_ShouldIncludeGray()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100,
            Grayscale = true
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().Contain("gray=1");
    }

    [Fact]
    public void ToCanonicalString_Empty_ShouldReturnEmptyString()
    {
        // Arrange
        var spec = new TransformationSpec();

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().BeEmpty();
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
        var spec = new TransformationSpec
        {
            Width = 100,
            Height = 100,
            Fit = fit,
            Format = ImageFormat.Original,
            Quality = 85
        };

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
        var spec = new TransformationSpec
        {
            Width = 100,
            Height = 100,
            Fit = ImageFit.Contain,
            Format = format,
            Quality = 85
        };

        // Act
        var result = spec.ToCanonicalString();

        // Assert
        result.Should().NotBeNullOrEmpty();
    }
}
