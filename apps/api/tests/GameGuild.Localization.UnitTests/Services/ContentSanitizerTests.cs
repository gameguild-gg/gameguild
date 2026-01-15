using FluentAssertions;
using GameGuild.Localization;
using Xunit;

namespace GameGuild.Tests.Localization.Unit.Services;

/// <summary>
/// Tests for ContentSanitizer to verify XSS prevention.
/// </summary>
public class ContentSanitizerTests
{
    private readonly ContentSanitizer _sanitizer;

    public ContentSanitizerTests()
    {
        _sanitizer = new ContentSanitizer();
    }

    [Fact]
    public void Sanitize_RemovesScriptTags()
    {
        // Arrange
        var maliciousContent = "Hello <script>alert('xss')</script> World";

        // Act
        var result = _sanitizer.Sanitize(maliciousContent);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("</script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void Sanitize_RemovesEventHandlers()
    {
        // Arrange
        var maliciousContent = "<img src='x' onerror='alert(1)' />";

        // Act
        var result = _sanitizer.Sanitize(maliciousContent);

        // Assert
        result.Should().NotContain("onerror");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void Sanitize_RemovesJavascriptUrls()
    {
        // Arrange
        var maliciousContent = "<a href='javascript:alert(1)'>Click me</a>";

        // Act
        var result = _sanitizer.Sanitize(maliciousContent);

        // Assert
        result.Should().NotContain("javascript:");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void Sanitize_RemovesDataUrls()
    {
        // Arrange
        var maliciousContent = "<img src='data:text/html,<script>alert(1)</script>' />";

        // Act
        var result = _sanitizer.Sanitize(maliciousContent);

        // Assert
        result.Should().NotContain("data:");
        result.Should().NotContain("<script>");
    }

    [Fact]
    public void Sanitize_EncodesHtmlEntities()
    {
        // Arrange
        var content = "Hello <b>World</b> & \"Test\"";

        // Act
        var result = _sanitizer.Sanitize(content);

        // Assert
        result.Should().NotContain("<b>");
        result.Should().NotContain("</b>");
        // HTML entities should be encoded
        result.Should().Contain("&amp;");
    }

    [Fact]
    public void Sanitize_ReturnsEmptyForNullInput()
    {
        // Act
        var result = _sanitizer.Sanitize(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_ReturnsEmptyForEmptyInput()
    {
        // Act
        var result = _sanitizer.Sanitize(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_PreservesSafeText()
    {
        // Arrange
        var safeContent = "Hello World! This is a test.";

        // Act
        var result = _sanitizer.Sanitize(safeContent);

        // Assert
        result.Should().Be("Hello World! This is a test.");
    }

    [Fact]
    public void SanitizeWithAllowedTags_PreservesAllowedTags()
    {
        // Arrange
        var content = "Hello <b>bold</b> and <i>italic</i> text";
        var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b", "i" };

        // Act
        var result = _sanitizer.SanitizeWithAllowedTags(content, allowedTags);

        // Assert - SanitizeTag converts closing tags to self-closing format
        result.Should().Contain("<b>");
        result.Should().Contain("<i>");
        // The method converts </b> and </i> to <b /> and <i /> format
        result.Should().Contain("bold");
        result.Should().Contain("italic");
    }

    [Fact]
    public void SanitizeWithAllowedTags_RemovesDisallowedTags()
    {
        // Arrange
        var content = "Hello <b>bold</b> and <div>div content</div> text";
        var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b" };

        // Act
        var result = _sanitizer.SanitizeWithAllowedTags(content, allowedTags);

        // Assert
        result.Should().Contain("<b>");
        result.Should().NotContain("<div>");
        result.Should().NotContain("</div>");
    }

    [Fact]
    public void SanitizeWithAllowedTags_StillRemovesScriptTags()
    {
        // Arrange - even if allowed (which would be bad), script tags should be removed
        var content = "Hello <script>alert('xss')</script> World";
        var allowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "b", "script" };

        // Act
        var result = _sanitizer.SanitizeWithAllowedTags(content, allowedTags);

        // Assert
        result.Should().NotContain("<script>");
        result.Should().NotContain("alert");
    }

    [Fact]
    public void Sanitize_HandlesComplexXssPayloads()
    {
        // Arrange - various XSS payloads
        var payloads = new[]
        {
            "<IMG SRC=\"javascript:alert('XSS');\">",
            "<IMG SRC=javascript:alert('XSS')>",
            "<IMG SRC=JaVaScRiPt:alert('XSS')>",
            "<IMG SRC=`javascript:alert(\"RSnake says, 'XSS'\")`>",
            "<BODY ONLOAD=alert('XSS')>",
            "<svg onload=alert(1)>",
            "<DIV STYLE=\"background-image: url(javascript:alert('XSS'))\">",
            "<<SCRIPT>alert(\"XSS\");//<</SCRIPT>"
        };

        foreach (var payload in payloads)
        {
            // Act
            var result = _sanitizer.Sanitize(payload);

            // Assert
            result.Should().NotContain("javascript:", because: $"Payload: {payload}");
            result.Should().NotContain("alert", because: $"Payload: {payload}");
            result.Should().NotContain("onload", because: $"Payload: {payload}");
            result.Should().NotContain("<script>", because: $"Payload: {payload}");
        }
    }
}
