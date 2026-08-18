using GameGuild.Email;
using GameGuild.Identity.Users;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.UnitTests.Services.Email;

// Legacy boolean preference gate is the interim seam the dispatcher codes against (held for rework lane).
#pragma warning disable CS0618

public sealed class EmailDispatcherServiceTests : IDisposable
{
    private static readonly DateTime Now = new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    public EmailDispatcherServiceTests()
    {
        SystemClock.SetProvider(new FrozenTimeProvider());
    }

    public void Dispose()
    {
        SystemClock.Reset();
    }

    private sealed class FrozenTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(Now, TimeSpan.Zero);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<EmailMessage> Sent { get; } = [];

        public bool ThrowOnSend { get; set; }

        public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("smtp unavailable");
            }

            Sent.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class StubRenderer(NotificationType type, Func<Notification, EmailMessage?>? render = null) : IEmailRenderer
    {
        private readonly Func<Notification, EmailMessage?> _render = render ?? (_ => new EmailMessage(
            "resolved-by-dispatcher@example.com", "Rendered subject", "plain body", "<p>html body</p>"));

        public NotificationType Type { get; } = type;

        public int RenderCount { get; private set; }

        public Task<EmailMessage?> RenderAsync(Notification notification, CancellationToken cancellationToken = default)
        {
            RenderCount++;
            return Task.FromResult(_render(notification));
        }
    }

    [Fact]
    public async Task Sweep_Sends_Pending_Row_And_Marks_Sent()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Welcome", "Body",
            scheduledAt: Now.AddMinutes(-5), recipientEmail: "member@example.com");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        sender.Sent.Should().ContainSingle();
        sender.Sent[0].ToEmail.Should().Be("member@example.com");
        sender.Sent[0].Subject.Should().Be("Rendered subject");
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        notification.IsSent.Should().BeTrue();
        notification.SentAt.Should().NotBeNull();
        (await context.Notifications.SingleAsync(n => n.Id == notification.Id))
            .DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task Sweep_Retries_With_Backoff_When_Sender_Throws()
    {
        var (subject, context, sender, options) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        sender.ThrowOnSend = true;
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "member@example.com");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(0);
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        notification.AttemptCount.Should().Be(1);
        notification.NextAttemptAt.Should().Be(Now.Add(options.BackoffSchedule[0]));
        notification.LastError.Should().Contain("smtp unavailable");
    }

    [Fact]
    public async Task Sweep_Deadletters_At_MaxAttempts()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        sender.ThrowOnSend = true;
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "member@example.com");
        for (var i = 0; i < 4; i++)
        {
            notification.MarkDeliveryAttemptFailed($"failure {i + 1}", Now);
        }
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        await subject.SweepOnceAsync();

        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        notification.AttemptCount.Should().Be(4);
        notification.NextAttemptAt.Should().BeNull();
        notification.LastError.Should().Contain("after 5 attempts");
    }

    [Fact]
    public async Task Sweep_Deadletters_Stale_Transactional_Row_Without_Rendering()
    {
        var renderer = new StubRenderer(NotificationType.EmailVerification);
        var (subject, context, sender, _) = CreateSubject(renderers: [renderer]);
        var notification = Notification.Create(
            null, NotificationType.EmailVerification, NotificationChannel.Email, "Verify", "Body",
            recipientEmail: "member@example.com");
        notification.CreatedAt = Now.AddHours(-25);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        renderer.RenderCount.Should().Be(0);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        notification.LastError.Should().Contain("stale");
    }

    [Fact]
    public async Task Sweep_Skips_Row_Scheduled_In_The_Future()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            scheduledAt: Now.AddHours(1), recipientEmail: "member@example.com");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(0);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
    }

    [Fact]
    public async Task Sweep_Never_Touches_HeldForDigest_Rows()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "member@example.com");
        context.Entry(notification).Property(n => n.DeliveryStatus).CurrentValue =
            NotificationDeliveryStatus.HeldForDigest;
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(0);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.HeldForDigest);
    }

    [Fact]
    public async Task Sweep_Reclaims_Row_Stuck_In_Sending()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "member@example.com");
        notification.ClaimForSending();
        notification.UpdatedAt = Now.AddMinutes(-15);
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        sender.Sent.Should().ContainSingle();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task Sweep_Holds_Row_When_Preferences_Block_Delivery()
    {
        var renderer = new StubRenderer(NotificationType.System);
        var (subject, context, sender, _) = CreateSubject(
            renderers: [renderer],
            shouldSend: false);
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.Email, "Title", "Body");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        renderer.RenderCount.Should().Be(0);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        notification.NextAttemptAt.Should().Be(Now.AddMinutes(30));
        notification.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_Deadletters_Row_With_No_Renderer()
    {
        var (subject, context, sender, _) = CreateSubject(renderers: [new StubRenderer(NotificationType.System)]);
        var notification = Notification.Create(
            null, NotificationType.MonthlyStatement, NotificationChannel.Email, "Statement", "Body",
            recipientEmail: "member@example.com");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
        notification.LastError.Should().Contain("MonthlyStatement");
    }

    [Fact]
    public async Task Sweep_Continues_Past_Single_Row_Failure()
    {
        var renderer = new StubRenderer(NotificationType.System, n => n.Title == "poison"
            ? throw new InvalidOperationException("render exploded")
            : new EmailMessage("placeholder@example.com", "Rendered subject", "plain body", "<p>html body</p>"));
        var (subject, context, sender, options) = CreateSubject(renderers: [renderer]);
        var first = Notification.Create(null, NotificationType.System, NotificationChannel.Email, "ok-1", "Body", recipientEmail: "a@example.com");
        var poison = Notification.Create(null, NotificationType.System, NotificationChannel.Email, "poison", "Body", recipientEmail: "b@example.com");
        var last = Notification.Create(null, NotificationType.System, NotificationChannel.Email, "ok-2", "Body", recipientEmail: "c@example.com");
        context.Notifications.AddRange(first, poison, last);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(2);
        sender.Sent.Should().HaveCount(2);
        first.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        last.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
        poison.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending);
        poison.AttemptCount.Should().Be(1);
        poison.NextAttemptAt.Should().Be(Now.Add(options.BackoffSchedule[0]));
    }

    [Fact]
    public async Task Sweep_Marks_Row_Sent_When_Renderer_Returns_Null()
    {
        var renderer = new StubRenderer(NotificationType.System, _ => null);
        var (subject, context, sender, _) = CreateSubject(renderers: [renderer]);
        var notification = Notification.Create(
            null, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "member@example.com");
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();

        var processed = await subject.SweepOnceAsync();

        processed.Should().Be(1);
        sender.Sent.Should().BeEmpty();
        notification.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task RecipientResolver_Prefers_RecipientEmail_Column()
    {
        var userRepository = new Mock<IUserRepository>();
        var resolver = new RecipientEmailResolver(userRepository.Object);
        var notification = Notification.Create(
            Guid.NewGuid(), NotificationType.System, NotificationChannel.Email, "Title", "Body",
            recipientEmail: "column@example.com");

        var email = await resolver.ResolveAsync(notification);

        email.Should().Be("column@example.com");
        userRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RecipientResolver_Falls_Back_To_User_Lookup_Then_Fails()
    {
        var user = new User { Email = "looked-up@example.com" };
        var userRepository = new Mock<IUserRepository>();
        userRepository.Setup(r => r.GetByIdAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var resolver = new RecipientEmailResolver(userRepository.Object);
        var withUser = Notification.Create(user.Id, NotificationType.System, NotificationChannel.Email, "Title", "Body");
        var ghost = Notification.Create(Guid.NewGuid(), NotificationType.System, NotificationChannel.Email, "Title", "Body");

        (await resolver.ResolveAsync(withUser)).Should().Be("looked-up@example.com");
        (await resolver.ResolveAsync(ghost)).Should().BeNull();
    }

    private (EmailDispatcherService Subject, NotificationsTestDbContext Context, CapturingEmailSender Sender, EmailDispatcherOptions Options) CreateSubject(
        IEmailRenderer[] renderers,
        bool shouldSend = true)
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new NotificationsTestDbContext(options);
        var sender = new CapturingEmailSender();
        var prefs = new Mock<INotificationPreferenceService>();
        prefs.Setup(p => p.ShouldSendNotificationAsync(
                It.IsAny<Guid>(), It.IsAny<NotificationType>(), It.IsAny<NotificationChannel>(),
                It.IsAny<NotificationPriority>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(shouldSend);
        var dispatcherOptions = Options.Create(new EmailDispatcherOptions());
        var subject = new EmailDispatcherService(
            context,
            new EmailRendererRegistry(renderers),
            new AlwaysResolvesResolver(),
            prefs.Object,
            sender,
            dispatcherOptions,
            NullLogger<EmailDispatcherService>.Instance);
        return (subject, context, sender, dispatcherOptions.Value);
    }

    private sealed class AlwaysResolvesResolver : IRecipientEmailResolver
    {
        public Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default)
            => Task.FromResult(notification.RecipientEmail ?? "resolved@example.com");
    }
}
