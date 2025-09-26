using GameGuild.Database;

namespace GameGuild.Modules.Localization;

/// <summary>
/// Entity Framework implementation for <see cref="ILanguageRepository"/>.
/// </summary>
public sealed class LanguageRepository(ApplicationDbContext context) : ILanguageRepository
{
    private readonly ApplicationDbContext _context = context;

    public async Task<Language?> GetDefaultAsync(CancellationToken cancellationToken = default) { return await _context.Languages.AsNoTracking().FirstOrDefaultAsync(language => language.IsDefault, cancellationToken); }

    public async Task<Language?> GetByIdAsync(Guid languageId, CancellationToken cancellationToken = default)
    {
        return await _context.Languages.AsNoTracking().FirstOrDefaultAsync(language => language.Id == languageId, cancellationToken);
    }

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await _context.Languages.AsNoTracking().FirstOrDefaultAsync(language => language.Code == code, cancellationToken);
    }

    public async Task<IReadOnlyList<Language>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        List<Language> languages = await _context.Languages.AsNoTracking().Where(language => language.IsActive).OrderBy(language => language.Name).ToListAsync(cancellationToken);

        return languages.AsReadOnly();
    }
}
