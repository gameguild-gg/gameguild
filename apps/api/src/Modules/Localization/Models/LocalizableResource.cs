namespace GameGuild.Common.Entities;

/// <summary>
/// Base resource that can be localized
/// Provides localization capabilities for resources
/// Mirrors the TypeScript LocalizableResourceBase functionality
/// </summary>
public abstract class LocalizableResource : BaseEntity
{
    private ICollection<ResourceLocalization> _localizations = new List<ResourceLocalization>();

    private string? _defaultLanguageCode = "en-US";

    private bool _isLocalizationEnabled = true;

    /// <summary>
    /// Collection of localizations for this resource
    /// </summary>
    public virtual ICollection<ResourceLocalization> Localizations
    {
        get => _localizations;
        set => _localizations = value;
    }

    /// <summary>
    /// Default language code for this resource
    /// </summary>
    public string? DefaultLanguageCode
    {
        get => _defaultLanguageCode;
        set => _defaultLanguageCode = value;
    }

    /// <summary>
    /// Whether this resource supports localization
    /// </summary>
    public bool IsLocalizationEnabled
    {
        get => _isLocalizationEnabled;
        set => _isLocalizationEnabled = value;
    }
}
