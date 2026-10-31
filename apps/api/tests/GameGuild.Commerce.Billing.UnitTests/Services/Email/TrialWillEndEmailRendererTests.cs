using System.Text.Json;
using FluentAssertions;
using GameGuild.Commerce.Billing.Services.Email.Renderers;
using GameGuild.Notifications;
using GameGuild.Notifications.Services.Email;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GameGuild.Commerce.Billing.UnitTests.Services.Email;

/// <summary>
///     Unit tests for the trial-ending reminder email renderer (issue #408).
/// </summary>
public class TrialWillEndEmailRendererTests
{
    private readonly TrialWillEndEmailRenderer _renderer = new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["TrialEmails:ConsoleBaseUrl"] = "https://console.gameguild.test"
        }).Build(),
        new NoFooterService());

    [Fact]
    public async Task RenderAsync_Should_Produce_The_Reminder_From_Metadata()
    {
        var notification = MakeNotification(new TrialWillEndEmailMetadata(
            TenantId: Guid.NewGuid(),
            SubscriptionId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            TrialEndUtc: new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc),
            Amount: 29.99m,
            Currency: "USD",
            RecipientEmail: "subscriber@example.test",
            RecipientName: "Subscriber"));

        var message = await _renderer.RenderAsync(notification, CancellationToken.None);

        message.Should().NotBeNull();
        message!.ToEmail.Should().Be("subscriber@example.test");
        message.ToName.Should().Be("Subscriber");
        message.Subject.Should().Contain("trial is ending");
        message.PlainTextContent.Should().Contain("2026-11-05");
        message.PlainTextContent.Should().Contain("29.99 USD");
        message.PlainTextContent.Should().Contain("https://console.gameguild.test/subscriptions/");
    }

    [Fact]
    public async Task RenderAsync_Should_Tolerate_A_Missing_Trial_End_Date()
    {
        var notification = MakeNotification(new TrialWillEndEmailMetadata(
            TenantId: null,
            SubscriptionId: Guid.NewGuid(),
            UserId: Guid.NewGuid(),
            TrialEndUtc: null,
            Amount: 10m,
            Currency: "EUR",
            RecipientEmail: "subscriber@example.test",
            RecipientName: null));

        var message = await _renderer.RenderAsync(notification, CancellationToken.None);

        message!.PlainTextContent.Should().Contain("ends on soon");
        message.ToName.Should().Be("subscriber@example.test");
    }

    [Fact]
    public async Task RenderAsync_Should_Throw_On_Missing_Or_Malformed_Metadata()
    {
        var noMetadata = MakeNotificationFromRawString(null);
        var act1 = () => _renderer.RenderAsync(noMetadata, CancellationToken.None);
        await act1.Should().ThrowAsync<InvalidOperationException>();

        var malformed = MakeNotificationFromRawString("not-json{");
        var act2 = () => _renderer.RenderAsync(malformed, CancellationToken.None);
        await act2.Should().ThrowAsync<InvalidOperationException>();
    }

    private static Notification MakeNotification(TrialWillEndEmailMetadata metadata) =>
        MakeNotificationFromRawString(
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static Notification MakeNotificationFromRawString(string? metadata)
    {
        var notification = (Notification)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(typeof(Notification));
        typeof(Notification).GetProperty(nameof(Notification.Metadata))!.SetValue(notification, metadata);
        typeof(Notification).GetProperty(nameof(Notification.RecipientEmail))!
            .SetValue(notification, "subscriber@example.test");
        return notification;
    }

    private sealed class NoFooterService : IEmailFooterService
    {
        public EmailFooter? Build(Notification notification) => null;
    }
}
