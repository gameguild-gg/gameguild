using FluentAssertions;

namespace GameGuild.API.UnitTests.Core;

public sealed class AuthenticationSourceHygieneTests
{
    [Fact]
    public void Authentication_sources_should_not_contain_temporary_debug_or_secret_logging()
    {
        var sourceRoot = FindSourceRoot();
        var files = new[]
        {
            Path.Combine(sourceRoot, "GameGuild.API", "Core", "Endpoints", "AuthenticationEndpoint.cs"),
            Path.Combine(sourceRoot, "Modules", "GameGuild.Identity.Authentication", "Services", "LocalAuthService.cs")
        };
        var forbiddenPatterns = new[]
        {
            "TEMPORARY DEBUG",
            "[DEBUG",
            "DEBUG:",
            "Password length",
            "Hash: {Hash}",
            "[AUTHSERVICE]",
            "Processing refresh token request: {RefreshToken}"
        };

        var violations = files
            .SelectMany(file => File.ReadLines(file)
                .Select((line, index) => new { File = file, Line = line, LineNumber = index + 1 }))
            .Where(entry => forbiddenPatterns.Any(pattern => entry.Line.Contains(pattern, StringComparison.Ordinal)))
            .Select(entry => $"{Path.GetFileName(entry.File)}:{entry.LineNumber}: {entry.Line.Trim()}")
            .ToArray();

        violations.Should().BeEmpty();
    }

    private static string FindSourceRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "apps", "api", "Source");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("Could not find apps/api/Source from the test output directory.");
    }
}
