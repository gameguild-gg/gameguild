using GameGuild.Email;
using GameGuild.Notifications.Services.Email;
using GameGuild.Notifications.UnitTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace GameGuild.Notifications.UnitTests.Services.Email;

public sealed class DigestDispatcherServiceTests : IDisposable
{
    // Thursday, 2026-01-15 08:30 UTC. ISO weeks: Mon 2026-01-05 = week 2 (even), Mon 2026-01-12 = week 3 (odd).
    private static readonly DateTime Now = new(2026, 1, 15, 8, 30, 0, DateTimeKind.Utc);

    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");

    public DigestDispatcherServiceTests()
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

        public Task<string?> SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
        {
            if (ThrowOnSend)
            {
                throw new InvalidOperationException("smtp unavailable");
            }

            Sent.Add(message);
            return Task.FromResult<string?>("test-message-id");
        }
    }

    [Fact]
    public void ComputeMostRecentFire_Daily_AfterFireTime_ReturnsToday()
    {
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Daily, Now, TimeZoneInfo.Utc, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_Daily_BeforeFireTime_RollsBackToYesterday()
    {
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Daily, Now.AddHours(-2), TimeZoneInfo.Utc, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 14, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_Daily_TimezoneOvernightBoundary_UsesUserLocalDay()
    {
        // 07:00 UTC = 02:00 in New York (EST, UTC-5): today's 08:00 local has not fired yet,
        // so the most recent fire is yesterday 08:00 EST = yesterday 13:00 UTC.
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Daily, new DateTime(2026, 1, 15, 7, 0, 0, DateTimeKind.Utc), NewYork, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 14, 13, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_Daily_InvalidTimezone_FallsBackToUtc()
    {
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Daily, Now, DigestDispatcherService.ResolveTimeZone("Not/AZone"), new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_Weekly_UsesMostRecentMonday()
    {
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Weekly, Now, TimeZoneInfo.Utc, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 12, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_Weekly_BeforeMondayFire_RollsToPreviousMonday()
    {
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.Weekly, new DateTime(2026, 1, 12, 7, 59, 0, DateTimeKind.Utc), TimeZoneInfo.Utc, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void ComputeMostRecentFire_BiWeekly_AnchorsOnEvenIsoWeekMonday()
    {
        // Most recent Monday is 2026-01-12 (ISO week 3, odd) — the even-parity anchor walks back to 2026-01-05 (week 2).
        var fire = DigestDispatcherService.ComputeMostRecentFireUtc(
            DigestFrequency.BiWeekly, Now, TimeZoneInfo.Utc, new TimeOnly(8, 0));

        fire.Should().Be(new DateTime(2026, 1, 5, 8, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Sweep_Bundles_Rows_Into_Single_Email_Grouped_By_Type_And_Marks_All_Sent()
    {
        var (subject, context, sender, _) = CreateSubject();
        var userId = Guid.NewGuid();
        SeedDigestPreferences(context, userId, DigestFrequency.Daily);
        var enrollment1 = SeedHeldRow(context, userId, NotificationType.CourseEnrollment, "Enrollment 1", actionUrl: "https://app.example.com/courses/1");
        var enrollment2 = SeedHeldRow(context, userId, NotificationType.CourseEnrollment, "Enrollment 2");
        var achievement = SeedHeldRow(context, userId, NotificationType.AchievementUnlocked, "Achievement!");
        await context.SaveChangesAsync();

        var sent = await subject.SweepOnceAsync();

        sent.Should().Be(1);
        sender.Sent.Should().ContainSingle();
        var digest = sender.Sent[0];
        digest.ToEmail.Should().Be("digest-user@example.com");
        digest.Subject.Should().Be("Your GameGuild digest");
        digest.PlainTextContent.Should().Contain("CourseEnrollment:").And.Contain("AchievementUnlocked:");
        digest.PlainTextContent.Should().Contain("Enrollment 1").And.Contain("Enrollment 2").And.Contain("Achievement!");
        digest.PlainTextContent.Should().Contain("https://app.example.com/courses/1").And.Contain("2026-01-15 06:30 UTC");
        digest.HtmlContent.Should().Contain("https://app.example.com/courses/1");

        foreach (var row in new[] { enrollment1, enrollment2, achievement })
        {
            row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
            row.IsSent.Should().BeTrue();
            row.SentAt.Should().NotBeNull();
            // Digest bundles have no single provider message id — the returned id is discarded by design.
            row.ProviderMessageId.Should().BeNull();
        }

        // Claim exclusivity / no re-digest: a second sequential sweep finds nothing left to claim.
        (await subject.SweepOnceAsync()).Should().Be(0);
        sender.Sent.Should().HaveCount(1);
    }

    [Fact]
    public async Task Sweep_After_Success_Row_Arriving_After_Fire_Time_Waits_For_Next_Window()
    {
        var (subject, context, sender, _) = CreateSubject();
        var userId = Guid.NewGuid();
        SeedDigestPreferences(context, userId, DigestFrequency.Daily);
        var old = SeedHeldRow(context, userId, NotificationType.System, "Old");
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(1);

        var fresh = Notification.Create(userId, NotificationType.System, NotificationChannel.Email, "Arrived after 08:00 fire", "Body");
        fresh.CreatedAt = Now; // after today's 08:00 fire — belongs to the next window
        fresh.MarkHeldForDigest();
        context.Notifications.Add(fresh);
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(0);
        sender.Sent.Should().HaveCount(1);
        fresh.DeliveryStatus.Should().Be(NotificationDeliveryStatus.HeldForDigest);
    }

    [Fact]
    public async Task Sweep_Sender_Throws_Rows_Returned_To_HeldForDigest_And_Retry_Succeeds()
    {
        var (subject, context, sender, _) = CreateSubject();
        var userId = Guid.NewGuid();
        SeedDigestPreferences(context, userId, DigestFrequency.Daily);
        var row = SeedHeldRow(context, userId, NotificationType.System, "Title");
        await context.SaveChangesAsync();
        sender.ThrowOnSend = true;

        (await subject.SweepOnceAsync()).Should().Be(0);

        sender.Sent.Should().BeEmpty();
        row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.HeldForDigest);
        row.IsSent.Should().BeFalse();
        row.AttemptCount.Should().Be(0); // digest failures are bundle-level; retry attempts are not consumed

        sender.ThrowOnSend = false;
        (await subject.SweepOnceAsync()).Should().Be(1);
        row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Sent);
    }

    [Fact]
    public async Task Sweep_Deadletters_Claimed_Rows_For_Suppressed_User_Without_Sending()
    {
        var (subject, context, sender, _) = CreateSubject();
        var userId = Guid.NewGuid();
        SeedDigestPreferences(context, userId, DigestFrequency.Daily);
        var row1 = SeedHeldRow(context, userId, NotificationType.System, "One");
        var row2 = SeedHeldRow(context, userId, NotificationType.System, "Two");
        // Resolver returns "digest-user@example.com"; mixed-case seed proves both sides normalize.
        context.EmailSuppressions.Add(EmailSuppression.Create(
            "Digest-User@Example.com", EmailSuppressionReason.Complaint));
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(0);

        sender.Sent.Should().BeEmpty();
        foreach (var row in new[] { row1, row2 })
        {
            row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.DeadLettered);
            row.LastError.Should().Be("suppressed");
            row.AttemptCount.Should().Be(0);
            row.IsSent.Should().BeFalse();
        }

        // Suppression is permanent for the digest: a later sweep finds no HeldForDigest rows to claim.
        (await subject.SweepOnceAsync()).Should().Be(0);
        sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Sweep_Skips_User_With_No_Held_Rows()
    {
        var (subject, context, sender, _) = CreateSubject();
        SeedDigestPreferences(context, Guid.NewGuid(), DigestFrequency.Daily);
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(0);
        sender.Sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Sweep_Processes_Users_Independently()
    {
        var (subject, context, sender, _) = CreateSubject();
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        SeedDigestPreferences(context, user1, DigestFrequency.Daily);
        SeedDigestPreferences(context, user2, DigestFrequency.Daily);
        SeedHeldRow(context, user1, NotificationType.System, "For user 1");
        SeedHeldRow(context, user2, NotificationType.System, "For user 2");
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(2);

        sender.Sent.Should().HaveCount(2);
        sender.Sent.Should().OnlyContain(m => m.PlainTextContent.Contains("For user"));
    }

    [Fact]
    public async Task Sweep_Requeues_Orphaned_Rows_When_Digest_Disabled()
    {
        var (subject, context, sender, _) = CreateSubject();
        var userId = Guid.NewGuid();
        SeedDigestPreferences(context, userId, digestFrequency: null);
        var row = SeedHeldRow(context, userId, NotificationType.System, "Orphaned");
        await context.SaveChangesAsync();

        (await subject.SweepOnceAsync()).Should().Be(0);

        sender.Sent.Should().BeEmpty();
        row.DeliveryStatus.Should().Be(NotificationDeliveryStatus.Pending); // email dispatcher picks it up individually
    }

    [Fact]
    public void DigestRenderer_Manage_Link_Only_No_PerType_Unsubscribe()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = "https://app.example.com" })
            .Build();
        var renderer = new DigestRenderer(configuration);
        var userId = Guid.NewGuid();
        var row = Notification.Create(userId, NotificationType.System, NotificationChannel.Email, "Title", "Body",
            actionUrl: "https://app.example.com/go");

        var message = renderer.Render("user@example.com", [row]);

        message.Subject.Should().Be("Your GameGuild digest");
        message.PlainTextContent.Should().Contain("https://app.example.com/workspace/settings/notifications");
        message.PlainTextContent.Should().NotContain("token=");
        message.HtmlContent.Should().Contain("/workspace/settings/notifications");
        message.HtmlContent.Should().NotContain("token=");
    }

    [Fact]
    public async Task Transactional_Types_Bypass_Digest_Routing_By_Construction()
    {
        // A type can only reach HeldForDigest through DecideDeliveryAsync's Digest branch, which the
        // transactional bypass short-circuits — so these types can never be bundled into a digest.
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var context = new NotificationsTestDbContext(options);
        var userId = Guid.NewGuid();
        var preferences = NotificationPreference.CreateDefault(userId);
        preferences.SetEmailDigestFrequency(DigestFrequency.Daily);
        context.Set<NotificationPreference>().Add(preferences);
        await context.SaveChangesAsync();

        var preferenceService = new NotificationPreferenceService(context);

        foreach (var type in NotificationCategories.Transactional)
        {
            var decision = await preferenceService.DecideDeliveryAsync(userId, type, NotificationChannel.Email, NotificationPriority.Normal);
            decision.Action.Should().Be(NotificationDeliveryAction.Send, $"transactional type {type} must bypass digest routing");
        }
    }

    private (DigestDispatcherService Subject, NotificationsTestDbContext Context, CapturingEmailSender Sender, DigestDispatcherOptions Options) CreateSubject()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new NotificationsTestDbContext(options);
        var sender = new CapturingEmailSender();
        var dispatcherOptions = Options.Create(new DigestDispatcherOptions());
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["App:BaseUrl"] = "https://app.example.com" })
            .Build();
        var subject = new DigestDispatcherService(
            context,
            new AlwaysResolvesResolver(),
            sender,
            new DigestRenderer(configuration),
            dispatcherOptions,
            NullLogger<DigestDispatcherService>.Instance);
        return (subject, context, sender, dispatcherOptions.Value);
    }

    private static void SeedDigestPreferences(NotificationsTestDbContext context, Guid userId, DigestFrequency? digestFrequency)
    {
        var preferences = NotificationPreference.CreateDefault(userId);
        preferences.SetEmailDigestFrequency(digestFrequency);
        context.Set<NotificationPreference>().Add(preferences);
    }

    private static Notification SeedHeldRow(
        NotificationsTestDbContext context, Guid userId, NotificationType type, string title, string? actionUrl = null)
    {
        var notification = Notification.Create(userId, type, NotificationChannel.Email, title, "Body", actionUrl: actionUrl);
        notification.CreatedAt = Now.AddHours(-2); // inside the elapsed daily window (fire was 08:00 UTC)
        notification.MarkHeldForDigest();
        context.Notifications.Add(notification);
        return notification;
    }

    private sealed class AlwaysResolvesResolver : IRecipientEmailResolver
    {
        public Task<string?> ResolveAsync(Notification notification, CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(notification.RecipientEmail ?? "digest-user@example.com");
    }
}
