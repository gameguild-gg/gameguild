using System.Globalization;

namespace GameGuild.API.Core.OpenApi;

/// <summary>
///     Configures translated variants of the API's Swagger documents.
/// </summary>
public sealed class OpenApiLocalizationOptions
{
    /// <summary>
    ///     Gets the translations keyed by a supported culture name such as <c>pt-BR</c>.
    /// </summary>
    public Dictionary<string, OpenApiLocalizedDocumentOptions> Locales { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Validates the locale names and translation values before Swagger documents are registered.
    /// </summary>
    public void Validate()
    {
        if (Locales is null)
            throw new ArgumentException("OpenAPI locales cannot be null.", nameof(Locales));

        var normalizedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, document) in Locales)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("OpenAPI locale names cannot be empty.", nameof(Locales));

            CultureInfo culture;
            try
            {
                culture = CultureInfo.GetCultureInfo(name);
            }
            catch (CultureNotFoundException exception)
            {
                throw new ArgumentException($"'{name}' is not a supported OpenAPI locale.", nameof(Locales), exception);
            }

            if (culture.Name.Length == 0)
                throw new ArgumentException("The invariant culture cannot be used as an OpenAPI locale.", nameof(Locales));

            if (!normalizedNames.Add(culture.Name))
                throw new ArgumentException($"OpenAPI locale '{culture.Name}' is configured more than once.", nameof(Locales));

            if (document is null)
                throw new ArgumentException($"OpenAPI locale '{culture.Name}' has no translation options.", nameof(Locales));

            document.Validate(culture.Name);
        }
    }

    internal IReadOnlyList<KeyValuePair<string, OpenApiLocalizedDocumentOptions>> GetNormalizedLocales()
    {
        Validate();
        return Locales
            .Select(pair => new KeyValuePair<string, OpenApiLocalizedDocumentOptions>(
                CultureInfo.GetCultureInfo(pair.Key).Name,
                pair.Value))
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToArray();
    }

    internal static string GetLocalizedDocumentName(string documentName, string locale)
        => $"{documentName}.{locale}";

    internal static string GetBaseDocumentName(
        string documentName,
        IReadOnlyList<KeyValuePair<string, OpenApiLocalizedDocumentOptions>> locales)
    {
        foreach (var (locale, _) in locales)
        {
            var suffix = $".{locale}";
            if (documentName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                return documentName[..^suffix.Length];
        }

        return documentName;
    }
}

/// <summary>
///     Translation values applied to one API document for a specific locale.
/// </summary>
public sealed class OpenApiLocalizedDocumentOptions
{
    /// <summary>Gets or sets the localized document title.</summary>
    public string? Title { get; set; }

    /// <summary>Gets or sets the localized document description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets localized tag descriptions keyed by the existing tag name.</summary>
    public Dictionary<string, string> Tags { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets localized operation text keyed by operation ID or HTTP method and path.</summary>
    public Dictionary<string, OpenApiLocalizedOperationOptions> Operations { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets localized schema and serialized-property descriptions keyed by generated schema ID.</summary>
    public Dictionary<string, OpenApiLocalizedSchemaOptions> Schemas { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    internal void Validate(string locale)
    {
        ValidateOptionalText(Title, $"OpenAPI locale '{locale}' title");
        ValidateOptionalText(Description, $"OpenAPI locale '{locale}' description");
        ValidateTranslations(Tags, $"OpenAPI locale '{locale}' tags");

        if (Operations is null)
            throw new ArgumentException($"OpenAPI locale '{locale}' operations cannot be null.");

        foreach (var (key, operation) in Operations)
        {
            ValidateKey(key, $"OpenAPI locale '{locale}' operation");
            if (operation is null)
                throw new ArgumentException($"OpenAPI locale '{locale}' operation '{key}' has no translation options.");

            ValidateOptionalText(operation.Summary, $"OpenAPI locale '{locale}' operation '{key}' summary");
            ValidateOptionalText(operation.Description, $"OpenAPI locale '{locale}' operation '{key}' description");
        }

        if (Schemas is null)
            throw new ArgumentException($"OpenAPI locale '{locale}' schemas cannot be null.");

        foreach (var (key, schema) in Schemas)
        {
            ValidateKey(key, $"OpenAPI locale '{locale}' schema");
            if (schema is null)
                throw new ArgumentException($"OpenAPI locale '{locale}' schema '{key}' has no translation options.");

            ValidateOptionalText(schema.Description, $"OpenAPI locale '{locale}' schema '{key}' description");
            ValidateTranslations(schema.Properties, $"OpenAPI locale '{locale}' schema '{key}' properties");
        }
    }

    private static void ValidateTranslations(IDictionary<string, string>? translations, string name)
    {
        if (translations is null)
            throw new ArgumentException($"{name} cannot be null.");

        foreach (var (key, value) in translations)
        {
            ValidateKey(key, name);
            ValidateOptionalText(value, $"{name} '{key}' translation");
        }
    }

    private static void ValidateKey(string? key, string name)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException($"{name} keys cannot be empty.");
    }

    private static void ValidateOptionalText(string? value, string name)
    {
        if (value is not null && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{name} cannot be empty when specified.");
    }
}

/// <summary>
///     Localized operation summary and description values.
/// </summary>
public sealed class OpenApiLocalizedOperationOptions
{
    /// <summary>Gets or sets the localized operation summary.</summary>
    public string? Summary { get; set; }

    /// <summary>Gets or sets the localized operation description.</summary>
    public string? Description { get; set; }
}

/// <summary>
///     Localized schema and serialized-property descriptions.
/// </summary>
public sealed class OpenApiLocalizedSchemaOptions
{
    /// <summary>Gets or sets the localized schema description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets serialized-property descriptions keyed by the wire-format property name.</summary>
    public Dictionary<string, string> Properties { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
