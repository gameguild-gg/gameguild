using Microsoft.Extensions.Options;
using GameGuild.Assets.Transformation;

namespace GameGuild.Assets.UnitTests.Transformation;

public class TransformationValidatorTests
{
    private readonly TransformationOptions _options;
    private readonly TransformationValidator _validator;

    public TransformationValidatorTests()
    {
        _options = new TransformationOptions
        {
            MaxDimension = 4096,
            MinDimension = 16,
            MinQuality = 10,
            MaxQuality = 100,
            MaxBlurRadius = 100
        };

        _validator = new TransformationValidator(Options.Create(_options));
    }

    #region Validate Tests - Valid Cases

    [Fact]
    public void Validate_EmptySpec_ReturnsValid()
    {
        // Arrange
        var spec = new TransformationSpec();

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Validate_ValidResize_ReturnsValid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 800,
            Height = 600
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidFormat_ReturnsValid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Format = ImageFormat.Webp
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ValidQuality_ReturnsValid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Quality = 80
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Validate Tests - Invalid Resize

    [Fact]
    public void Validate_ResizeOnAudio_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Audio);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not allowed");
    }

    [Fact]
    public void Validate_WidthBelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 10  // Below minimum of 16
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Width must be at least");
    }

    [Fact]
    public void Validate_WidthAboveMaximum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 10000  // Above maximum of 4096
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    [Fact]
    public void Validate_HeightBelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Height = 5  // Below minimum of 16
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Height must be at least");
    }

    [Fact]
    public void Validate_HeightAboveMaximum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Height = 5000  // Above maximum of 4096
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    #endregion

    #region Validate Tests - Invalid Format

    [Fact]
    public void Validate_FormatConversionOnVideo_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Format = ImageFormat.Webp
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Video);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not allowed");
    }

    [Fact]
    public void Validate_OriginalFormat_IsAlwaysValid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Format = ImageFormat.Original
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Video);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Validate Tests - Invalid Quality

    [Fact]
    public void Validate_QualityBelowMinimum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Quality = 5  // Below minimum of 10
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Quality must be at least");
    }

    [Fact]
    public void Validate_QualityAboveMaximum_ReturnsInvalid()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Quality = 150  // Above maximum of 100
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Image);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    #endregion

    #region GetAllowedTransformations Tests

    [Fact]
    public void GetAllowedTransformations_Image_AllowsResize()
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(AssetKind.Image);

        // Assert
        allowed.AllowResize.Should().BeTrue();
    }

    [Fact]
    public void GetAllowedTransformations_Image_AllowsFormatConversion()
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(AssetKind.Image);

        // Assert
        allowed.AllowFormatConversion.Should().BeTrue();
    }

    [Fact]
    public void GetAllowedTransformations_Audio_DisallowsResize()
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(AssetKind.Audio);

        // Assert
        allowed.AllowResize.Should().BeFalse();
    }

    [Fact]
    public void GetAllowedTransformations_Video_DisallowsFormatConversion()
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(AssetKind.Video);

        // Assert
        allowed.AllowFormatConversion.Should().BeFalse();
    }

    [Fact]
    public void GetAllowedTransformations_Document_ReturnsLimits()
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(AssetKind.Document);

        // Assert
        allowed.AllowResize.Should().BeTrue();
        allowed.MaxDimension.Should().Be(2048);
    }

    [Theory]
    [InlineData(AssetKind.Image)]
    [InlineData(AssetKind.Video)]
    [InlineData(AssetKind.Audio)]
    [InlineData(AssetKind.Document)]
    public void GetAllowedTransformations_ReturnsNonNull(AssetKind kind)
    {
        // Act
        var allowed = _validator.GetAllowedTransformations(kind);

        // Assert
        allowed.Should().NotBeNull();
    }

    #endregion

    #region Asset Kind Specific Tests

    [Fact]
    public void Validate_VideoResize_LimitedTo1920()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 2000  // Above 1920 limit for video
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Video);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    [Fact]
    public void Validate_Archive_NoTransformationsAllowed()
    {
        // Arrange
        var spec = new TransformationSpec
        {
            Width = 100
        };

        // Act
        var result = _validator.Validate(spec, AssetKind.Archive);

        // Assert
        result.IsValid.Should().BeFalse();
    }

    #endregion
}
