namespace GameGuild.Localization;

/// <summary>
/// Service for managing field-level localizations of resources.
/// Provides CRUD operations for ResourceLocalization entities.
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Gets the localization for a specific resource and language.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="languageId">The ID of the language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The localization if found; otherwise null.</returns>
    Task<ResourceLocalization?> GetLocalizationAsync(
        Guid resourceId, 
        Guid languageId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all localizations for a specific resource.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of all localizations for the resource.</returns>
    Task<IReadOnlyList<ResourceLocalization>> GetAllLocalizationsAsync(
        Guid resourceId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new localization for a resource.
    /// </summary>
    /// <param name="localization">The localization to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created localization.</returns>
    Task<ResourceLocalization> CreateLocalizationAsync(
        ResourceLocalization localization, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing localization.
    /// </summary>
    /// <param name="localization">The localization with updated values.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated localization.</returns>
    Task<ResourceLocalization> UpdateLocalizationAsync(
        ResourceLocalization localization, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a localization for a resource and language.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="languageId">The ID of the language.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteLocalizationAsync(
        Guid resourceId, 
        Guid languageId, 
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all localizations for a specific field on a resource.
    /// </summary>
    /// <param name="resourceId">The ID of the resource.</param>
    /// <param name="fieldName">The name of the field (e.g., "Title", "Description").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>All localizations for the specified field.</returns>
    Task<IReadOnlyList<ResourceLocalization>> GetLocalizationsForFieldAsync(
        Guid resourceId, 
        string fieldName, 
        CancellationToken cancellationToken = default);
}
