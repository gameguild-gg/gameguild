using Microsoft.Extensions.Options;

namespace GameGuild.Assets.Transformation;

/// <summary>
/// Configuration for transformation limits.
/// Mitigates: Transformation Downgrade Attack (Threat #5)
/// </summary>
public class TransformationOptions
{
    public const string SectionName = "Assets:Transformation";

    /// <summary>
    /// Maximum output dimension (width or height) in pixels.
    /// </summary>
    public int MaxDimension { get; set; } = 4096;

    /// <summary>
    /// Minimum output dimension to prevent tiny image attacks.
    /// </summary>
    public int MinDimension { get; set; } = 16;

    /// <summary>
    /// Minimum quality for lossy formats (prevents ultra-low quality abuse).
    /// </summary>
    public int MinQuality { get; set; } = 10;

    /// <summary>
    /// Maximum quality (100 is lossless for most formats).
    /// </summary>
    public int MaxQuality { get; set; } = 100;

    /// <summary>
    /// Allowed transformations per asset kind.
    /// </summary>
    public Dictionary<string, AllowedTransformations> KindLimits { get; set; } = new()
    {
        ["Image"] = new AllowedTransformations
        {
            AllowResize = true,
            AllowFormatConversion = true,
            AllowEffects = true,
            MaxDimension = 4096,
            AllowedFormats = ["Original", "Jpeg", "Png", "Webp", "Avif"]
        },
        ["Video"] = new AllowedTransformations
        {
            AllowResize = true,
            AllowFormatConversion = false, // Video transcoding is expensive
            AllowEffects = false,
            MaxDimension = 1920 // Max 1080p thumbnails
        },
        ["Document"] = new AllowedTransformations
        {
            AllowResize = true, // For PDF thumbnails
            AllowFormatConversion = true,
            AllowEffects = false,
            MaxDimension = 2048,
            AllowedFormats = ["Original", "Png", "Jpeg"]
        },
        ["Audio"] = new AllowedTransformations
        {
            AllowResize = false,
            AllowFormatConversion = false,
            AllowEffects = false
        },
        ["Archive"] = new AllowedTransformations
        {
            AllowResize = false,
            AllowFormatConversion = false,
            AllowEffects = false
        },
        ["Other"] = new AllowedTransformations
        {
            AllowResize = false,
            AllowFormatConversion = false,
            AllowEffects = false
        }
    };

    /// <summary>
    /// Maximum blur radius to prevent resource exhaustion.
    /// </summary>
    public int MaxBlurRadius { get; set; } = 100;

    /// <summary>
    /// Rate limit for transformation requests per asset per hour.
    /// </summary>
    public int MaxTransformationsPerAssetPerHour { get; set; } = 100;
}

/// <summary>
/// Allowed transformations for a specific asset kind.
/// </summary>
public class AllowedTransformations
{
    public bool AllowResize { get; set; }
    public bool AllowFormatConversion { get; set; }
    public bool AllowEffects { get; set; }
    public int MaxDimension { get; set; } = 4096;
    public string[] AllowedFormats { get; set; } = ["Original"];
}

/// <summary>
/// Result of transformation validation.
/// </summary>
public sealed record TransformationValidationResult(
    bool IsValid,
    string? Error = null,
    TransformationSpec? SanitizedSpec = null);

/// <summary>
/// Service for validating and sanitizing transformation requests.
/// </summary>
public interface ITransformationValidator
{
    /// <summary>
    /// Validates a transformation spec against the limits for the given asset kind.
    /// Returns a sanitized spec if valid, or an error if not.
    /// </summary>
    TransformationValidationResult Validate(
        TransformationSpec spec,
        AssetKind kind);

    /// <summary>
    /// Gets allowed transformations for an asset kind.
    /// </summary>
    AllowedTransformations GetAllowedTransformations(AssetKind kind);
}

/// <summary>
/// Implementation of transformation validation.
/// </summary>
public sealed class TransformationValidator : ITransformationValidator
{
    private readonly TransformationOptions _options;

    public TransformationValidator(IOptions<TransformationOptions> options)
    {
        _options = options.Value;
    }

    public TransformationValidationResult Validate(
        TransformationSpec spec,
        AssetKind kind)
    {
        var allowed = GetAllowedTransformations(kind);

        // Check if resize is allowed
        if ((spec.Width.HasValue || spec.Height.HasValue) && !allowed.AllowResize)
        {
            return new TransformationValidationResult(
                false, $"Resize not allowed for {kind} assets");
        }

        // Check dimension limits
        if (spec.Width.HasValue)
        {
            if (spec.Width < _options.MinDimension)
            {
                return new TransformationValidationResult(
                    false, $"Width must be at least {_options.MinDimension}px");
            }
            if (spec.Width > allowed.MaxDimension || spec.Width > _options.MaxDimension)
            {
                return new TransformationValidationResult(
                    false, $"Width exceeds maximum of {Math.Min(allowed.MaxDimension, _options.MaxDimension)}px");
            }
        }

        if (spec.Height.HasValue)
        {
            if (spec.Height < _options.MinDimension)
            {
                return new TransformationValidationResult(
                    false, $"Height must be at least {_options.MinDimension}px");
            }
            if (spec.Height > allowed.MaxDimension || spec.Height > _options.MaxDimension)
            {
                return new TransformationValidationResult(
                    false, $"Height exceeds maximum of {Math.Min(allowed.MaxDimension, _options.MaxDimension)}px");
            }
        }

        // Check format conversion
        if (spec.Format.HasValue && spec.Format != ImageFormat.Original)
        {
            if (!allowed.AllowFormatConversion)
            {
                return new TransformationValidationResult(
                    false, $"Format conversion not allowed for {kind} assets");
            }

            var formatName = spec.Format.Value.ToString();
            if (!allowed.AllowedFormats.Contains(formatName))
            {
                return new TransformationValidationResult(
                    false, $"Format {formatName} not allowed for {kind} assets");
            }
        }

        // Check effects
        if ((spec.Blur == true || spec.Grayscale == true) && !allowed.AllowEffects)
        {
            return new TransformationValidationResult(
                false, $"Effects not allowed for {kind} assets");
        }

        // Check blur radius
        if (spec.BlurRadius.HasValue && spec.BlurRadius > _options.MaxBlurRadius)
        {
            return new TransformationValidationResult(
                false, $"Blur radius exceeds maximum of {_options.MaxBlurRadius}");
        }

        // Check quality
        if (spec.Quality.HasValue)
        {
            if (spec.Quality < _options.MinQuality)
            {
                return new TransformationValidationResult(
                    false, $"Quality must be at least {_options.MinQuality}");
            }
            if (spec.Quality > _options.MaxQuality)
            {
                return new TransformationValidationResult(
                    false, $"Quality exceeds maximum of {_options.MaxQuality}");
            }
        }

        // Return sanitized spec (clamped values)
        var sanitized = spec with
        {
            Width = spec.Width.HasValue 
                ? Math.Clamp(spec.Width.Value, _options.MinDimension, Math.Min(allowed.MaxDimension, _options.MaxDimension))
                : null,
            Height = spec.Height.HasValue
                ? Math.Clamp(spec.Height.Value, _options.MinDimension, Math.Min(allowed.MaxDimension, _options.MaxDimension))
                : null,
            Quality = spec.Quality.HasValue
                ? Math.Clamp(spec.Quality.Value, _options.MinQuality, _options.MaxQuality)
                : null,
            BlurRadius = spec.BlurRadius.HasValue
                ? Math.Min(spec.BlurRadius.Value, _options.MaxBlurRadius)
                : null
        };

        return new TransformationValidationResult(true, null, sanitized);
    }

    public AllowedTransformations GetAllowedTransformations(AssetKind kind)
    {
        var kindName = kind.ToString();
        if (_options.KindLimits.TryGetValue(kindName, out var limits))
        {
            return limits;
        }

        // Default: no transformations allowed
        return new AllowedTransformations();
    }
}

/// <summary>
/// Backward compatibility alias for TransformationOptions.
/// </summary>
public class TransformationLimitsOptions : TransformationOptions
{
    public new const string SectionName = "Assets:TransformationLimits";
}
