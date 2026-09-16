using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Entities;

public sealed class ProgramWishlistTests
{
    [Fact]
    public void DefaultsAndComputedProperties_DescribeANewGlobalEntry()
    {
        var wishlist = new ProgramWishlist
        {
            AddedAt = SystemClock.UtcNow.AddDays(-10),
            Program = new Program()
        };

        wishlist.IsGlobal.Should().BeTrue();
        wishlist.Priority.Should().Be(3);
        wishlist.PriorityDescription.Should().Be("Medium");
        wishlist.NotifyWhenAvailable.Should().BeTrue();
        wishlist.NotificationSent.Should().BeFalse();
        wishlist.DaysOnWishlist.Should().BeInRange(9, 10);

        wishlist.TenantId = Guid.NewGuid();
        wishlist.IsGlobal.Should().BeFalse();
    }

    [Theory]
    [InlineData(1, "Very Low")]
    [InlineData(2, "Low")]
    [InlineData(3, "Medium")]
    [InlineData(4, "High")]
    [InlineData(5, "Very High")]
    [InlineData(6, "Unknown")]
    public void PriorityDescription_MapsEveryPriority(int priority, string description)
    {
        new ProgramWishlist { Priority = priority }.PriorityDescription.Should().Be(description);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(3, 3)]
    [InlineData(8, 5)]
    public void SetPriority_ClampsValuesToSupportedRange(int requested, int expected)
    {
        var wishlist = new ProgramWishlist();

        wishlist.SetPriority(requested);

        wishlist.Priority.Should().Be(expected);
        wishlist.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void NotesAndNotificationMethods_UpdateTheEntryState()
    {
        var wishlist = new ProgramWishlist();

        wishlist.UpdateNotes("Waiting for the next cohort");
        wishlist.Notes.Should().Be("Waiting for the next cohort");

        wishlist.MarkNotificationSent();
        wishlist.NotificationSent.Should().BeTrue();
        wishlist.LastNotificationSentAt.Should().NotBeNull();

        wishlist.EnableNotifications();
        wishlist.NotifyWhenAvailable.Should().BeTrue();
        wishlist.NotificationSent.Should().BeFalse();

        wishlist.DisableNotifications();
        wishlist.NotifyWhenAvailable.Should().BeFalse();

        wishlist.ResetNotificationStatus();
        wishlist.NotificationSent.Should().BeFalse();
        wishlist.LastNotificationSentAt.Should().BeNull();
        wishlist.UpdatedAt.Should().NotBe(default);
    }

    [Fact]
    public void InterestedTags_IgnoreBlankValuesAndMatchCaseInsensitively()
    {
        var wishlist = new ProgramWishlist();

        wishlist.GetInterestedTagsArray().Should().BeEmpty();
        wishlist.SetInterestedTags("unity", " ", "Multiplayer");

        wishlist.InterestedTags.Should().Be("unity,Multiplayer");
        wishlist.GetInterestedTagsArray().Should().Equal("unity", "Multiplayer");
        wishlist.IsInterestedInTag("UNITY").Should().BeTrue();
        wishlist.IsInterestedInTag("unreal").Should().BeFalse();
    }

    [Fact]
    public void PriorityChanges_StopAtTheirBoundaries()
    {
        var wishlist = new ProgramWishlist { Priority = 4 };

        wishlist.IncreasePriority();
        wishlist.Priority.Should().Be(5);
        var updatedAtMaximum = wishlist.UpdatedAt;
        wishlist.IncreasePriority();
        wishlist.Priority.Should().Be(5);
        wishlist.UpdatedAt.Should().Be(updatedAtMaximum);

        wishlist.Priority = 2;
        wishlist.DecreasePriority();
        wishlist.Priority.Should().Be(1);
        var updatedAtMinimum = wishlist.UpdatedAt;
        wishlist.DecreasePriority();
        wishlist.Priority.Should().Be(1);
        wishlist.UpdatedAt.Should().Be(updatedAtMinimum);
    }

    [Theory]
    [InlineData(false, false, true, false)]
    [InlineData(true, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    public void ShouldNotify_RequiresOptInUnsentNotificationAndOpenEnrollment(
        bool notifyWhenAvailable,
        bool notificationSent,
        bool enrollmentOpen,
        bool expected)
    {
        var program = new Program();
        if (!enrollmentOpen)
            program.CloseEnrollment();
        var wishlist = new ProgramWishlist
        {
            NotifyWhenAvailable = notifyWhenAvailable,
            NotificationSent = notificationSent,
            Program = program
        };

        wishlist.ShouldNotify.Should().Be(expected);
    }

    [Fact]
    public void CanEnrollNow_DelegatesToTheWishlistedProgramForItsUser()
    {
        var wishlist = new ProgramWishlist
        {
            UserId = Guid.NewGuid(),
            Program = new Program()
        };

        wishlist.CanEnrollNow().Should().BeTrue();
        wishlist.Program.CloseEnrollment();
        wishlist.CanEnrollNow().Should().BeFalse();
    }
}
