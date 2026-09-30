namespace GameGuild.Configuration.PresentationLayer.ApiVersioning;

/// <summary>
///     Selects how numeric API versions are parsed.
/// </summary>
public enum ApiVersionFormatKind
{
    /// <summary>Use the ASP.NET API versioning format (major/minor with optional status).</summary>
    Native = 0,

    /// <summary>Accept semantic major.minor.patch versions, including optional prerelease identifiers.</summary>
    SemanticVersion = 1
}
