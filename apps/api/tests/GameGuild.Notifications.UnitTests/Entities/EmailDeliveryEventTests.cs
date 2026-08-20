using GameGuild.Notifications.UnitTests.Infrastructure;

namespace GameGuild.Notifications.UnitTests.Entities;

public class EmailDeliveryEventTests
{
    [Fact]
    public void Create_Should_Initialize_All_Fields()
    {
        var occurredAt = SystemClock.UtcNow.AddMinutes(-5);

        var evt = EmailDeliveryEvent.Create(
            "ses-message-id-123",
            "Bounced@Example.COM",
            EmailDeliveryEventType.Bounce,
            occurredAt,
            "sns-msg-id-1",
            "Permanent",
            "5.1.1",
            "{\"eventType\":\"Bounce\"}");

        evt.Id.Should().NotBe(Guid.Empty);
        evt.ProviderMessageId.Should().Be("ses-message-id-123");
        evt.RecipientEmail.Should().Be("bounced@example.com");
        evt.EventType.Should().Be(EmailDeliveryEventType.Bounce);
        evt.OccurredAt.Should().Be(occurredAt);
        evt.BounceType.Should().Be("Permanent");
        evt.DiagnosticCode.Should().Be("5.1.1");
        evt.SnsMessageId.Should().Be("sns-msg-id-1");
        evt.Payload.Should().Be("{\"eventType\":\"Bounce\"}");
    }

    [Fact]
    public void Create_Should_Normalize_RecipientEmail()
    {
        var evt = EmailDeliveryEvent.Create(
            "mid",
            "  Mixed.Case@Example.TEST  ",
            EmailDeliveryEventType.Send,
            SystemClock.UtcNow,
            "sns-1");

        evt.RecipientEmail.Should().Be("mixed.case@example.test");
    }

    [Fact]
    public async Task Event_Should_Roundtrip_Through_The_Context()
    {
        var databaseName = Guid.NewGuid().ToString();
        using var context = CreateContext(databaseName);
        var evt = EmailDeliveryEvent.Create(
            "mid-rt",
            "RoundTrip@Example.com",
            EmailDeliveryEventType.Delivery,
            SystemClock.UtcNow,
            "sns-rt");

        context.EmailDeliveryEvents.Add(evt);
        await context.SaveChangesAsync();

        using var readContext = CreateContext(databaseName);
        var stored = await readContext.EmailDeliveryEvents.SingleAsync(e => e.SnsMessageId == "sns-rt");

        stored.ProviderMessageId.Should().Be("mid-rt");
        stored.RecipientEmail.Should().Be("roundtrip@example.com");
        stored.EventType.Should().Be(EmailDeliveryEventType.Delivery);
        stored.Id.Should().Be(evt.Id);
    }

    private static NotificationsTestDbContext CreateContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        return new NotificationsTestDbContext(options);
    }
}
