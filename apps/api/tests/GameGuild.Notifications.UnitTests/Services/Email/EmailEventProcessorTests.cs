using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.UnitTests.Infrastructure;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class EmailEventProcessorTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    public EmailEventProcessorTests()
    {
        SystemClock.SetProvider(new FrozenTimeProvider());
    }

    public void Dispose()
    {
        SystemClock.Reset();
    }

    [Fact]
    public async Task HardBounce_Suppresses_Address_And_Deadletters_Pending_Sends()
    {
        var (subject, context, _) = CreateSubject();
        var bounce = Event(EmailDeliveryEventType.Bounce, "Bounced@Example.com", bounceType: "Permanent");
        var sameAddressLowercased = Row("bounced@example.com");
        var sameAddressMixedCase = Row("Bounced@Example.COM");
        var otherAddress = Row("other@example.com");
        context.Notifications.AddRange(sameAddressLowercased, sameAddressMixedCase, otherAddress);
        await context.SaveChangesAsync();

        await subject.ProcessAsync(bounce);

        var suppression = context.EmailSuppressions.Local.Should().ContainSingle().Subject;
        suppression.EmailAddress.Should().Be("bounced@example.com");
        suppression.Reason.Should().Be(EmailSuppressionReason.HardBounce);
        suppression.BounceType.Should().Be("Permanent");
        suppression.SourceEventId.Should().Be(bounce.Id);
        suppression.IsActive.Should().BeTrue();

        sameAddressLowercased.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        sameAddressLowercased.LastError.Should().Be("suppressed: hard bounce");
        sameAddressMixedCase.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        otherAddress.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        otherAddress.LastError.Should().BeNull();
    }

    [Fact]
    public async Task UndeterminedBounce_Is_Treated_As_HardBounce()
    {
        var (subject, context, _) = CreateSubject();
        var bounce = Event(EmailDeliveryEventType.Bounce, "member@example.com", bounceType: "Undetermined");

        await subject.ProcessAsync(bounce);

        context.EmailSuppressions.Local.Should().ContainSingle()
            .Which.Reason.Should().Be(EmailSuppressionReason.HardBounce);
    }

    [Fact]
    public async Task Complaint_Suppresses_Address_And_Deadletters_Pending_Sends()
    {
        var (subject, context, _) = CreateSubject();
        var complaint = Event(EmailDeliveryEventType.Complaint, "complainer@example.com");
        var pending = Row("Complainer@Example.com");
        context.Notifications.Add(pending);
        await context.SaveChangesAsync();

        await subject.ProcessAsync(complaint);

        var suppression = context.EmailSuppressions.Local.Should().ContainSingle().Subject;
        suppression.Reason.Should().Be(EmailSuppressionReason.Complaint);
        suppression.BounceType.Should().BeNull();
        suppression.SourceEventId.Should().Be(complaint.Id);
        pending.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        pending.LastError.Should().Be("suppressed: complaint");
    }

    [Theory]
    [InlineData(EmailDeliveryEventType.Bounce, "Transient")]
    [InlineData(EmailDeliveryEventType.Bounce, "ContentRejected")]
    [InlineData(EmailDeliveryEventType.Bounce, null)]
    [InlineData(EmailDeliveryEventType.Send, null)]
    [InlineData(EmailDeliveryEventType.Delivery, null)]
    [InlineData(EmailDeliveryEventType.Open, null)]
    public async Task TransientBounces_And_NonBounceEvents_Do_Nothing(EmailDeliveryEventType eventType, string? bounceType)
    {
        var (subject, context, _) = CreateSubject();
        var pending = Row("member@example.com");
        context.Notifications.Add(pending);
        await context.SaveChangesAsync();

        await subject.ProcessAsync(Event(eventType, "member@example.com", bounceType: bounceType));

        context.EmailSuppressions.Local.Should().BeEmpty();
        pending.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        pending.LastError.Should().BeNull();
    }

    [Fact]
    public async Task ReleasedSuppression_Is_Resuppressed_On_New_Complaint()
    {
        var (subject, context, _) = CreateSubject();
        var released = EmailSuppression.Create("member@example.com", EmailSuppressionReason.HardBounce, "Permanent");
        context.EmailSuppressions.Add(released);
        await context.SaveChangesAsync();
        released.Release();
        await context.SaveChangesAsync();

        var complaint = Event(EmailDeliveryEventType.Complaint, "member@example.com");
        await subject.ProcessAsync(complaint);

        context.EmailSuppressions.Local.Should().ContainSingle();
        released.IsActive.Should().BeTrue();
        released.ReleasedAt.Should().BeNull();
        released.Reason.Should().Be(EmailSuppressionReason.Complaint);
        released.SourceEventId.Should().Be(complaint.Id);
        released.SuppressedAt.Should().Be(Now);
    }

    [Fact]
    public async Task ActiveSuppression_Is_Refreshed_Not_Duplicated()
    {
        var (subject, context, _) = CreateSubject();
        var active = EmailSuppression.Create("member@example.com", EmailSuppressionReason.HardBounce, "Permanent");
        context.EmailSuppressions.Add(active);
        await context.SaveChangesAsync();

        var newBounce = Event(EmailDeliveryEventType.Bounce, "member@example.com", bounceType: "Undetermined");
        await subject.ProcessAsync(newBounce);

        context.EmailSuppressions.Local.Should().ContainSingle();
        active.BounceType.Should().Be("Undetermined");
        active.SourceEventId.Should().Be(newBounce.Id);
        active.Reason.Should().Be(EmailSuppressionReason.HardBounce);
        active.SuppressedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Duplicate_Process_Call_Leaves_Single_Suppression_And_Is_Idempotent()
    {
        // Mirrors SNS redelivery: first request committed, second request re-runs on a fresh context.
        var dbName = InMemoryDatabaseName();
        var (first, context, _) = CreateSubject(dbName);
        var bounce = Event(EmailDeliveryEventType.Bounce, "member@example.com", bounceType: "Permanent");
        var pending = Row("member@example.com");
        context.Notifications.Add(pending);
        await context.SaveChangesAsync();

        await first.ProcessAsync(bounce);
        await context.SaveChangesAsync(); // the webhook controller's save

        var (second, redeliveryContext, _) = CreateSubject(dbName);
        await second.ProcessAsync(bounce);
        await redeliveryContext.SaveChangesAsync();

        var suppressions = await redeliveryContext.EmailSuppressions.ToListAsync();
        suppressions.Should().ContainSingle();
        suppressions[0].EmailAddress.Should().Be("member@example.com");
        suppressions[0].SourceEventId.Should().Be(bounce.Id);
        suppressions[0].IsActive.Should().BeTrue();

        var row = await redeliveryContext.Notifications.SingleAsync();
        row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        row.LastError.Should().Be("suppressed: hard bounce");
    }

    [Fact]
    public async Task Already_DeadLettered_Rows_Are_Untouched()
    {
        var (subject, context, _) = CreateSubject();
        var deadLettered = Row("member@example.com");
        deadLettered.MarkDeadLettered("delivery failed after 5 attempts");
        var pending = Row("member@example.com");
        context.Notifications.AddRange(deadLettered, pending);
        await context.SaveChangesAsync();

        await subject.ProcessAsync(Event(EmailDeliveryEventType.Bounce, "member@example.com", bounceType: "Permanent"));

        deadLettered.LastError.Should().Be("delivery failed after 5 attempts");
        pending.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        pending.LastError.Should().Be("suppressed: hard bounce");
    }

    [Fact]
    public async Task RecipientId_Only_Rows_Are_Skipped_By_The_Sweep()
    {
        var (subject, context, _) = CreateSubject();
        var recipientIdOnly = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.Email, "Title", "Body");
        context.Notifications.Add(recipientIdOnly);
        await context.SaveChangesAsync();

        await subject.ProcessAsync(Event(EmailDeliveryEventType.Bounce, "member@example.com", bounceType: "Permanent"));

        recipientIdOnly.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        recipientIdOnly.LastError.Should().BeNull();
    }

    private static EmailDeliveryEvent Event(EmailDeliveryEventType eventType, string recipientEmail, string? bounceType = null) =>
        EmailDeliveryEvent.Create("provider-message-id", recipientEmail, eventType, Now, "sns-message-id", bounceType: bounceType);

    private static Notification Row(string recipientEmail) =>
        Notification.Create(null, NotificationType.System, NotificationChannel.Email, "Title", "Body", recipientEmail: recipientEmail);

    private static string InMemoryDatabaseName() => Guid.NewGuid().ToString();

    private (EmailEventProcessor Subject, NotificationsTestDbContext Context, string DbName) CreateSubject(string? dbName = null)
    {
        dbName ??= InMemoryDatabaseName();
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var context = new NotificationsTestDbContext(options);
        return (new EmailEventProcessor(context, NullLogger<EmailEventProcessor>.Instance), context, dbName);
    }

    private sealed class FrozenTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }
}
