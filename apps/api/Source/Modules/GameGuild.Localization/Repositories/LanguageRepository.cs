using Microsoft.EntityFrameworkCore;

namespace GameGuild.Localization;

/// <summary>
///     Entity Framework implementation for <see cref="ILanguageRepository" />.
/// </summary>
public sealed class LanguageRepository(IApplicationDbContext context) : ILanguageRepository
{
    private readonly IApplicationDbContext _context = context;

    public async Task<Language?> GetDefaultAsync(CancellationToken cancellationToken = default) { return await _context.Set<Language>().AsNoTracking().FirstOrDefaultAsync(language => language.IsDefault, cancellationToken).ConfigureAwait(false); }

    public async Task<Language?> GetByIdAsync(Guid languageId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Language>().AsNoTracking().FirstOrDefaultAsync(language => language.Id == languageId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<Language?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        return await _context.Set<Language>().AsNoTracking().FirstOrDefaultAsync(language => language.Code == code, cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<Language>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        var languages = await _context.Set<Language>().AsNoTracking().Where(language => language.IsActive).OrderBy(language => language.Name).ToListAsync(cancellationToken).ConfigureAwait(false);

        return languages.AsReadOnly();
    }
}
