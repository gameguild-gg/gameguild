using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit;

internal sealed class LanguageRepositoryDbContext : DbContext, IApplicationDbContext
{
    public LanguageRepositoryDbContext(DbContextOptions<LanguageRepositoryDbContext> options)
        : base(options)
    {
    }

    public DbSet<Language> Languages => Set<Language>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Language>();
        base.OnModelCreating(modelBuilder);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }
}

public class LanguageRepositoryTests : IAsyncDisposable
{
    private readonly LanguageRepositoryDbContext _context;
    private readonly LanguageRepository _repository;

    public LanguageRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<LanguageRepositoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new LanguageRepositoryDbContext(options);
        _repository = new LanguageRepository(_context);
    }

    public async ValueTask DisposeAsync()
    {
        await _context.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetDefaultAsync_ReturnsDefaultLanguage()
    {
        await SeedLanguagesAsync(
            new Language { Code = "en-US", Name = "English", IsDefault = true, IsActive = true },
            new Language { Code = "es-ES", Name = "Spanish", IsDefault = false, IsActive = true });

        var result = await _repository.GetDefaultAsync();

        result.Should().NotBeNull();
        result!.Code.Should().Be("en-US");
        result.IsDefault.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsLanguage()
    {
        var language = new Language { Code = "de-DE", Name = "German", IsDefault = false, IsActive = true };
        await SeedLanguagesAsync(language);

        var result = await _repository.GetByIdAsync(language.Id);

        result.Should().NotBeNull();
        result!.Code.Should().Be("de-DE");
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsLanguage()
    {
        await SeedLanguagesAsync(new Language { Code = "pt-BR", Name = "Portuguese", IsDefault = false, IsActive = true });

        var result = await _repository.GetByCodeAsync("pt-BR");

        result.Should().NotBeNull();
        result!.Name.Should().Be("Portuguese");
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyActiveLanguages_OrderedByName()
    {
        await SeedLanguagesAsync(
            new Language { Code = "ja-JP", Name = "Japanese", IsDefault = false, IsActive = true },
            new Language { Code = "ar-SA", Name = "Arabic", IsDefault = false, IsActive = true },
            new Language { Code = "fr-FR", Name = "French", IsDefault = false, IsActive = false });

        var result = await _repository.GetActiveAsync();

        result.Should().HaveCount(2);
        result.Select(language => language.Name).Should().Equal("Arabic", "Japanese");
    }

    [Fact]
    public async Task GetByCodeAsync_ReturnsDetachedEntity()
    {
        var language = new Language { Code = "sv-SE", Name = "Swedish", IsDefault = false, IsActive = true };
        await SeedLanguagesAsync(language);

        var result = await _repository.GetByCodeAsync("sv-SE");

        result.Should().NotBeNull();
        _context.Entry(result!).State.Should().Be(EntityState.Detached);
    }

    private async Task SeedLanguagesAsync(params Language[] languages)
    {
        await _context.Set<Language>().AddRangeAsync(languages);
        await _context.SaveChangesAsync();
    }
}
