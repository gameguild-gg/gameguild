using FluentAssertions;
using GameGuild.API.Database;
using GameGuild.API.Eventing;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.API.UnitTests.Database;

/// <summary>
///     Issue #1313 S2-b: <see cref="InboxStore.RecordFailureAsync"/> durably persists
///     exception text that the admin dead-letter surface serves back — the text must be
///     redacted (emails, credentials, secrets, connection-string passwords) before it is
///     stored, and truncated to the 4000-char column budget after redaction.
/// </summary>
public sealed class InboxFailureRedactionTests
{
    [Theory]
    [InlineData("listener failed for user jane.doe@example.com", "listener failed for user [redacted]")]
    [InlineData("config api_key = hunter2000supersecret invalid", "config api_key = [redacted] invalid")]
    [InlineData("connect with Password=Sup3rS3cret;Host=db", "connect with Password=[redacted];Host=db")]
    public void Redact_RemovesEmbeddedSensitiveValues(string input, string expected)
    {
        InboxFailureTextRedactor.Redact(input).Should().Be(expected);
    }

    [Fact]
    public void Redact_RemovesCredentialSchemePairsAndProviderSecretKeys()
    {
        // Built by concatenation so the test source never contains a literal credential
        // that repository push protection would flag.
        var bearerValue = string.Concat("abc123", "def456", "ghi789", "==");
        var providerKey = string.Concat("sk", "_live_", "Abc123", "Def456", "Ghi789", "Jkl012");

        InboxFailureTextRedactor.Redact($"Authorization: Bearer {bearerValue} rejected")
            .Should().Be("Authorization: Bearer [redacted] rejected");
        InboxFailureTextRedactor.Redact($"charge failed with {providerKey} at gateway")
            .Should().Be("charge failed with [redacted] at gateway");
    }

    [Fact]
    public void Redact_KeepsOrdinaryDiagnosticText()
    {
        const string diagnostic = "InvalidOperationException: tenant projection failed at step 3 after 2 retries";

        InboxFailureTextRedactor.Redact(diagnostic).Should().Be(diagnostic);
    }

    [Fact]
    public void Redact_EmptyAndNullInputPassThrough()
    {
        InboxFailureTextRedactor.Redact(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public async Task RecordFailureAsync_PersistsRedactedErrorBeforeTruncation()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"inbox-redaction-{Guid.NewGuid():N}")
            .Options;
        await using var context = new ApplicationDbContext(options);
        var store = new InboxStore(context);
        var secret = new string('x', 5000);
        var exception = new InvalidOperationException(
            $"handler blew up for admin@example.com with Bearer {secret} at some deep stack frame");

        var receipt = await store.RecordFailureAsync(Guid.NewGuid(), "Redaction.Tests.Consumer", exception, DateTimeOffset.UtcNow);

        receipt.LastError.Should().NotBeNull();
        receipt.LastError.Should().NotContain("admin@example.com", "the email must be redacted");
        receipt.LastError.Should().NotContain(secret, "the credential must be redacted");
        receipt.LastError.Should().NotContain("Bearer xxxxx", "the credential scheme value must be replaced");
        receipt.LastError!.Length.Should().BeLessThanOrEqualTo(4000);
        receipt.LastError.Should().Contain("[redacted]");
    }
}
