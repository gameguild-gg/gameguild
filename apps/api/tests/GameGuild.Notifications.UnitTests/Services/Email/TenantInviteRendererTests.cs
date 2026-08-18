using System.Text.Json;
using FluentAssertions;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.Services.Email.Renderers;
using Moq;
using Xunit;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class TenantInviteRendererTests
{
    private static IEmailFooterService CreateFooterService() =>
        Mock.Of<IEmailFooterService>(s => s.Build(It.IsAny<Notification>()) == (EmailFooter?)null);

    private static Notification CreateNotification(string metadata, string recipientEmail) =>
        Notification.Create(
            null,
            NotificationType.TenantInvite,
            NotificationChannel.Email,
            "Title",
            "Message",
            metadata: metadata,
            recipientEmail: recipientEmail);

    private static string Metadata(object value) => JsonSerializer.Serialize(value);

    [Fact]
    public async Task NewInvite_Renders_Verbatim_Copy_From_Metadata()
    {
        var renderer = new TenantInviteRenderer(CreateFooterService());
        var notification = CreateNotification(
            Metadata(new
            {
                inviteeName = "Learner One",
                invitedByEmail = "admin@game-guild.com",
                tenantName = "GameGuild Studio",
                role = "Moderator",
                reviewUrl = "https://app.example.com/sign-in?callbackUrl=%2Faccount%2Finvitations",
                activationUrl = "https://app.example.com/forgot-password?email=learner%40example.com",
                resend = false
            }),
            "learner@example.com");

        var message = await renderer.RenderAsync(notification);

        message.Should().NotBeNull();
        message!.Subject.Should().Be("You were invited to GameGuild Studio on GameGuild");
        message.ToName.Should().Be("Learner One");
        message.PlainTextContent.Should().Be(
            "Hi Learner One,\n\nadmin@game-guild.com invited you to join GameGuild Studio on GameGuild as Moderator.\n\n" +
            "Review and accept your access:\nhttps://app.example.com/sign-in?callbackUrl=%2Faccount%2Finvitations\n\n" +
            "If this is your first GameGuild invitation, set your password first:\n" +
            "https://app.example.com/forgot-password?email=learner%40example.com\n\n" +
            "If you were not expecting this invite, you can ignore this email.");
        message.HtmlContent.Should().Contain("admin@game-guild.com invited you to join <strong>GameGuild Studio</strong>");
        message.HtmlContent.Should().Contain("as <strong>Moderator</strong>");
        message.HtmlContent.Should().NotContain("resent");
        message.HtmlContent.Should().NotContain("/unsubscribe");
    }

    [Fact]
    public async Task Resend_Renders_Reminder_Copy_From_Metadata()
    {
        var renderer = new TenantInviteRenderer(CreateFooterService());
        var notification = CreateNotification(
            Metadata(new
            {
                inviteeName = "Learner One",
                invitedByEmail = "admin@game-guild.com",
                tenantName = "GameGuild Studio",
                role = "Member",
                reviewUrl = "https://app.example.com/sign-in?callbackUrl=%2Finvitations",
                activationUrl = "https://app.example.com/forgot-password?email=learner%40example.com",
                resend = true
            }),
            "learner@example.com");

        var message = await renderer.RenderAsync(notification);

        message.Should().NotBeNull();
        message!.Subject.Should().Be("Reminder: you were invited to GameGuild Studio on GameGuild");
        message.PlainTextContent.Should().Contain(
            "admin@game-guild.com resent your invitation to join GameGuild Studio on GameGuild as Member.");
        message.HtmlContent.Should().Contain("resent your invitation to join <strong>GameGuild Studio</strong>");
        message.HtmlContent.Should().NotContain("/unsubscribe");
    }

    [Fact]
    public async Task Renderer_Falls_Back_To_RecipientEmail_And_Administrator_When_Metadata_Missing()
    {
        var renderer = new TenantInviteRenderer(CreateFooterService());
        var notification = CreateNotification(
            Metadata(new { tenantName = "GameGuild Studio", role = "Member", resend = false }),
            "learner@example.com");

        var message = await renderer.RenderAsync(notification);

        message.Should().NotBeNull();
        message!.ToName.Should().Be("learner@example.com");
        message.PlainTextContent.Should().Contain("A GameGuild administrator invited you");
    }
}
