namespace GameGuild.Resources.Contents;

/// <summary>
/// Reusable document template whose published body is stored as a content version.
/// </summary>
public class DocumentTemplate : EntityBase
{
    public const string VersionEntityType = "DocumentTemplate";

    private DocumentTemplate() { }

    public string TemplateKey { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Category { get; private set; }
    public string? SupportedEntityType { get; private set; }
    public string? PlaceholderSchema { get; private set; }
    public bool IsSystemTemplate { get; private set; }

    public static DocumentTemplate Create(
        string templateKey,
        string name,
        string? description = null,
        string? category = null,
        string? supportedEntityType = null,
        string? placeholderSchema = null,
        bool isSystemTemplate = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(templateKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new DocumentTemplate
        {
            TemplateKey = templateKey.Trim(),
            Name = name.Trim(),
            Description = Normalize(description),
            Category = Normalize(category),
            SupportedEntityType = Normalize(supportedEntityType),
            PlaceholderSchema = Normalize(placeholderSchema),
            IsSystemTemplate = isSystemTemplate,
        };
    }

    public void Update(
        string name,
        string? description = null,
        string? category = null,
        string? supportedEntityType = null,
        string? placeholderSchema = null,
        bool? isSystemTemplate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        Name = name.Trim();
        Description = Normalize(description);
        Category = Normalize(category);
        SupportedEntityType = Normalize(supportedEntityType);
        PlaceholderSchema = Normalize(placeholderSchema);

        if (isSystemTemplate.HasValue)
        {
            IsSystemTemplate = isSystemTemplate.Value;
        }

        Touch();
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
