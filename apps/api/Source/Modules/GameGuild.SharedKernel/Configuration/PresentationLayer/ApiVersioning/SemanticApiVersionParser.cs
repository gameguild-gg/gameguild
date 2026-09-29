using System.Globalization;
using System.Text.RegularExpressions;
using Asp.Versioning;

namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Parses semantic major.minor.patch versions and delegates date-based versions to the native parser.
/// </summary>
public sealed class SemanticApiVersionParser : ApiVersionParser
{
    private static readonly Regex SemanticVersionPattern = new(
        @"\A(?<major>0|[1-9][0-9]*)\.(?<minor>0|[1-9][0-9]*)\.(?<patch>0|[1-9][0-9]*)(?:-(?<prerelease>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+(?<metadata>[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?\z",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    /// <summary>The stateless parser used by version attributes and DI registration.</summary>
    public static SemanticApiVersionParser Instance { get; } = new();

    public override ApiVersion Parse(ReadOnlySpan<char> text)
    {
        if (TryParseSemanticVersion(text, out var semanticVersion))
        {
            return semanticVersion;
        }

        return base.Parse(text);
    }

    public override bool TryParse(ReadOnlySpan<char> text, out ApiVersion apiVersion)
    {
        if (TryParseSemanticVersion(text, out var semanticVersion))
        {
            apiVersion = semanticVersion;
            return true;
        }

        var parsed = base.TryParse(text, out var nativeVersion);
        apiVersion = nativeVersion!;
        return parsed;
    }

    private static bool TryParseSemanticVersion(ReadOnlySpan<char> text, out SemanticApiVersion apiVersion)
    {
        var match = SemanticVersionPattern.Match(text.ToString());
        if (!match.Success ||
            !int.TryParse(match.Groups["major"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var major) ||
            !int.TryParse(match.Groups["minor"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var minor) ||
            !int.TryParse(match.Groups["patch"].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var patch))
        {
            apiVersion = null!;
            return false;
        }

        var prerelease = match.Groups["prerelease"].Success ? match.Groups["prerelease"].Value : null;
        var metadata = match.Groups["metadata"].Success ? match.Groups["metadata"].Value : null;
        if (!HasValidPrereleaseIdentifiers(prerelease))
        {
            apiVersion = null!;
            return false;
        }

        apiVersion = new SemanticApiVersion(major, minor, patch, prerelease, metadata);
        return true;
    }

    private static bool HasValidPrereleaseIdentifiers(string? prerelease)
    {
        if (prerelease is null)
        {
            return true;
        }

        foreach (var identifier in prerelease.Split('.'))
        {
            var isNumeric = identifier.All(char.IsAsciiDigit);
            if (isNumeric && identifier.Length > 1 && identifier[0] == '0')
            {
                return false;
            }
        }

        return true;
    }
}
