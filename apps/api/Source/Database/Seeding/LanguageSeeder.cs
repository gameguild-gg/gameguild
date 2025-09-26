using System;
using System.Collections.Generic;
using GameGuild.Database;
using GameGuild.Database.Seeding;
using GameGuild.Modules.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Source.Database.Seeding;

/// <summary>
/// Seeds supported languages for the application, ensuring a single default entry exists.
/// </summary>
/// <param name="logger">Logger instance for diagnostic output.</param>
public class LanguageSeeder(ILogger<LanguageSeeder> logger) : IDataSeeder
{
    private readonly ILogger<LanguageSeeder> _logger = logger;

    private static readonly IReadOnlyList<(string Code, string Name, bool IsDefault)> SeedLanguages =
    [
        ("en-US", "English (United States)", true),
        ("pt-BR", "Português (Brasil)", false),
        ("es-ES", "Español (España)", false)
    ];

    public async Task SeedAsync(ApplicationDbContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting language seeding...");

        foreach ((string code, string name, bool isDefault) in SeedLanguages)
        {
            Language? existingLanguage = await context.Languages.FirstOrDefaultAsync(language => language.Code == code, cancellationToken);

            if (existingLanguage is null)
            {
                _logger.LogInformation("Adding language {LanguageCode} - {LanguageName}", code, name);

                Language language = new()
                {
                    Code = code,
                    Name = name,
                    IsActive = true,
                    IsDefault = isDefault
                };

                _ = context.Languages.Add(language);
            }
            else
            {
                if (!existingLanguage.IsActive)
                {
                    existingLanguage.IsActive = true;
                }

                if (existingLanguage.IsDefault != isDefault)
                {
                    existingLanguage.IsDefault = isDefault;
                    _logger.LogInformation("Updated default flag for language {LanguageCode}", existingLanguage.Code);
                }
            }

            if (isDefault)
            {
                List<Language> otherDefaults = await context.Languages
                    .Where(language => language.Code != code && language.IsDefault)
                    .ToListAsync(cancellationToken);

                foreach (Language otherDefault in otherDefaults)
                {
                    _logger.LogInformation("Removing default flag from language {LanguageCode}", otherDefault.Code);
                    otherDefault.IsDefault = false;
                }
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        Language? defaultLanguage = await context.Languages.FirstOrDefaultAsync(language => language.IsDefault, cancellationToken);

        if (defaultLanguage is null)
        {
            Language? fallbackLanguage = await context.Languages.FirstOrDefaultAsync(language => language.Code == SeedLanguages[0].Code, cancellationToken);

            if (fallbackLanguage is not null)
            {
                fallbackLanguage.IsDefault = true;
                _ = await context.SaveChangesAsync(cancellationToken);
                _logger.LogWarning("No default language detected. Promoted {LanguageCode} as default.", fallbackLanguage.Code);
            }
            else
            {
                _logger.LogWarning("No languages available to promote as default.");
            }
        }

        _logger.LogInformation("Language seeding completed.");
    }
}
