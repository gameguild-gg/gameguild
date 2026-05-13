namespace GameGuild.Notifications.UnitTests.Entities;

public class NotificationPreferenceTests
{
    [Fact]
    public void CreateDefault_Should_Use_Expected_Defaults()
    {
        var userId = Guid.NewGuid();

        var preference = NotificationPreference.CreateDefault(userId);

        preference.UserId.Should().Be(userId);
        preference.EmailEnabled.Should().BeTrue();
        preference.PushEnabled.Should().BeTrue();
        preference.InAppEnabled.Should().BeTrue();
        preference.SmsEnabled.Should().BeFalse();
        preference.MarketingEnabled.Should().BeTrue();
        preference.SocialEnabled.Should().BeTrue();
        preference.LearningEnabled.Should().BeTrue();
        preference.AchievementsEnabled.Should().BeTrue();
        preference.QuietHoursBypassPriority.Should().Be(NotificationPriority.Urgent);
    }

    [Fact]
    public void UpdateMethods_Should_Modify_Preferences()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        var start = new TimeOnly(22, 0);
        var end = new TimeOnly(7, 0);

        preference.UpdateChannelPreferences(false, false, true, true);
        preference.UpdateCategoryPreferences(false, false, true, false);
        preference.SetQuietHours(start, end, "UTC", NotificationPriority.High);
        preference.SetEmailDigestFrequency(DigestFrequency.Weekly);
        preference.SetMutedTypes("[\"Marketing\"]");

        preference.EmailEnabled.Should().BeFalse();
        preference.PushEnabled.Should().BeFalse();
        preference.InAppEnabled.Should().BeTrue();
        preference.SmsEnabled.Should().BeTrue();
        preference.MarketingEnabled.Should().BeFalse();
        preference.SocialEnabled.Should().BeFalse();
        preference.LearningEnabled.Should().BeTrue();
        preference.AchievementsEnabled.Should().BeFalse();
        preference.QuietHoursStart.Should().Be(start);
        preference.QuietHoursEnd.Should().Be(end);
        preference.Timezone.Should().Be("UTC");
        preference.QuietHoursBypassPriority.Should().Be(NotificationPriority.High);
        preference.EmailDigestFrequency.Should().Be(DigestFrequency.Weekly);
        preference.MutedTypes.Should().Be("[\"Marketing\"]");
    }

    [Fact]
    public void ClearQuietHours_Should_Remove_Quiet_Hours_Window()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());
        preference.SetQuietHours(new TimeOnly(22, 0), new TimeOnly(7, 0), "UTC");

        preference.ClearQuietHours();

        preference.QuietHoursStart.Should().BeNull();
        preference.QuietHoursEnd.Should().BeNull();
    }
}
