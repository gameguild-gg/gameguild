using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace GameGuild.Modules.Authentication;

/// <summary>
/// Implementation of username slugification service
/// </summary>
public partial class UsernameSlugificationService : IUsernameSlugificationService
{
    private const int MaxSlugLength = 50;
    private const int MaxAttempts = 100;

    /// <inheritdoc />
    public string Slugify(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be empty", nameof(username));

        // Convert to lowercase and normalize unicode
        var slug = username.ToLowerInvariant().Normalize(NormalizationForm.FormD);

        // Remove diacritics (accents)
        var chars = slug.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        slug = new string(chars.ToArray()).Normalize(NormalizationForm.FormC);

        // Replace spaces and underscores with hyphens
        slug = slug.Replace(' ', '-').Replace('_', '-');

        // Remove invalid characters (keep alphanumeric and hyphens only)
        slug = InvalidCharsRegex().Replace(slug, string.Empty);

        // Replace multiple consecutive hyphens with single hyphen
        slug = MultipleHyphensRegex().Replace(slug, "-");

        // Remove leading/trailing hyphens
        slug = slug.Trim('-');

        // Truncate to max length
        if (slug.Length > MaxSlugLength)
            slug = slug[..MaxSlugLength].TrimEnd('-');

        return slug;
    }

    /// <inheritdoc />
    public bool IsValidSlug(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return false;

        if (slug.Length > MaxSlugLength)
            return false;

        // Must start with alphanumeric
        if (!char.IsLetterOrDigit(slug[0]))
            return false;

        // Must contain only lowercase alphanumeric and hyphens
        return ValidSlugRegex().IsMatch(slug);
    }

    /// <inheritdoc />
    public async Task<string> GenerateUniqueSlugAsync(string username, Func<string, Task<bool>> checkAvailability)
    {
        var baseSlug = Slugify(username);

        if (await checkAvailability(baseSlug))
            return baseSlug;

        // Try appending numbers
        for (int i = 1; i <= MaxAttempts; i++)
        {
            var candidate = $"{baseSlug}-{i}";

            // Ensure we don't exceed max length
            if (candidate.Length > MaxSlugLength)
            {
                var trimLength = MaxSlugLength - i.ToString().Length - 1; // -1 for hyphen
                candidate = $"{baseSlug[..trimLength]}-{i}";
            }

            if (await checkAvailability(candidate))
                return candidate;
        }

        // Fallback to GUID suffix
        var guidSuffix = Guid.NewGuid().ToString("N")[..8];
        var fallbackSlug = $"{baseSlug[..(MaxSlugLength - 9)]}-{guidSuffix}";

        return fallbackSlug;
    }

    /// <inheritdoc />
    public string Normalize(string username)
    {
        return username.ToLowerInvariant().Trim();
    }

    [GeneratedRegex(@"[^a-z0-9-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"-{2,}")]
    private static partial Regex MultipleHyphensRegex();

    [GeneratedRegex(@"^[a-z0-9][a-z0-9-]*[a-z0-9]$")]
    private static partial Regex ValidSlugRegex();
}
