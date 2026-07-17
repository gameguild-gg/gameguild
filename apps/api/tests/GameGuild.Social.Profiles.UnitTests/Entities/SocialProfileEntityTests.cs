using FluentAssertions;
using Xunit;

namespace GameGuild.Social.Profiles.UnitTests.Entities;

public sealed class SocialProfileEntityTests
{
    [Theory]
    [InlineData("  @GameDev  ", "gamedev")]
    [InlineData("@@Creator", "creator")]
    [InlineData("plain-handle", "plain-handle")]
    public void NormalizeHandle_TrimsMentionsWhitespaceAndCase(string input, string expected)
    {
        SocialProfile.NormalizeHandle(input).Should().Be(expected);
    }

    [Fact]
    public void UpdateProfile_NormalizesFieldsAndCalculatesCompleteness()
    {
        var profile = new SocialProfile();
        var command = new UpdateSocialProfileCommand(
            Guid.NewGuid(),
            " @Ada ",
            "Ada Lovelace",
            "Engine programmer",
            "https://cdn.test/ada.png",
            "https://cdn.test/banner.png",
            "Gameplay Engineer",
            "Sao Paulo",
            "America/Sao_Paulo",
            "https://ada.test",
            "{\"github\":\"ada\"}",
            ProfileAvailabilityStatus.OpenToCollaborate);

        profile.UpdateProfile(command);

        profile.Should().Match<SocialProfile>(value =>
            value.Handle == "ada" &&
            value.DisplayName == command.DisplayName &&
            value.Bio == command.Bio &&
            value.AvatarUrl == command.AvatarUrl &&
            value.BannerUrl == command.BannerUrl &&
            value.Headline == command.Headline &&
            value.Location == command.Location &&
            value.TimeZone == command.TimeZone &&
            value.WebsiteUrl == command.WebsiteUrl &&
            value.SocialLinksJson == command.SocialLinksJson &&
            value.AvailabilityStatus == command.AvailabilityStatus &&
            value.CompletenessScore == 90);
    }

    [Fact]
    public void CalculateCompleteness_IncludesSkillsAndPortfolioWithoutExceedingOneHundred()
    {
        var profile = new SocialProfile
        {
            Handle = "complete",
            DisplayName = "Complete Profile",
            Bio = "Bio",
            AvatarUrl = "avatar",
            Headline = "Headline",
            WebsiteUrl = "website",
            Skills = [new ProfileSkill { Name = "C#" }],
            PortfolioItems = [new ProfilePortfolioItem { Title = "Game" }]
        };

        profile.CalculateCompleteness().Should().Be(100);
    }

    [Fact]
    public void UpdateStats_ClampsEveryCounterAtZero()
    {
        var profile = new SocialProfile();

        profile.UpdateStats(-10, 12, -1, 3);

        profile.FollowerCount.Should().Be(0);
        profile.FollowingCount.Should().Be(12);
        profile.PostCount.Should().Be(0);
        profile.ProjectCount.Should().Be(3);
    }

    [Fact]
    public void UpdatePrivacy_ReplacesVisibilityFlagsTogether()
    {
        var profile = new SocialProfile();

        profile.UpdatePrivacy(ProfileVisibility.Connections, false, false, true);

        profile.Visibility.Should().Be(ProfileVisibility.Connections);
        profile.ShowActivity.Should().BeFalse();
        profile.ShowPortfolio.Should().BeFalse();
        profile.ShowSkills.Should().BeTrue();
    }

    [Fact]
    public void ToDto_OrdersSkillsAndPinsPortfolioBeforeDisplayOrder()
    {
        var profile = new SocialProfile
        {
            Skills =
            [
                new ProfileSkill { Name = "Later", DisplayOrder = 20 },
                new ProfileSkill { Name = "First", DisplayOrder = 1 }
            ],
            PortfolioItems =
            [
                new ProfilePortfolioItem { Title = "Unpinned", DisplayOrder = 0 },
                new ProfilePortfolioItem { Title = "Pinned later", IsPinned = true, DisplayOrder = 20 },
                new ProfilePortfolioItem { Title = "Pinned first", IsPinned = true, DisplayOrder = 1 }
            ]
        };

        var result = profile.ToDto();

        result.Skills.Select(skill => skill.Name).Should().Equal("First", "Later");
        result.PortfolioItems.Select(item => item.Title).Should().Equal("Pinned first", "Pinned later", "Unpinned");
    }

    [Fact]
    public void SkillAndPortfolioUpdates_ReplaceMutableFields()
    {
        var skill = new ProfileSkill { Proficiency = ProfileSkillProficiency.Beginner, DisplayOrder = 5 };
        var item = new ProfilePortfolioItem { Title = "Old" };

        skill.Update(ProfileSkillProficiency.Expert, 1);
        item.Update("New", "Description", "https://game.test", "https://image.test", true, 2);

        skill.ToDto().Should().Match<ProfileSkillDto>(dto => dto.Proficiency == ProfileSkillProficiency.Expert && dto.DisplayOrder == 1);
        item.ToDto().Should().Match<ProfilePortfolioItemDto>(dto =>
            dto.Title == "New" && dto.Description == "Description" && dto.Url == "https://game.test" &&
            dto.ImageUrl == "https://image.test" && dto.IsPinned && dto.DisplayOrder == 2);
    }
}
