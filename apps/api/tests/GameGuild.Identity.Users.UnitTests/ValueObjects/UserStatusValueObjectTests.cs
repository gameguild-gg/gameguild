using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Users.UnitTests.ValueObjects;

public class UserStatusValueObjectTests
{
    [Fact]
    public void Active_ShouldReturnActiveNotSuspended()
    {
        var status = UserStatus.Active();

        status.IsActive.Should().BeTrue();
        status.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void Inactive_ShouldReturnInactiveNotSuspended()
    {
        var status = UserStatus.Inactive();

        status.IsActive.Should().BeFalse();
        status.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void Suspended_ShouldReturnActiveAndSuspended()
    {
        var status = UserStatus.Suspended();

        status.IsActive.Should().BeTrue();
        status.IsSuspended.Should().BeTrue();
    }

    [Fact]
    public void CanPerformActions_Active_ShouldBeTrue()
    {
        UserStatus.Active().CanPerformActions.Should().BeTrue();
    }

    [Fact]
    public void CanPerformActions_Inactive_ShouldBeFalse()
    {
        UserStatus.Inactive().CanPerformActions.Should().BeFalse();
    }

    [Fact]
    public void CanPerformActions_Suspended_ShouldBeFalse()
    {
        UserStatus.Suspended().CanPerformActions.Should().BeFalse();
    }

    [Fact]
    public void CanPerformActions_InactiveSuspended_ShouldBeFalse()
    {
        new UserStatus(false, true).CanPerformActions.Should().BeFalse();
    }

    [Fact]
    public void CanSignIn_Active_ShouldBeTrue()
    {
        UserStatus.Active().CanSignIn.Should().BeTrue();
    }

    [Fact]
    public void CanSignIn_Suspended_ShouldBeTrue()
    {
        UserStatus.Suspended().CanSignIn.Should().BeTrue();
    }

    [Fact]
    public void CanSignIn_Inactive_ShouldBeFalse()
    {
        UserStatus.Inactive().CanSignIn.Should().BeFalse();
    }

    [Theory]
    [InlineData(true, false, "Active")]
    [InlineData(true, true, "Suspended")]
    [InlineData(false, false, "Inactive")]
    [InlineData(false, true, "Inactive (Suspended)")]
    public void StatusName_ShouldReturnCorrectName(bool isActive, bool isSuspended, string expected)
    {
        var status = new UserStatus(isActive, isSuspended);

        status.StatusName.Should().Be(expected);
    }

    [Fact]
    public void Activate_FromInactive_ShouldMakeActive()
    {
        var result = UserStatus.Inactive().Activate();

        result.IsActive.Should().BeTrue();
        result.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void Deactivate_FromActive_ShouldMakeInactive()
    {
        var result = UserStatus.Active().Deactivate();

        result.IsActive.Should().BeFalse();
        result.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void Suspend_FromActive_ShouldMakeSuspended()
    {
        var result = UserStatus.Active().Suspend();

        result.IsActive.Should().BeTrue();
        result.IsSuspended.Should().BeTrue();
    }

    [Fact]
    public void Unsuspend_FromSuspended_ShouldRemoveSuspension()
    {
        var result = UserStatus.Suspended().Unsuspend();

        result.IsActive.Should().BeTrue();
        result.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void Suspend_ThenDeactivate_ShouldBeInactiveSuspended()
    {
        var result = UserStatus.Active().Suspend().Deactivate();

        result.IsActive.Should().BeFalse();
        result.IsSuspended.Should().BeTrue();
        result.StatusName.Should().Be("Inactive (Suspended)");
    }

    [Fact]
    public void ToString_ShouldReturnStatusName()
    {
        UserStatus.Active().ToString().Should().Be("Active");
        UserStatus.Suspended().ToString().Should().Be("Suspended");
        UserStatus.Inactive().ToString().Should().Be("Inactive");
        new UserStatus(false, true).ToString().Should().Be("Inactive (Suspended)");
    }

    [Fact]
    public void EqualStatus_ShouldBeEqual()
    {
        var a = UserStatus.Active();
        var b = UserStatus.Active();

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void DifferentStatus_ShouldNotBeEqual()
    {
        UserStatus.Active().Should().NotBe(UserStatus.Inactive());
        UserStatus.Active().Should().NotBe(UserStatus.Suspended());
    }

    [Fact]
    public void Constructor_ShouldSetProperties()
    {
        var status = new UserStatus(isActive: true, isSuspended: true);

        status.IsActive.Should().BeTrue();
        status.IsSuspended.Should().BeTrue();
    }
}
