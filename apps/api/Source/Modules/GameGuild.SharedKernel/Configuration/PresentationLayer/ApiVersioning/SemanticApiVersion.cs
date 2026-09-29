using System.Globalization;
using Asp.Versioning;

namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     An API version with a semantic patch component.
/// </summary>
public sealed class SemanticApiVersion : ApiVersion
{
    public SemanticApiVersion(int major, int minor, int patch, string? prerelease = null, string? metadata = null)
        : base(major, minor, GetNativeStatus(patch, prerelease))
    {
        if (major < 0) throw new ArgumentOutOfRangeException(nameof(major));
        if (minor < 0) throw new ArgumentOutOfRangeException(nameof(minor));
        if (patch < 0) throw new ArgumentOutOfRangeException(nameof(patch));

        Patch = patch;
        Prerelease = prerelease;
        Metadata = metadata;
    }

    /// <summary>The semantic patch component.</summary>
    public int Patch { get; }

    /// <summary>The optional semantic prerelease label.</summary>
    public string? Prerelease { get; }

    /// <summary>The optional semantic build metadata, which does not affect version precedence.</summary>
    public string? Metadata { get; }

    public override int CompareTo(ApiVersion? other)
    {
        if (other is null) return 1;

        if (other.GroupVersion is not null) return base.CompareTo(other);

        var semanticOther = other as SemanticApiVersion ?? new SemanticApiVersion(
            other.MajorVersion ?? 0,
            other.MinorVersion ?? 0,
            0,
            other.Status);

        var majorComparison = MajorVersion!.Value.CompareTo(semanticOther.MajorVersion!.Value);
        if (majorComparison != 0) return majorComparison;

        var minorComparison = MinorVersion!.Value.CompareTo(semanticOther.MinorVersion!.Value);
        if (minorComparison != 0) return minorComparison;

        var patchComparison = Patch.CompareTo(semanticOther.Patch);
        if (patchComparison != 0) return patchComparison;

        return ComparePrerelease(Prerelease, semanticOther.Prerelease);
    }

    public override bool Equals(ApiVersion? other)
    {
        if (other is null || other.GroupVersion is not null) return false;

        var otherMajor = other.MajorVersion ?? 0;
        var otherMinor = other.MinorVersion ?? 0;
        var otherPatch = other is SemanticApiVersion semantic ? semantic.Patch : 0;
        var otherPrerelease = other is SemanticApiVersion semanticVersion ? semanticVersion.Prerelease : other.Status;

        return MajorVersion == otherMajor && MinorVersion == otherMinor && Patch == otherPatch &&
               string.Equals(Prerelease, otherPrerelease, StringComparison.Ordinal);
    }

    public override bool Equals(object? obj) => obj is ApiVersion other && Equals(other);

    public override int GetHashCode()
    {
        // Patch zero and its prerelease form deliberately retain the native ApiVersion hash,
        // so they remain equal to existing major/minor declarations.
        return Patch == 0 ? base.GetHashCode() : HashCode.Combine(base.GetHashCode(), Patch);
    }

    public override string ToString() => ToSemanticString();

    public override string ToString(string? format) => ToSemanticString(format);

    public override string ToString(string? format, IFormatProvider? formatProvider) => ToSemanticString(format);

    private static string? GetNativeStatus(int patch, string? prerelease)
    {
        // ApiVersion has no patch member. Preserve patch identity in its status so equality with
        // a native 1.2 API version cannot accidentally match semantic version 1.2.3.
        if (patch == 0) return prerelease;

        var patchStatus = "semver.p" + patch.ToString(CultureInfo.InvariantCulture);
        return prerelease is null ? patchStatus : patchStatus + "." + prerelease;
    }

    private string ToSemanticString(string? format = null)
    {
        var version = string.Create(CultureInfo.InvariantCulture, $"{MajorVersion}.{MinorVersion}.{Patch}");
        var semanticVersion = Prerelease is null ? version : version + "-" + Prerelease;
        if (Metadata is not null) semanticVersion += "+" + Metadata;

        if (string.IsNullOrEmpty(format)) return semanticVersion;

        var tokenIndex = format.IndexOf('V');
        if (tokenIndex < 0) return semanticVersion;

        var tokenEnd = tokenIndex;
        while (tokenEnd < format.Length && format[tokenEnd] == 'V') tokenEnd++;

        var prefix = format[..tokenIndex].Replace("'", string.Empty, StringComparison.Ordinal);
        var suffix = format[tokenEnd..].Replace("'", string.Empty, StringComparison.Ordinal);
        return prefix + semanticVersion + suffix;
    }

    private static int ComparePrerelease(string? left, string? right)
    {
        if (left is null) return right is null ? 0 : 1;
        if (right is null) return -1;

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        var count = Math.Min(leftParts.Length, rightParts.Length);

        for (var index = 0; index < count; index++)
        {
            var leftPart = leftParts[index];
            var rightPart = rightParts[index];
            var leftNumeric = IsNumericIdentifier(leftPart);
            var rightNumeric = IsNumericIdentifier(rightPart);

            int comparison;
            if (leftNumeric && rightNumeric)
                comparison = CompareNumericIdentifiers(leftPart, rightPart);
            else if (leftNumeric != rightNumeric)
                comparison = leftNumeric ? -1 : 1;
            else
                comparison = string.Compare(leftPart, rightPart, StringComparison.Ordinal);

            if (comparison != 0) return comparison;
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    private static bool IsNumericIdentifier(string value) => value.Length > 0 && value.All(char.IsAsciiDigit);

    private static int CompareNumericIdentifiers(string left, string right)
    {
        var lengthComparison = left.Length.CompareTo(right.Length);
        return lengthComparison != 0 ? lengthComparison : string.Compare(left, right, StringComparison.Ordinal);
    }
}
