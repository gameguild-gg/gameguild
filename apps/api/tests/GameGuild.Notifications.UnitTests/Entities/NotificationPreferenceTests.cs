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

    [Fact]
    public void MuteTypeHelpers_Should_Roundtrip_Case_Insensitively()
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());

        preference.MuteType("MonthlyStatement");
        preference.MuteType("marketing");

        preference.GetMutedTypeNames().Should().Contain("MonthlyStatement").And.Contain("Marketing");
        preference.GetMutedTypeNames().Contains("monthlystatement").Should().BeTrue();

        preference.UnmuteType("MONTHLYSTATEMENT");

        preference.GetMutedTypeNames().Should().NotContain("MonthlyStatement").And.Contain("Marketing");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json at all")]
    [InlineData("[1,2]")]
    [InlineData("{\"a\":\"b\"}")]
    [InlineData("null")]
    public void GetMutedTypeNames_Should_Be_Parse_Safe_For_Malformed_Input(string? mutedTypes)
    {
        var preference = NotificationPreference.CreateDefault(Guid.NewGuid());

        preference.SetMutedTypes(mutedTypes);

        preference.GetMutedTypeNames().Should().BeEmpty();
    }
}
