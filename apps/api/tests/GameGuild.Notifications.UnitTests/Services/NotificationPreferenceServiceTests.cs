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
    public async Task ShouldSendNotificationAsync_Should_Return_True_When_No_Preferences_Exist()
    {
        using var context = CreateContext();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.ShouldSendNotificationAsync(Guid.NewGuid(), NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Return_False_When_Channel_Is_Disabled()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateChannelPreferences(false, true, true, false);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.Email, NotificationPriority.Normal);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Return_False_When_Category_Is_Disabled()
    {
        using var context = CreateContext();
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.UpdateCategoryPreferences(false, false, false, false);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var marketing = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.Marketing, NotificationChannel.InApp, NotificationPriority.Normal);
        var social = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.SocialInteraction, NotificationChannel.InApp, NotificationPriority.Normal);
        var learning = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.CourseCompletion, NotificationChannel.InApp, NotificationPriority.Normal);
        var achievement = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.AchievementUnlocked, NotificationChannel.InApp, NotificationPriority.Normal);

        marketing.Should().BeFalse();
        social.Should().BeFalse();
        learning.Should().BeFalse();
        achievement.Should().BeFalse();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ShouldSendNotificationAsync_Should_Ignore_Partial_Quiet_Hours_Configuration(bool missingStart)
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

        var result = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Respect_SameDay_Quiet_Hours_And_Bypass_Priority()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddMinutes(-30), now.AddMinutes(30), "UTC", NotificationPriority.High);
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var blocked = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);
        var bypassed = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Urgent);

        blocked.Should().BeFalse();
        bypassed.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Allow_When_SameDay_Quiet_Hours_Do_Not_Contain_Current_Time()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(1), now.AddHours(2), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Respect_Overnight_Quiet_Hours_Window()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(-1), now.AddHours(-2), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Respect_Overnight_Quiet_Hours_When_Current_Time_Is_Before_End()
    {
        using var context = CreateContext();
        var now = TimeOnly.FromDateTime(SystemClock.UtcNow);
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(now.AddHours(2), now.AddHours(1), "UTC");
        context.NotificationPreferences.Add(preference);
        await context.SaveChangesAsync();
        var subject = new NotificationPreferenceService(new ApplicationDbContextAdapter(context));

        var result = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Allow_When_SameDay_Quiet_Hours_Are_Before_Or_After_Current_Time()
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
            var beforeWindow = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            preference.SetQuietHours(new TimeOnly(10, 0), new TimeOnly(11, 0), "UTC");
            var afterWindow = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            beforeWindow.Should().BeTrue();
            afterWindow.Should().BeTrue();
        }
        finally
        {
            SystemClock.Reset();
        }
    }

    [Fact]
    public async Task ShouldSendNotificationAsync_Should_Evaluate_All_Overnight_Quiet_Hours_Branches()
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
            var afterStart = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 3, 0, 0, TimeSpan.Zero)));
            var beforeEnd = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            SystemClock.SetProvider(new FixedTimeProvider(new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero)));
            var outsideWindow = await subject.ShouldSendNotificationAsync(preference.UserId, NotificationType.System, NotificationChannel.InApp, NotificationPriority.Normal);

            afterStart.Should().BeFalse();
            beforeEnd.Should().BeFalse();
            outsideWindow.Should().BeTrue();
        }
        finally
        {
            SystemClock.Reset();
        }
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
