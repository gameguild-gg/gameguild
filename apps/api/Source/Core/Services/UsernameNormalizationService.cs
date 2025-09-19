using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;


namespace GameGuild.Source.Core.Services;

/// <summary>
/// Service for normalizing usernames and other identifiers using slugify
/// Ensures consistent, URL-friendly, and unique identifiers across the system
/// </summary>
public interface IUsernameNormalizationService {
    /// <summary>
    /// Normalize a username to a slug-friendly format
    /// </summary>
    /// <param name="input">Input username or name</param>
    /// <param name="maxLength">Maximum length of normalized username</param>
    /// <returns>Normalized username</returns>
    string NormalizeUsername(string input, int maxLength = 50);

    /// <summary>
    /// Generate a unique username by checking against existing usernames
    /// </summary>
    /// <param name="input">Base input for username</param>
    /// <param name="existingUsernames">Collection of existing usernames to check against</param>
    /// <param name="maxLength">Maximum length of normalized username</param>
    /// <returns>Unique normalized username</returns>
    string GenerateUniqueUsername(string input, IEnumerable<string> existingUsernames, int maxLength = 50);

    /// <summary>
    /// Slugify any identifier (tenant names, project names, etc.)
    /// </summary>
    /// <param name="input">Input text to slugify</param>
    /// <param name="maxLength">Maximum length of slug</param>
    /// <returns>URL-friendly slug</returns>
    string Slugify(string input, int maxLength = 100);

    /// <summary>
    /// Generate a unique slug by checking against existing slugs
    /// </summary>
    /// <param name="input">Base input for slug</param>
    /// <param name="existingSlugs">Collection of existing slugs to check against</param>
    /// <param name="maxLength">Maximum length of slug</param>
    /// <returns>Unique slug</returns>
    string GenerateUniqueSlug(string input, IEnumerable<string> existingSlugs, int maxLength = 100);

    /// <summary>
    /// Validate if a username meets the requirements
    /// </summary>
    /// <param name="username">Username to validate</param>
    /// <returns>True if valid</returns>
    bool IsValidUsername(string username);

    /// <summary>
    /// Validate if a slug meets the requirements
    /// </summary>
    /// <param name="slug">Slug to validate</param>
    /// <returns>True if valid</returns>
    bool IsValidSlug(string slug);

    /// <summary>
    /// Check if a username is reserved
    /// </summary>
    /// <param name="username">Username to check</param>
    /// <returns>True if reserved</returns>
    bool IsReservedUsername(string username);

    /// <summary>
    /// Get all reserved usernames
    /// </summary>
    /// <returns>Collection of reserved usernames</returns>
    IEnumerable<string> GetReservedUsernames();
}

/// <summary>
/// Implementation of username normalization service
/// </summary>
public class UsernameNormalizationService : IUsernameNormalizationService {
    private static readonly Regex InvalidCharsRegex = new(@"[^a-z0-9\-_.]", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex MultipleHyphensRegex = new(@"-{2,}", RegexOptions.Compiled);
    private static readonly Regex StartEndHyphensRegex = new(@"^-+|-+$", RegexOptions.Compiled);
    private static readonly Regex UsernameValidationRegex = new(@"^[a-z0-9][a-z0-9\-_.]*[a-z0-9]$|^[a-z0-9]$", RegexOptions.Compiled);
    private static readonly Regex SlugValidationRegex = new(@"^[a-z0-9][a-z0-9\-]*[a-z0-9]$|^[a-z0-9]$", RegexOptions.Compiled);

    // Reserved usernames that cannot be used
    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.OrdinalIgnoreCase) {
    // System/Admin
    "admin", "administrator", "root", "system", "sysadmin", "superuser", "superadmin", "sa",
    
    // API/Technical
    "api", "www", "mail", "email", "smtp", "pop", "imap", "ftp", "ssh", "ssl", "tls",
    "http", "https", "tcp", "udp", "dns", "dhcp", "proxy", "gateway", "firewall",
    
    // Application specific
    "tenant", "user", "users", "profile", "account", "settings", "config", "configuration",
    "dashboard", "home", "index", "main", "app", "application", "service", "services",
    "auth", "authentication", "authorization", "login", "logout", "signin", "signup",
    "register", "registration", "password", "forgot", "reset", "verify", "verification",
    
    // Content/Pages
    "about", "contact", "help", "support", "privacy", "terms", "legal", "blog", "news",
    "docs", "documentation", "guide", "tutorial", "faq", "search", "explore", "discover",
    
    // Actions
    "create", "new", "add", "edit", "update", "delete", "remove", "save", "cancel",
    "submit", "send", "post", "get", "put", "patch", "head", "options", "trace",
    
    // Common words that might conflict
    "null", "undefined", "true", "false", "yes", "no", "on", "off", "none", "all",
    "public", "private", "protected", "internal", "static", "const", "var", "let",
    "function", "class", "object", "array", "string", "number", "boolean", "date",
    
    // Gaming specific
    "game", "guild", "player", "team", "match", "tournament", "league", "season",
    "rank", "level", "score", "achievement", "badge", "reward", "point", "coin",
    
    // Social/Community
    "follow", "following", "follower", "friend", "friends", "group", "groups",
    "community", "forum", "chat", "message", "notification", "invite", "share",
    
    // File/Media related
    "file", "files", "image", "images", "video", "videos", "audio", "media",
    "upload", "download", "export", "import", "backup", "restore",
    
    // Status/States
    "active", "inactive", "enabled", "disabled", "online", "offline", "available",
    "busy", "away", "pending", "approved", "rejected", "banned", "suspended"
  };

    public string NormalizeUsername(string input, int maxLength = 50) {
        if (string.IsNullOrWhiteSpace(input)) {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var normalized = input.Trim().ToLowerInvariant();

        // Remove accents and diacritics
        normalized = RemoveDiacritics(normalized);

        // Replace spaces and underscores with hyphens
        normalized = normalized.Replace(' ', '-').Replace('_', '-');

        // Remove invalid characters (keep only a-z, 0-9, hyphens, dots)
        normalized = InvalidCharsRegex.Replace(normalized, "");

        // Replace multiple consecutive hyphens with single hyphen
        normalized = MultipleHyphensRegex.Replace(normalized, "-");

        // Remove leading/trailing hyphens
        normalized = StartEndHyphensRegex.Replace(normalized, "");

        // Ensure it doesn't start or end with a dot
        normalized = normalized.Trim('.');

        // Truncate to max length
        if (normalized.Length > maxLength) {
            normalized = normalized.Substring(0, maxLength).TrimEnd('-').TrimEnd('.');
        }

        // Ensure minimum length and valid format
        if (string.IsNullOrEmpty(normalized) || normalized.Length < 2) {
            // Generate a fallback based on first few characters of original input
            var fallback = "user-" + Guid.NewGuid().ToString("N")[..8];
            return fallback.Length > maxLength ? fallback.Substring(0, maxLength) : fallback;
        }

        return normalized;
    }

    public string GenerateUniqueUsername(string input, IEnumerable<string> existingUsernames, int maxLength = 50) {
        var baseUsername = NormalizeUsername(input, maxLength - 4); // Reserve space for suffix
        var uniqueUsername = baseUsername;
        var counter = 1;

        var existingSet = new HashSet<string>(existingUsernames, StringComparer.OrdinalIgnoreCase);

        while (existingSet.Contains(uniqueUsername) || IsReservedUsername(uniqueUsername)) {
            var suffix = counter.ToString();
            var maxBaseLength = maxLength - suffix.Length - 1; // -1 for hyphen

            if (baseUsername.Length > maxBaseLength) {
                uniqueUsername = baseUsername.Substring(0, maxBaseLength) + "-" + suffix;
            }
            else {
                uniqueUsername = baseUsername + "-" + suffix;
            }

            counter++;

            // Prevent infinite loop
            if (counter > 9999) {
                uniqueUsername = "user-" + Guid.NewGuid().ToString("N")[..8];
                break;
            }
        }

        return uniqueUsername;
    }

    public string Slugify(string input, int maxLength = 100) {
        if (string.IsNullOrWhiteSpace(input)) {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        var slug = input.Trim().ToLowerInvariant();

        // Remove accents and diacritics
        slug = RemoveDiacritics(slug);

        // Replace spaces and other separators with hyphens
        slug = Regex.Replace(slug, @"[\s_\.]+", "-");

        // Remove invalid characters (keep only a-z, 0-9, hyphens)
        slug = Regex.Replace(slug, @"[^a-z0-9\-]", "");

        // Replace multiple consecutive hyphens with single hyphen
        slug = MultipleHyphensRegex.Replace(slug, "-");

        // Remove leading/trailing hyphens
        slug = StartEndHyphensRegex.Replace(slug, "");

        // Truncate to max length
        if (slug.Length > maxLength) {
            slug = slug.Substring(0, maxLength).TrimEnd('-');
        }

        // Ensure minimum length
        if (string.IsNullOrEmpty(slug) || slug.Length < 2) {
            slug = "item-" + Guid.NewGuid().ToString("N")[..8];
            return slug.Length > maxLength ? slug.Substring(0, maxLength) : slug;
        }

        return slug;
    }

    public string GenerateUniqueSlug(string input, IEnumerable<string> existingSlugs, int maxLength = 100) {
        var baseSlug = Slugify(input, maxLength - 4); // Reserve space for suffix
        var uniqueSlug = baseSlug;
        var counter = 1;

        var existingSet = new HashSet<string>(existingSlugs, StringComparer.OrdinalIgnoreCase);

        while (existingSet.Contains(uniqueSlug)) {
            var suffix = counter.ToString();
            var maxBaseLength = maxLength - suffix.Length - 1; // -1 for hyphen

            if (baseSlug.Length > maxBaseLength) {
                uniqueSlug = baseSlug.Substring(0, maxBaseLength) + "-" + suffix;
            }
            else {
                uniqueSlug = baseSlug + "-" + suffix;
            }

            counter++;

            // Prevent infinite loop
            if (counter > 9999) {
                uniqueSlug = "item-" + Guid.NewGuid().ToString("N")[..8];
                break;
            }
        }

        return uniqueSlug;
    }

    public bool IsValidUsername(string username) {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 2 || username.Length > 50) {
            return false;
        }

        return UsernameValidationRegex.IsMatch(username) && !IsReservedUsername(username);
    }

    public bool IsValidSlug(string slug) {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 2 || slug.Length > 100) {
            return false;
        }

        return SlugValidationRegex.IsMatch(slug);
    }

    public bool IsReservedUsername(string username) {
        return ReservedUsernames.Contains(username);
    }

    public IEnumerable<string> GetReservedUsernames() {
        return ReservedUsernames.ToList();
    }

    private static string RemoveDiacritics(string text) {
        var normalizedString = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalizedString) {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark) {
                stringBuilder.Append(c);
            }
        }

        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}