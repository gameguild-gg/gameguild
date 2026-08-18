using GameGuild.Notifications.UnitTests.Infrastructure;

namespace GameGuild.Notifications.UnitTests.Services;

public class NotificationPreferenceServiceTests
{
    [Fact]
    public async Task GetPreferencesAsync_Should_Return_Existing_Preferences()
    {
        using var context = CreateContext();
        var existing = NotificationPreference.CreateDefault(Guid.NewGuid());
        context.NotificationPreferences.Add(existing);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.GetPreferencesAsync(existing.UserId);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(existing.UserId);
        context.NotificationPreferences.Should().ContainSingle();
    }

    [Fact]
    public async Task GetPreferencesAsync_Should_Create_Default_When_Missing()
    {
        using var context = CreateContext();
        var userId = Guid.NewGuid();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.GetPreferencesAsync(userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        context.NotificationPreferences.Should().ContainSingle();
    }

    [Fact]
    public async Task UpdatePreferencesAsync_Should_Merge_New_Values_With_Existing_State()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.UpdatePreferencesAsync(
            preference.UserId,
            emailEnabled: false,
            smsEnabled: true,
            marketingEnabled: false,
            achievementsEnabled: false);

        result.IsSuccess.Should().BeTrue();
        result.Value.EmailEnabled.Should().BeFalse();
        result.Value.PushEnabled.Should().BeTrue();
        result.Value.SmsEnabled.Should().BeTrue();
        result.Value.MarketingEnabled.Should().BeFalse();
        result.Value.AchievementsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task SetQuietHoursAsync_Should_Set_And_Clear_Quiet_Hours()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var setResult = await subject.SetQuietHoursAsync(preference.UserId, new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC");
        var clearResult = await subject.SetQuietHoursAsync(preference.UserId, null, new TimeOnly(7, 0), "UTC");

        setResult.IsSuccess.Should().BeTrue();
        clearResult.IsSuccess.Should().BeTrue();
        preference.QuietHoursStart.Should().BeNull();
        preference.QuietHoursEnd.Should().BeNull();
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Send_When_No_Preferences_Exist()
    {
        using var context = CreateContext();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Drop_When_Channel_Is_Disabled()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateChannelPreferences(false, true, true, false);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Drop);
        result.Reason.Should().Be("channel-disabled");
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Drop_When_Category_Is_Disabled()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateCategoryPreferences(false, false, false, false);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var marketing = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.Marketing, NotificationChannel.InApp, NotificationPriority.Normal);
        var social = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.SocialInteraction, NotificationChannel.InApp, NotificationPriority.Normal);
        var learning = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.CourseCompletion, NotificationChannel.InApp, NotificationPriority.Normal);
        var achievement = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.AchievementUnlocked, NotificationChannel.InApp, NotificationPriority.Normal);

        marketing.Action.Should().Be(NotificationDeliveryAction.Drop);
        marketing.Reason.Should().Be("category-disabled");
        social.Action.Should().Be(NotificationDeliveryAction.Drop);
        learning.Action.Should().Be(NotificationDeliveryAction.Drop);
        achievement.Action.Should().Be(NotificationDeliveryAction.Drop);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task DecideDeliveryAsync_Should_Ignore_Partial_Quiet_Hours_Configuration(bool missingStart)
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(
            missingStart ? null : now.AddHours(-1),
            missingStart ? now.AddHours(1) : null,
            "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Respect_SameDay_Quiet_Hours_And_Bypass_Priority()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddMinutes(-30), now.AddMinutes(30), "UTC", NotificationPriority.High);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var blocked = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);
        var bypassed = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Urgent);

        blocked.Action.Should().Be(NotificationDeliveryAction.Drop);
        blocked.Reason.Should().Be("quiet-hours");
        bypassed.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Allow_When_SameDay_Quiet_Hours_Do_Not_Contain_Current_Time()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(1), now.AddHours(2), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Respect_Overnight_Quiet_Hours_Window()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(-1), now.AddHours(-2), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Drop);
        result.Reason.Should().Be("quiet-hours");
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Respect_Overnight_Quiet_Hours_When_Current_Time_Is_Before_End()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(2), now.AddHours(1), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Drop);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Allow_When_SameDay_Quiet_Hours_Are_Before_Or_After_Current_Time()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));

            preference.SetQuietHours(new TimeOnly(13, 0), new TimeOnly(14, 0), "UTC");
            var beforeWindow = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            preference.SetQuietHours(new TimeOnly(10, 0), new TimeOnly(11, 0), "UTC");
            var afterWindow = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            beforeWindow.Action.Should().Be(NotificationDeliveryAction.Send);
            afterWindow.Action.Should().Be(NotificationDeliveryAction.Send);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Evaluate_All_Overnight_Quiet_Hours_Branches()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 23, 0, 0, TimeSpan.Zero)));
            var afterStart = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero)));
            var beforeEnd = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
            var outsideWindow = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            afterStart.Action.Should().Be(NotificationDeliveryAction.Drop);
            beforeEnd.Action.Should().Be(NotificationDeliveryAction.Drop);
            outsideWindow.Action.Should().Be(NotificationDeliveryAction.Send);
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Theory]
    [InlineData(NotificationType.EmailVerification)]
    [InlineData(NotificationType.PasswordReset)]
    [InlineData(NotificationType.MagicLink)]
    [InlineData(NotificationType.TenantInvite)]
    public async Task DecideDeliveryAsync_Should_Bypass_All_Preferences_For_Transactional_Types(NotificationType type)
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateChannelPreferences(false, false, false, false);
        preference.UpdateCategoryPreferences(false, false, false, false);
        preference.SetEmailDigestFrequency(DigestFrequency.Daily);
        preference.MuteType(type.ToString());
        preference.SetQuietHours(now.AddHours(-1), now.AddHours(1), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, type, NotificationChannel.Email, NotificationPriority.Normal);

        result.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Bypass_All_Preferences_For_Urgent_Priority()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateChannelPreferences(false, false, false, false);
        preference.UpdateCategoryPreferences(false, false, false, false);
        preference.SetEmailDigestFrequency(DigestFrequency.Daily);
        preference.MuteType(nameof(NotificationType.MonthlyStatement));
        preference.SetQuietHours(now.AddHours(-1), now.AddHours(1), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.MonthlyStatement, NotificationChannel.Email, NotificationPriority.Urgent);

        result.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Route_Digestible_Email_To_Digest()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetEmailDigestFrequency(DigestFrequency.Daily);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var digest = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.MonthlyStatement, NotificationChannel.Email, NotificationPriority.Normal);
        var urgent = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.MonthlyStatement, NotificationChannel.Email, NotificationPriority.Urgent);
        var inApp = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.MonthlyStatement, NotificationChannel.InApp, NotificationPriority.Normal);

        digest.Action.Should().Be(NotificationDeliveryAction.Digest);
        urgent.Action.Should().Be(NotificationDeliveryAction.Send);
        inApp.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Drop_Muted_Type_Case_Insensitively()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetMutedTypes("[\"monthlystatement\"]");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var muted = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.MonthlyStatement, NotificationChannel.Email, NotificationPriority.Normal);
        var other = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

        muted.Action.Should().Be(NotificationDeliveryAction.Drop);
        muted.Reason.Should().Be("muted");
        other.Action.Should().Be(NotificationDeliveryAction.Send);
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Drop_InApp_During_Quiet_Hours()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 23, 0, 0, TimeSpan.Zero)));

            var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            result.Action.Should().Be(NotificationDeliveryAction.Drop);
            result.Reason.Should().Be("quiet-hours");
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Hold_Email_Until_SameDay_Quiet_Hours_End()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(9, 0), new TimeOnly(17, 0), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));

            var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

            result.Action.Should().Be(NotificationDeliveryAction.HoldUntil);
            result.HeldUntil.Should().Be(new DateTime(2026, 1, 1, 17, 0, 0, DateTimeKind.Utc));
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Theory]
    [InlineData(23, 0, 2, 6, 0)] // after overnight start -> end lands on the next day
    [InlineData(3, 0, 1, 6, 0)]  // before overnight end -> end lands on the same day
    public async Task DecideDeliveryAsync_Should_Hold_Email_Until_Overnight_Quiet_Hours_End(
        int nowHour, int nowMinute, int expectedDay, int expectedHour, int expectedMinute)
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, nowHour, nowMinute, 0, TimeSpan.Zero)));

            var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

            result.Action.Should().Be(NotificationDeliveryAction.HoldUntil);
            result.HeldUntil.Should().Be(new DateTime(2026, 1, expectedDay, expectedHour, expectedMinute, 0, DateTimeKind.Utc));
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Honor_User_Timezone_For_Quiet_Hours()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "Asia/Tokyo");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            // 2026-01-01 20:00 UTC == 2026-01-02 05:00 Tokyo (inside the 22:00-06:00 window);
            // window ends 06:00 Tokyo == 2026-01-01 21:00 UTC.
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 20, 0, 0, TimeSpan.Zero)));

            var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

            result.Action.Should().Be(NotificationDeliveryAction.HoldUntil);
            result.HeldUntil.Should().Be(new DateTime(2026, 1, 1, 21, 0, 0, DateTimeKind.Utc));
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task DecideDeliveryAsync_Should_Fall_Back_To_Utc_For_Invalid_Timezone()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "Not/AZone");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 23, 0, 0, TimeSpan.Zero)));

            var result = await subject.DecideDeliveryAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

            result.Action.Should().Be(NotificationDeliveryAction.HoldUntil);
            result.HeldUntil.Should().Be(new DateTime(2026, 1, 2, 6, 0, 0, DateTimeKind.Utc));
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Map_Only_Send_Decision_To_True()
    {
        try
        {
            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 23, 0, 0, TimeSpan.Zero)));

            var send = await DecideWithPreference(preference => { });
            var digest = await DecideWithPreference(preference => preference.SetEmailDigestFrequency(DigestFrequency.Daily), NotificationChannel.Email);
            var held = await DecideWithPreference(preference => preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(6, 0), "UTC"), NotificationChannel.Email);
            var dropped = await DecideWithPreference(preference => preference.UpdateChannelPreferences(true, true, false, true));

            send.Should().BeTrue();
            digest.Should().BeFalse();
            held.Should().BeFalse();
            dropped.Should().BeFalse();
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    private static async Task<bool> DecideWithPreference(Action<NotificationPreference> configure, NotificationChannel channel = NotificationChannel.InApp)
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        configure(preference);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

#pragma warning disable CS0618 // legacy wrapper contract for the dispatcher lane
        return await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, channel, NotificationPriority.Normal);
#pragma warning restore CS0618
    }

    private static NotificationsTestDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NotificationsTestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationsTestDbContext(options);
    }

    private sealed class ApplicationDbContextAdapter(NotificationsTestDbContext inner) : IApplicationDbContext
    {
        public DbSet<T> Set<T>() where T : class => inner.Set<T>();

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => inner.SaveChangesAsync(cancellationToken);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(Mock.Of<IDbContextTransaction>());
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
