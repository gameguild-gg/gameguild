namespace GameGuild.Localization;

/// <summary>
///     Provides data access operations for localization languages.
/// </summary>
public interface ILanguageRepository
{
    /// <summary>
    ///     Retrieves the platform's default language.
    /// </summary>
    Task<Language?> GetDefaultAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves a language by its unique identifier.
    /// </summary>
    Task<Language?> GetByIdAsync(Guid languageId, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Retrieves a language by its ISO code.
    /// </summary>
    Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Lists all active languages ordered by display name.
    /// </summary>
    Task<IReadOnlyList<Language>> GetActiveAsync(CancellationToken cancellationToken = default);
}
