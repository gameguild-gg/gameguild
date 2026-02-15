using GameGuild.Assets.Transformation;
using Microsoft.Extensions.Options;

namespace GameGuild.Assets.UnitTests;

public class TransformationValidatorTests
{
    private readonly TransformationValidator _validator;
    private readonly TransformationOptions _options;

    public TransformationValidatorTests()
    {
        _options = new TransformationOptions();
        _validator = new TransformationValidator(Options.Create(_options));
    }

    #region Validate — Valid specs

    [Fact]
    public void Validate_IdentitySpec_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec();
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec.Should().NotBeNull();
    }

    [Fact]
    public void Validate_ResizeWidth_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Width = 800 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.Width.Should().Be(800);
    }

    [Fact]
    public void Validate_ResizeHeightAndWidth_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Width = 800, Height = 600 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.Width.Should().Be(800);
        result.SanitizedSpec.Height.Should().Be(600);
    }

    [Fact]
    public void Validate_FormatConversion_Jpeg_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Jpeg };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_FormatConversion_Webp_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Webp };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_BlurEffect_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Blur = true, BlurRadius = 10 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Grayscale_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Grayscale = true };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_Quality_ForImage_ShouldBeValid()
    {
        var spec = new TransformationSpec { Quality = 80 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.Quality.Should().Be(80);
    }

    #endregion

    #region Validate — Resize not allowed

    [Fact]
    public void Validate_Resize_ForAudio_ShouldFail()
    {
        var spec = new TransformationSpec { Width = 100 };
        var result = _validator.Validate(spec, AssetKind.Audio);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Resize not allowed");
    }

    [Fact]
    public void Validate_Resize_ForArchive_ShouldFail()
    {
        var spec = new TransformationSpec { Height = 100 };
        var result = _validator.Validate(spec, AssetKind.Archive);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Resize not allowed");
    }

    [Fact]
    public void Validate_Resize_ForOther_ShouldFail()
    {
        var spec = new TransformationSpec { Width = 100 };
        var result = _validator.Validate(spec, AssetKind.Other);
        result.IsValid.Should().BeFalse();
    }

    #endregion

    #region Validate — Dimension limits

    [Fact]
    public void Validate_WidthBelowMin_ShouldFail()
    {
        var spec = new TransformationSpec { Width = 5 }; // MinDimension=16
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("at least 16");
    }

    [Fact]
    public void Validate_HeightBelowMin_ShouldFail()
    {
        var spec = new TransformationSpec { Height = 10 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("at least 16");
    }

    [Fact]
    public void Validate_WidthAboveMax_ShouldFail()
    {
        var spec = new TransformationSpec { Width = 10000 }; // MaxDimension=4096
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    [Fact]
    public void Validate_HeightAboveMax_ShouldFail()
    {
        var spec = new TransformationSpec { Height = 10000 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    [Fact]
    public void Validate_Width_AboveKindSpecificMax_ShouldFail()
    {
        // Video MaxDimension is 1920
        var spec = new TransformationSpec { Width = 2000 };
        var result = _validator.Validate(spec, AssetKind.Video);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    #endregion

    #region Validate — Format conversion

    [Fact]
    public void Validate_FormatConversion_ForVideo_ShouldFail()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Jpeg };
        var result = _validator.Validate(spec, AssetKind.Video);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Format conversion not allowed");
    }

    [Fact]
    public void Validate_OriginalFormat_ForVideo_ShouldBeValid()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Original };
        var result = _validator.Validate(spec, AssetKind.Video);
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_DisallowedFormat_ForDocument_ShouldFail()
    {
        // Document allows Original, Png, Jpeg — not Webp
        var spec = new TransformationSpec { Format = ImageFormat.Webp };
        var result = _validator.Validate(spec, AssetKind.Document);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("not allowed");
    }

    [Fact]
    public void Validate_AllowedFormat_ForDocument_ShouldBeValid()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Png };
        var result = _validator.Validate(spec, AssetKind.Document);
        result.IsValid.Should().BeTrue();
    }

    #endregion

    #region Validate — Effects

    [Fact]
    public void Validate_Blur_ForVideo_ShouldFail()
    {
        var spec = new TransformationSpec { Blur = true };
        var result = _validator.Validate(spec, AssetKind.Video);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Effects not allowed");
    }

    [Fact]
    public void Validate_Grayscale_ForDocument_ShouldFail()
    {
        var spec = new TransformationSpec { Grayscale = true };
        var result = _validator.Validate(spec, AssetKind.Document);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Effects not allowed");
    }

    [Fact]
    public void Validate_BlurRadiusAboveMax_ShouldFail()
    {
        var spec = new TransformationSpec { Blur = true, BlurRadius = 200 }; // Max=100
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("Blur radius exceeds maximum");
    }

    #endregion

    #region Validate — Quality

    [Fact]
    public void Validate_QualityBelowMin_ShouldFail()
    {
        var spec = new TransformationSpec { Quality = 5 }; // Min=10
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("at least 10");
    }

    [Fact]
    public void Validate_QualityAboveMax_ShouldFail()
    {
        var spec = new TransformationSpec { Quality = 150 }; // Max=100
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeFalse();
        result.Error.Should().Contain("exceeds maximum");
    }

    #endregion

    #region Validate — Sanitized spec clamping

    [Fact]
    public void Validate_SanitizedSpec_ClampsWidth()
    {
        var spec = new TransformationSpec { Width = 4000 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.Width.Should().Be(4000);
    }

    [Fact]
    public void Validate_SanitizedSpec_ClampsBlurRadius()
    {
        var spec = new TransformationSpec { Blur = true, BlurRadius = 50 };
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.BlurRadius.Should().Be(50);
    }

    [Fact]
    public void Validate_SanitizedSpec_NullDimensions_StayNull()
    {
        var spec = new TransformationSpec();
        var result = _validator.Validate(spec, AssetKind.Image);
        result.IsValid.Should().BeTrue();
        result.SanitizedSpec!.Width.Should().BeNull();
        result.SanitizedSpec.Height.Should().BeNull();
        result.SanitizedSpec.Quality.Should().BeNull();
        result.SanitizedSpec.BlurRadius.Should().BeNull();
    }

    #endregion

    #region GetAllowedTransformations

    [Fact]
    public void GetAllowedTransformations_Image_ShouldAllowAll()
    {
        var allowed = _validator.GetAllowedTransformations(AssetKind.Image);
        allowed.AllowResize.Should().BeTrue();
        allowed.AllowFormatConversion.Should().BeTrue();
        allowed.AllowEffects.Should().BeTrue();
        allowed.MaxDimension.Should().Be(4096);
    }

    [Fact]
    public void GetAllowedTransformations_Video_ShouldAllowResizeOnly()
    {
        var allowed = _validator.GetAllowedTransformations(AssetKind.Video);
        allowed.AllowResize.Should().BeTrue();
        allowed.AllowFormatConversion.Should().BeFalse();
        allowed.AllowEffects.Should().BeFalse();
        allowed.MaxDimension.Should().Be(1920);
    }

    [Fact]
    public void GetAllowedTransformations_Audio_ShouldAllowNothing()
    {
        var allowed = _validator.GetAllowedTransformations(AssetKind.Audio);
        allowed.AllowResize.Should().BeFalse();
        allowed.AllowFormatConversion.Should().BeFalse();
        allowed.AllowEffects.Should().BeFalse();
    }

    [Fact]
    public void GetAllowedTransformations_UnknownKind_ShouldReturnDefaults()
    {
        // Use a kind not in the dictionary fallback
        var allowed = _validator.GetAllowedTransformations((AssetKind)999);
        allowed.AllowResize.Should().BeFalse();
        allowed.AllowFormatConversion.Should().BeFalse();
        allowed.AllowEffects.Should().BeFalse();
    }

    #endregion

    #region TransformationOptions defaults

    [Fact]
    public void TransformationOptions_DefaultValues()
    {
        var opts = new TransformationOptions();
        opts.MaxDimension.Should().Be(4096);
        opts.MinDimension.Should().Be(16);
        opts.MinQuality.Should().Be(10);
        opts.MaxQuality.Should().Be(100);
        opts.MaxBlurRadius.Should().Be(100);
        opts.MaxTransformationsPerAssetPerHour.Should().Be(100);
        opts.KindLimits.Should().ContainKey("Image");
        opts.KindLimits.Should().ContainKey("Video");
        opts.KindLimits.Should().ContainKey("Document");
        opts.KindLimits.Should().ContainKey("Audio");
        opts.KindLimits.Should().ContainKey("Archive");
        opts.KindLimits.Should().ContainKey("Other");
    }

    [Fact]
    public void TransformationOptions_SectionName()
    {
        TransformationOptions.SectionName.Should().Be("Assets:Transformation");
    }

    #endregion

    #region AllowedTransformations defaults

    [Fact]
    public void AllowedTransformations_DefaultValues()
    {
        var at = new AllowedTransformations();
        at.AllowResize.Should().BeFalse();
        at.AllowFormatConversion.Should().BeFalse();
        at.AllowEffects.Should().BeFalse();
        at.MaxDimension.Should().Be(4096);
        at.AllowedFormats.Should().Equal("Original");
    }

    #endregion

    #region TransformationValidationResult record

    [Fact]
    public void TransformationValidationResult_ValidWithSanitizedSpec()
    {
        var spec = new TransformationSpec { Width = 100 };
        var result = new TransformationValidationResult(true, null, spec);
        result.IsValid.Should().BeTrue();
        result.Error.Should().BeNull();
        result.SanitizedSpec.Should().Be(spec);
    }

    [Fact]
    public void TransformationValidationResult_InvalidWithError()
    {
        var result = new TransformationValidationResult(false, "Some error");
        result.IsValid.Should().BeFalse();
        result.Error.Should().Be("Some error");
        result.SanitizedSpec.Should().BeNull();
    }

    #endregion

    #region TransformationLimitsOptions backward compat

    [Fact]
    public void TransformationLimitsOptions_SectionName()
    {
        TransformationLimitsOptions.SectionName.Should().Be("Assets:TransformationLimits");
    }

    [Fact]
    public void TransformationLimitsOptions_InheritsTransformationOptions()
    {
        var opts = new TransformationLimitsOptions();
        opts.MaxDimension.Should().Be(4096);
        opts.MinDimension.Should().Be(16);
    }

    #endregion
}

#region Additional TransformationSpec Tests

public class TransformationSpecAdditionalTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsIdentity()
    {
        var spec = TransformationSpec.Parse("");
        spec.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Parse_NullString_ReturnsIdentity()
    {
        var spec = TransformationSpec.Parse(null!);
        spec.IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void Parse_WidthOnly()
    {
        var spec = TransformationSpec.Parse("w=200");
        spec.Width.Should().Be(200);
        spec.Height.Should().BeNull();
    }

    [Fact]
    public void Parse_HeightOnly()
    {
        var spec = TransformationSpec.Parse("h=300");
        spec.Height.Should().Be(300);
    }

    [Fact]
    public void Parse_WidthAndHeight()
    {
        var spec = TransformationSpec.Parse("w=100,h=200");
        spec.Width.Should().Be(100);
        spec.Height.Should().Be(200);
    }

    [Fact]
    public void Parse_Quality()
    {
        var spec = TransformationSpec.Parse("q=85");
        spec.Quality.Should().Be(85);
    }

    [Fact]
    public void Parse_Quality_ClampsTo100()
    {
        var spec = TransformationSpec.Parse("q=150");
        spec.Quality.Should().Be(100);
    }

    [Fact]
    public void Parse_Quality_ClampsTo1()
    {
        var spec = TransformationSpec.Parse("q=0");
        spec.Quality.Should().Be(1);
    }

    [Fact]
    public void Parse_Format()
    {
        var spec = TransformationSpec.Parse("f=webp");
        spec.Format.Should().Be(ImageFormat.Webp);
    }

    [Fact]
    public void Parse_Fit()
    {
        var spec = TransformationSpec.Parse("fit=cover");
        spec.Fit.Should().Be(ImageFit.Cover);
    }

    [Fact]
    public void Parse_Blur()
    {
        var spec = TransformationSpec.Parse("blur=15");
        spec.Blur.Should().BeTrue();
        spec.BlurRadius.Should().Be(15);
    }

    [Fact]
    public void Parse_Grayscale()
    {
        var spec = TransformationSpec.Parse("gray=1");
        spec.Grayscale.Should().BeTrue();
    }

    [Fact]
    public void Parse_GrayscaleTrue()
    {
        var spec = TransformationSpec.Parse("gray=true");
        spec.Grayscale.Should().BeTrue();
    }

    [Fact]
    public void Parse_GrayscaleFalse()
    {
        var spec = TransformationSpec.Parse("gray=0");
        spec.Grayscale.Should().BeFalse();
    }

    [Fact]
    public void Parse_InvalidKeyValuePair_ShouldBeIgnored()
    {
        var spec = TransformationSpec.Parse("invalid,w=100");
        spec.Width.Should().Be(100);
    }

    [Fact]
    public void Parse_ComplexSpec()
    {
        var spec = TransformationSpec.Parse("w=800,h=600,q=90,f=jpeg,blur=5,gray=1,fit=cover");
        spec.Width.Should().Be(800);
        spec.Height.Should().Be(600);
        spec.Quality.Should().Be(90);
        spec.Format.Should().Be(ImageFormat.Jpeg);
        spec.Blur.Should().BeTrue();
        spec.BlurRadius.Should().Be(5);
        spec.Grayscale.Should().BeTrue();
        spec.Fit.Should().Be(ImageFit.Cover);
    }

    [Fact]
    public void ToCanonicalString_EmptySpec_ReturnsEmpty()
    {
        var spec = new TransformationSpec();
        spec.ToCanonicalString().Should().BeEmpty();
    }

    [Fact]
    public void ToCanonicalString_WithWidth()
    {
        var spec = new TransformationSpec { Width = 200 };
        spec.ToCanonicalString().Should().Be("w=200");
    }

    [Fact]
    public void ToCanonicalString_MultipleFields_SortedAlphabetically()
    {
        var spec = new TransformationSpec { Width = 200, Height = 100, Quality = 80 };
        var canonical = spec.ToCanonicalString();
        canonical.Should().Contain("h=100");
        canonical.Should().Contain("q=80");
        canonical.Should().Contain("w=200");
    }

    [Fact]
    public void ToCanonicalString_Blur_DefaultRadius()
    {
        var spec = new TransformationSpec { Blur = true };
        spec.ToCanonicalString().Should().Contain("blur=10"); // default BlurRadius
    }

    [Fact]
    public void ToCanonicalString_Blur_CustomRadius()
    {
        var spec = new TransformationSpec { Blur = true, BlurRadius = 20 };
        spec.ToCanonicalString().Should().Contain("blur=20");
    }

    [Fact]
    public void ToCanonicalString_Grayscale()
    {
        var spec = new TransformationSpec { Grayscale = true };
        spec.ToCanonicalString().Should().Be("gray=1");
    }

    [Fact]
    public void ToCanonicalString_Format()
    {
        var spec = new TransformationSpec { Format = ImageFormat.Webp };
        spec.ToCanonicalString().Should().Be("f=webp");
    }

    [Fact]
    public void IsIdentity_AllNull_ReturnsTrue()
    {
        new TransformationSpec().IsIdentity.Should().BeTrue();
    }

    [Fact]
    public void IsIdentity_WithWidth_ReturnsFalse()
    {
        new TransformationSpec { Width = 100 }.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void IsIdentity_WithBlurTrue_ReturnsFalse()
    {
        new TransformationSpec { Blur = true }.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void IsIdentity_WithGrayscaleTrue_ReturnsFalse()
    {
        new TransformationSpec { Grayscale = true }.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_BothWithinLimits_ReturnsTrue()
    {
        var spec = new TransformationSpec { Width = 100, Height = 200 };
        spec.IsWithinLimits(4096).Should().BeTrue();
    }

    [Fact]
    public void IsWithinLimits_WidthExceedsLimit_ReturnsFalse()
    {
        var spec = new TransformationSpec { Width = 5000 };
        spec.IsWithinLimits(4096).Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_HeightExceedsLimit_ReturnsFalse()
    {
        var spec = new TransformationSpec { Height = 5000 };
        spec.IsWithinLimits(4096).Should().BeFalse();
    }

    [Fact]
    public void IsWithinLimits_NoDimensions_ReturnsTrue()
    {
        var spec = new TransformationSpec();
        spec.IsWithinLimits(4096).Should().BeTrue();
    }
}

#endregion
