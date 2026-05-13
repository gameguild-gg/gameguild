using System.Text.Encodings.Web;
using System.Text.RegularExpressions;

namespace GameGuild.Localization;

/// <summary>
/// Provides content sanitization for translated strings to prevent XSS attacks.
/// Used by localization services before returning user-facing content.
/// </summary>
public interface IContentSanitizer
{
    /// <summary>
    /// Sanitizes content by removing dangerous HTML and encoding special characters.
    /// </summary>
    /// <param name="content">The raw content to sanitize.</param>
    /// <returns>Safe content with dangerous elements removed.</returns>
    string Sanitize(string? content);

    /// <summary>
    /// Sanitizes content while preserving a whitelist of safe HTML tags.
    /// </summary>
    /// <param name="content">The raw content to sanitize.</param>
    /// <param name="allowedTags">Tags to preserve (e.g., "b", "i", "em", "strong").</param>
    /// <returns>Safe content with only allowed tags.</returns>
    string SanitizeWithAllowedTags(string? content, IReadOnlySet<string> allowedTags);
}

/// <summary>
/// Default implementation of IContentSanitizer.
/// Uses regex-based sanitization and HTML encoding for XSS prevention.
/// </summary>
public partial class ContentSanitizer : IContentSanitizer
{
    private static readonly IReadOnlySet<string> DefaultAllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "b", "i", "u", "em", "strong", "br"
    };

    // Patterns for dangerous content
    private static readonly Regex ScriptTagPattern = ScriptTagRegex();
    private static readonly Regex EventHandlerPattern = EventHandlerRegex();
    private static readonly Regex JavaScriptUrlPattern = JavaScriptUrlRegex();
    private static readonly Regex DataUrlPattern = DataUrlRegex();
    private static readonly Regex AllHtmlTagsPattern = HtmlTagRegex();

    public string Sanitize(string? content)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        // Step 1: Remove script tags and their content
        var sanitized = ScriptTagPattern.Replace(content, string.Empty);

        // Step 2: Remove event handlers (onclick, onerror, etc.)
        sanitized = EventHandlerPattern.Replace(sanitized, string.Empty);

        // Step 3: Remove javascript: and data: URLs
        sanitized = JavaScriptUrlPattern.Replace(sanitized, string.Empty);
        sanitized = DataUrlPattern.Replace(sanitized, string.Empty);

        // Step 4: Remove all remaining HTML tags
        sanitized = AllHtmlTagsPattern.Replace(sanitized, string.Empty);

        // Step 5: HTML encode any remaining special characters
        sanitized = HtmlEncoder.Default.Encode(sanitized);

        // Step 6: Decode safe entities back (for proper display)
        sanitized = sanitized
            .Replace("&amp;amp;", "&amp;")
            .Replace("&amp;lt;", "&lt;")
            .Replace("&amp;gt;", "&gt;")
            .Replace("&amp;quot;", "&quot;");

        return sanitized.Trim();
    }

    public string SanitizeWithAllowedTags(string? content, IReadOnlySet<string>? allowedTags)
    {
        if (string.IsNullOrEmpty(content))
            return string.Empty;

        allowedTags ??= DefaultAllowedTags;

        // Step 1: Remove script tags and their content
        var sanitized = ScriptTagPattern.Replace(content, string.Empty);

        // Step 2: Remove event handlers
        sanitized = EventHandlerPattern.Replace(sanitized, string.Empty);

        // Step 3: Remove dangerous URLs
        sanitized = JavaScriptUrlPattern.Replace(sanitized, string.Empty);
        sanitized = DataUrlPattern.Replace(sanitized, string.Empty);

        // Step 4: Process HTML tags - keep allowed, remove others
        sanitized = AllHtmlTagsPattern.Replace(sanitized, match =>
        {
            var tagMatch = ExtractTagNameRegex().Match(match.Value);
            if (tagMatch.Success)
            {
                var tagName = tagMatch.Groups[1].Value;
                if (allowedTags.Contains(tagName))
                {
                    // Keep the tag but sanitize its attributes
                    return SanitizeTag(match.Value, tagName);
                }
            }
            return string.Empty;
        });

        return sanitized.Trim();
    }

    private static string SanitizeTag(string fullTag, string tagName)
    {
        // Closing tags must be handled before self-closing detection.
        if (fullTag.StartsWith("</", StringComparison.Ordinal))
        {
            return $"</{tagName}>";
        }

        // For self-closing tags like <br />
        if (fullTag.EndsWith("/>", StringComparison.Ordinal))
        {
            return $"<{tagName} />";
        }

        // For opening tags, remove all attributes (potential XSS vectors)
        return $"<{tagName}>";
    }

    [GeneratedRegex(@"<script\b[^<]*(?:(?!</script>)<[^<]*)*</script>", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ScriptTagRegex();

    [GeneratedRegex(@"\bon\w+\s*=\s*[""'][^""']*[""']", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex EventHandlerRegex();

    [GeneratedRegex(@"javascript\s*:", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex JavaScriptUrlRegex();

    [GeneratedRegex(@"data\s*:\s*[^;]+;base64", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DataUrlRegex();

    [GeneratedRegex(@"</?[a-zA-Z][^>]*>", RegexOptions.Compiled)]
    private static partial Regex HtmlTagRegex();

    [GeneratedRegex(@"</?([a-zA-Z]+)", RegexOptions.Compiled)]
    private static partial Regex ExtractTagNameRegex();
}
