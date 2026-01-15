using GameGuild.Entities;

namespace GameGuild.Localization;

/// <summary>
/// Abstract base class for entities that support localization.
/// Provides a default implementation of ILocalizable to satisfy OCP -
/// new localizable entities only need to inherit, not implement.
/// </summary>
/// <typeparam name="TLocalization">The localization entity type</typeparam>
public abstract class LocalizableEntityBase<TLocalization> : EntityBase, ILocalizable
    where TLocalization : ResourceLocalization, new()
{
    /// <summary>
    /// Collection of localizations for this entity
    /// </summary>
    public virtual ICollection<ResourceLocalization> Localizations { get; set; } = new List<ResourceLocalization>();

    /// <summary>
    /// Adds a localization for a field on this entity.
    /// Default implementation creates a ResourceLocalization and adds to collection.
    /// </summary>
    /// <param name="fieldName">The field to localize (e.g., "Title", "Description")</param>
    /// <param name="content">The localized content</param>
    /// <param name="language">The target language</param>
    /// <param name="status">Initial localization status (default: Draft)</param>
    /// <returns>The created localization entity</returns>
    public virtual ResourceLocalization AddLocalization(
        string fieldName,
        string content,
        Language language,
        LocalizationStatus status = LocalizationStatus.Draft)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(language);

        var localization = new TLocalization
        {
            FieldName = fieldName,
            Content = content,
            LanguageId = language.Id,
            Language = language,
            Status = status
        };

        Localizations.Add(localization);
        return localization;
    }

    /// <summary>
    /// Gets the localization for a specific field and language.
    /// </summary>
    /// <param name="fieldName">The field name to retrieve</param>
    /// <param name="language">The target language</param>
    /// <returns>The localization if found, null otherwise</returns>
    public virtual ResourceLocalization? GetLocalization(string fieldName, Language language)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(language);

        return Localizations.FirstOrDefault(l => 
            l.FieldName == fieldName && 
            l.LanguageId == language.Id);
    }

    /// <summary>
    /// Gets all localizations for a specific field.
    /// </summary>
    /// <param name="fieldName">The field name</param>
    /// <returns>Collection of localizations for the field</returns>
    public virtual IEnumerable<ResourceLocalization> GetLocalizationsForField(string fieldName)
    {
        ArgumentNullException.ThrowIfNull(fieldName);
        return Localizations.Where(l => l.FieldName == fieldName);
    }

    /// <summary>
    /// Gets all localizations for a specific language.
    /// </summary>
    /// <param name="language">The target language</param>
    /// <returns>Collection of localizations in that language</returns>
    public virtual IEnumerable<ResourceLocalization> GetLocalizationsForLanguage(Language language)
    {
        ArgumentNullException.ThrowIfNull(language);
        return Localizations.Where(l => l.LanguageId == language.Id);
    }

    /// <summary>
    /// Checks if a localization exists for the given field and language.
    /// </summary>
    public virtual bool HasLocalization(string fieldName, Language language)
    {
        return GetLocalization(fieldName, language) != null;
    }

    /// <summary>
    /// Removes a localization for a specific field and language.
    /// </summary>
    /// <returns>True if removed, false if not found</returns>
    public virtual bool RemoveLocalization(string fieldName, Language language)
    {
        var localization = GetLocalization(fieldName, language);
        if (localization == null)
            return false;

        Localizations.Remove(localization);
        return true;
    }

    /// <summary>
    /// Updates an existing localization or adds a new one.
    /// </summary>
    public virtual ResourceLocalization UpsertLocalization(
        string fieldName,
        string content,
        Language language,
        LocalizationStatus status = LocalizationStatus.Draft)
    {
        var existing = GetLocalization(fieldName, language);
        if (existing != null)
        {
            existing.Content = content;
            existing.Status = status;
            return existing;
        }

        return AddLocalization(fieldName, content, language, status);
    }
}
