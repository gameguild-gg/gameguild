namespace GameGuild.Configuration.PresentationLayer.Authentication;

/// <summary>
///     Configuration for the optional HTTP Basic authentication scheme.
/// </summary>
public sealed class BasicAuthenticationSettings
{
    public static string DefaultSchemeName { get; } = "Basic";

    public string SchemeName { get; set; } = DefaultSchemeName;

    public string Realm { get; set; } = "GameGuild API";

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SchemeName) || SchemeName.Any(char.IsControl))
        {
            throw new InvalidOperationException("Basic authentication scheme name must be non-empty and contain no control characters.");
        }

        if (string.IsNullOrWhiteSpace(Realm) ||
            Realm.Any(character => char.IsControl(character) || character is '"' or '\\'))
        {
            throw new InvalidOperationException("Basic authentication realm must be non-empty and must not contain control characters, quotes, or backslashes.");
        }
    }
}
