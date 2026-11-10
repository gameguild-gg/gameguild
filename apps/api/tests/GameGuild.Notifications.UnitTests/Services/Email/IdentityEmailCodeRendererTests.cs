using System.Text.Json;
using FluentAssertions;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.Services.Email.Renderers;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class IdentityEmailCodeRendererTests
{
    private static readonly Guid UserId = Guid.NewGuid();
    private static readonly NotificationMetadataProtector MetadataProtector = new(new EphemeralDataProtectionProvider());

    private static IEmailFooterService CreateFooterService() =>
        new EmailFooterService(
            new UnsubscribeTokenService(new EphemeralDataProtectionProvider()),
            new ConfigurationBuilder().AddInMemoryCollection(
                new Dictionary<string, string?> { ["App:BaseUrl"] = "https://app.example.com" }).Build());

    private static Notification CreateNotification(string metadata) =>
        Notification.Create(
            UserId,
            NotificationType.EmailCode,
            NotificationChannel.Email,
            "Title",
            "Message",
            metadata: metadata);

    private static string Metadata(object value) => JsonSerializer.Serialize(value);

    [Fact]
    public void Type_Is_EmailCode()
    {
        var renderer = new EmailCodeRenderer(CreateFooterService(), MetadataProtector);

        renderer.Type.Should().Be(NotificationType.EmailCode);
    }

    [Fact]
    public async Task EmailCode_Renders_Code_From_Protected_Metadata()
    {
        var renderer = new EmailCodeRenderer(CreateFooterService(), MetadataProtector);
        var notification = MetadataProtectorRoundTrip(
            Metadata(new { code = "284917", email = "user@example.com", userName = "Alice" }));

        var message = await renderer.RenderAsync(notification);

        message.Should().NotBeNull();
        message!.Subject.Should().Be("Your GameGuild sign-in code");
        message.PlainTextContent.Should().Contain("284917");
        message.PlainTextContent.Should().Contain("expires in 10 minutes");
        message.HtmlContent.Should().Contain("284917");
        message.HtmlContent.Should().Contain("Hi Alice,");
    }

    [Fact]
    public async Task EmailCode_Is_Transactional_And_Has_No_Footer()
    {
        var renderer = new EmailCodeRenderer(CreateFooterService(), MetadataProtector);
        var notification = MetadataProtectorRoundTrip(
            Metadata(new { code = "284917", email = "user@example.com", userName = "Alice" }));

        var message = await renderer.RenderAsync(notification);

        message!.PlainTextContent.Should().NotContain("/unsubscribe");
        message.HtmlContent.Should().NotContain("/unsubscribe");
    }

    [Fact]
    public async Task EmailCode_Throws_When_Credential_Metadata_Is_Missing()
    {
        var renderer = new EmailCodeRenderer(CreateFooterService(), MetadataProtector);
        var notification = MetadataProtectorRoundTrip(
            Metadata(new { email = "user@example.com" }));

        var act = () => renderer.RenderAsync(notification);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task RiskCodeUsesTheProtectedAbsoluteExpiryInsteadOfTheLegacyTenMinuteLifetime()
    {
        var expiry = new DateTimeOffset(2026, 11, 5, 12, 5, 0, TimeSpan.Zero);
        var notification = MetadataProtectorRoundTrip(Metadata(new
        {
            code = "87654321", email = "synthetic@example.invalid", purpose = "orchestrated-risk", expiresAt = expiry
        }));
        var message = await new EmailCodeRenderer(CreateFooterService(), MetadataProtector).RenderAsync(notification);
        message.Should().NotBeNull();
        message!.Subject.Should().Be("Verify your sign-in");
        message.PlainTextContent.Should().Contain("87654321").And.Contain("2026-11-05 12:05:00 UTC").And.NotContain("10 minutes");
        message.HtmlContent.Should().Contain("87654321").And.Contain("2026-11-05 12:05:00 UTC");
        message.PlainTextContent.Should().NotContain("/unsubscribe");
    }

    [Theory]
    [InlineData("orchestrated-risk", null)]
    [InlineData("unknown-purpose", "2026-11-05T12:05:00Z")]
    [InlineData("orchestrated-risk", "2026-11-05T12:05:00+01:00")]
    public async Task RiskPurposeRequiresAnExplicitUtcExpiry(string purpose, string? expiresAt)
    {
        var notification = MetadataProtectorRoundTrip(Metadata(new
        {
            code = "87654321", email = "synthetic@example.invalid", purpose, expiresAt
        }));
        var renderer = new EmailCodeRenderer(CreateFooterService(), MetadataProtector);
        await Assert.ThrowsAsync<InvalidOperationException>(() => renderer.RenderAsync(notification));
    }

    private static Notification MetadataProtectorRoundTrip(string metadata)
    {
        var notification = CreateNotification(metadata);
        MetadataProtector.ProtectForStorage(notification);
        return notification;
    }
}
