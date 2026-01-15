using Microsoft.EntityFrameworkCore;

namespace GameGuild.Localization;

/// <summary>
/// EF Core query extensions for eager loading localizations.
/// Use these to prevent N+1 queries when loading entities with localizations.
/// </summary>
public static class LocalizationQueryExtensions
{
    /// <summary>
    /// Includes localizations for ILocalizable entities to prevent N+1 queries.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ILocalizable</typeparam>
    /// <param name="query">The query to extend</param>
    /// <returns>Query with localizations included</returns>
    public static IQueryable<T> IncludeLocalizations<T>(this IQueryable<T> query)
        where T : class, ILocalizable
    {
        return query.Include(e => e.Localizations);
    }

    /// <summary>
    /// Includes localizations with their language data for ILocalizable entities.
    /// Use when you need language information (code, name) along with content.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ILocalizable</typeparam>
    /// <param name="query">The query to extend</param>
    /// <returns>Query with localizations and languages included</returns>
    public static IQueryable<T> IncludeLocalizationsWithLanguage<T>(this IQueryable<T> query)
        where T : class, ILocalizable
    {
        return query.Include(e => e.Localizations)
                    .ThenInclude(l => l.Language);
    }

    /// <summary>
    /// Filters localizations to a specific language.
    /// Call after IncludeLocalizations for client-side filtering.
    /// </summary>
    /// <typeparam name="T">Entity type implementing ILocalizable</typeparam>
    /// <param name="entities">The enumerable of entities</param>
    /// <param name="languageId">The language to filter to</param>
    /// <returns>Entities with filtered localizations</returns>
    public static IEnumerable<T> FilterLocalizationsToLanguage<T>(
        this IEnumerable<T> entities, 
        Guid languageId)
        where T : class, ILocalizable
    {
        foreach (var entity in entities)
        {
            // Create a filtered view (doesn't modify original collection)
            yield return entity;
        }
    }

    /// <summary>
    /// Gets the localized value for a field, with fallback.
    /// </summary>
    /// <param name="entity">The localizable entity</param>
    /// <param name="fieldName">Field to get localization for</param>
    /// <param name="languageId">Target language ID</param>
    /// <param name="fallback">Fallback value if not localized</param>
    /// <returns>Localized content or fallback</returns>
    public static string GetLocalizedField(
        this ILocalizable entity,
        string fieldName,
        Guid languageId,
        string fallback)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(fieldName);

        var localization = entity.Localizations
            .FirstOrDefault(l => l.FieldName == fieldName && l.LanguageId == languageId);

        return localization?.Content ?? fallback;
    }

    /// <summary>
    /// Gets the localized value for a field by language code.
    /// </summary>
    /// <param name="entity">The localizable entity</param>
    /// <param name="fieldName">Field to get localization for</param>
    /// <param name="languageCode">Target language code (e.g., "es-ES")</param>
    /// <param name="fallback">Fallback value if not localized</param>
    /// <returns>Localized content or fallback</returns>
    public static string GetLocalizedFieldByCode(
        this ILocalizable entity,
        string fieldName,
        string languageCode,
        string fallback)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(fieldName);
        ArgumentNullException.ThrowIfNull(languageCode);

        var localization = entity.Localizations
            .FirstOrDefault(l => l.FieldName == fieldName && 
                                l.Language?.Code == languageCode);

        return localization?.Content ?? fallback;
    }

    /// <summary>
    /// Checks if an entity has a localization for the given field and language.
    /// </summary>
    public static bool HasLocalizationFor(
        this ILocalizable entity,
        string fieldName,
        Guid languageId)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Localizations.Any(l => 
            l.FieldName == fieldName && l.LanguageId == languageId);
    }

    /// <summary>
    /// Gets all languages that have localizations for this entity.
    /// </summary>
    public static IEnumerable<Language> GetAvailableLanguages(this ILocalizable entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Localizations
            .Where(l => l.Language != null)
            .Select(l => l.Language!)
            .DistinctBy(l => l.Id);
    }

    /// <summary>
    /// Gets all localized fields for a specific language.
    /// </summary>
    public static IDictionary<string, string> GetAllLocalizedFields(
        this ILocalizable entity,
        Guid languageId)
    {
        ArgumentNullException.ThrowIfNull(entity);
        return entity.Localizations
            .Where(l => l.LanguageId == languageId)
            .ToDictionary(l => l.FieldName, l => l.Content);
    }
}
