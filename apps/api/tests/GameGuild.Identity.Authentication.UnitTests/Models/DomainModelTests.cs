using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authentication.UnitTests.Models;

public class ResourcePermissionTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaults()
    {
        var perm = new GenericResourcePermission();

        perm.ResourceType.Should().BeEmpty();
        perm.ResourceTitle.Should().BeNull();
    }

    [Fact]
    public void Constructor_WithValidArgs_ShouldSetProperties()
    {
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();

        var perm = new GenericResourcePermission(userId, tenantId, resourceId, "Course");

        perm.UserId.Should().Be(userId);
        perm.ResourceId.Should().Be(resourceId);
        perm.ResourceType.Should().Be("Course");
    }

    [Fact]
    public void Constructor_WithNullResourceTypeName_ShouldThrow()
    {
        var act = () => new GenericResourcePermission(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceTypeName");
    }

    [Fact]
    public void UpdateResource_ShouldUpdateIdAndTitle()
    {
        var perm = new GenericResourcePermission();
        var newResourceId = Guid.NewGuid();

        perm.UpdateResource(newResourceId, "My Course");

        perm.ResourceId.Should().Be(newResourceId);
        perm.ResourceTitle.Should().Be("My Course");
    }

    [Fact]
    public void UpdateResource_WithoutTitle_ShouldSetNullTitle()
    {
        var perm = new GenericResourcePermission();
        perm.ResourceTitle = "Old Title";
        var newResourceId = Guid.NewGuid();

        perm.UpdateResource(newResourceId);

        perm.ResourceId.Should().Be(newResourceId);
        perm.ResourceTitle.Should().BeNull();
    }

    [Fact]
    public void UpdateResourceTitle_ShouldUpdateTitle()
    {
        var perm = new GenericResourcePermission();

        perm.UpdateResourceTitle("New Title");

        perm.ResourceTitle.Should().Be("New Title");
    }

    [Fact]
    public void UpdateResourceTitle_WithNull_ShouldClearTitle()
    {
        var perm = new GenericResourcePermission();
        perm.ResourceTitle = "Old";

        perm.UpdateResourceTitle(null);

        perm.ResourceTitle.Should().BeNull();
    }

    [Fact]
    public void AppliesToResource_WhenMatching_ShouldReturnTrue()
    {
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(
            Guid.NewGuid(), Guid.NewGuid(), resourceId, "Course");

        perm.AppliesToResource(resourceId).Should().BeTrue();
    }

    [Fact]
    public void AppliesToResource_WhenNotMatching_ShouldReturnFalse()
    {
        var perm = new GenericResourcePermission(
            Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Course");

        perm.AppliesToResource(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void IsForUserAndResource_WhenBothMatch_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(
            userId, Guid.NewGuid(), resourceId, "Course");

        perm.IsForUserAndResource(userId, resourceId).Should().BeTrue();
    }

    [Fact]
    public void IsForUserAndResource_WhenUserMismatch_ShouldReturnFalse()
    {
        var resourceId = Guid.NewGuid();
        var perm = new GenericResourcePermission(
            Guid.NewGuid(), Guid.NewGuid(), resourceId, "Course");

        perm.IsForUserAndResource(Guid.NewGuid(), resourceId).Should().BeFalse();
    }

    [Fact]
    public void IsForUserAndResource_WhenResourceMismatch_ShouldReturnFalse()
    {
        var userId = Guid.NewGuid();
        var perm = new GenericResourcePermission(
            userId, Guid.NewGuid(), Guid.NewGuid(), "Course");

        perm.IsForUserAndResource(userId, Guid.NewGuid()).Should().BeFalse();
    }
}

public class AuthenticationAttemptContextTests
{
    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var ctx = new AuthenticationAttemptContext();

        ctx.Identifier.Should().BeEmpty();
        ctx.AuthenticationMethod.Should().BeEmpty();
        ctx.IpAddress.Should().BeEmpty();
        ctx.UserAgent.Should().BeEmpty();
        ctx.UserId.Should().BeNull();
        ctx.Device.Should().BeNull();
        ctx.Location.Should().BeNull();
        ctx.DeviceFingerprint.Should().BeNull();
        ctx.TenantId.Should().BeNull();
        ctx.TenantInfo.Should().BeNull();
        ctx.Metadata.Should().BeNull();
    }

    [Fact]
    public void TimeOfDay_ShouldReturnAttemptedAtTimeOfDay()
    {
        var time = new DateTime(2025, 1, 15, 14, 30, 0, DateTimeKind.Utc);
        var ctx = new AuthenticationAttemptContext { AttemptedAt = time };

        ctx.TimeOfDay.Should().Be(new TimeSpan(14, 30, 0));
    }

    [Fact]
    public void DayOfWeek_ShouldReturnAttemptedAtDayOfWeek()
    {
        // 2025-01-15 is a Wednesday
        var ctx = new AuthenticationAttemptContext
        {
            AttemptedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        ctx.DayOfWeek.Should().Be(DayOfWeek.Wednesday);
    }

    [Fact]
    public void IsWeekend_OnSaturday_ShouldReturnTrue()
    {
        // 2025-01-18 is a Saturday
        var ctx = new AuthenticationAttemptContext
        {
            AttemptedAt = new DateTime(2025, 1, 18, 0, 0, 0, DateTimeKind.Utc)
        };

        ctx.IsWeekend.Should().BeTrue();
    }

    [Fact]
    public void IsWeekend_OnSunday_ShouldReturnTrue()
    {
        // 2025-01-19 is a Sunday
        var ctx = new AuthenticationAttemptContext
        {
            AttemptedAt = new DateTime(2025, 1, 19, 0, 0, 0, DateTimeKind.Utc)
        };

        ctx.IsWeekend.Should().BeTrue();
    }

    [Fact]
    public void IsWeekend_OnWeekday_ShouldReturnFalse()
    {
        // 2025-01-15 is a Wednesday
        var ctx = new AuthenticationAttemptContext
        {
            AttemptedAt = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc)
        };

        ctx.IsWeekend.Should().BeFalse();
    }

    [Fact]
    public void DeviceInfo_Alias_ShouldSyncWithDevice()
    {
        var ctx = new AuthenticationAttemptContext();
        var device = new DeviceInfo { Fingerprint = "test-device" };

        ctx.DeviceInfo = device;

        ctx.Device.Should().BeSameAs(device);
        ctx.DeviceInfo.Should().BeSameAs(device);
    }

    [Fact]
    public void LocationInfo_Alias_ShouldSyncWithLocation()
    {
        var ctx = new AuthenticationAttemptContext();
        var location = new LocationInfo { IpAddress = "1.2.3.4" };

        ctx.LocationInfo = location;

        ctx.Location.Should().BeSameAs(location);
        ctx.LocationInfo.Should().BeSameAs(location);
    }

    [Fact]
    public void Timestamp_Alias_ShouldSyncWithAttemptedAt()
    {
        var ctx = new AuthenticationAttemptContext();
        var time = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

        ctx.Timestamp = time;

        ctx.AttemptedAt.Should().Be(time);
        ctx.Timestamp.Should().Be(time);
    }
}

public class MfaVerificationResultTests
{
    [Fact]
    public void DefaultConstructor_ShouldSetDefaults()
    {
        var result = new MfaVerificationResult();

        result.IsSuccess.Should().BeFalse();
        result.Success.Should().BeFalse();
        result.Message.Should().BeNull();
        result.BackupCodes.Should().BeNull();
        result.RequiresAdditionalVerification.Should().BeFalse();
    }

    [Fact]
    public void Successful_ShouldCreateSuccessResult()
    {
        var result = MfaVerificationResult.Successful("MFA verified");

        result.IsSuccess.Should().BeTrue();
        result.Success.Should().BeTrue();
        result.Message.Should().Be("MFA verified");
        result.BackupCodes.Should().BeNull();
    }

    [Fact]
    public void Successful_WithBackupCodes_ShouldIncludeThem()
    {
        var codes = new[] { "code1", "code2", "code3" };
        var result = MfaVerificationResult.Successful("MFA enabled", codes);

        result.IsSuccess.Should().BeTrue();
        result.BackupCodes.Should().BeEquivalentTo(codes);
    }

    [Fact]
    public void Failure_ShouldCreateFailureResult()
    {
        var result = MfaVerificationResult.Failure("Invalid code");

        result.IsSuccess.Should().BeFalse();
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid code");
    }

    [Fact]
    public void Success_Property_ShouldSyncWithIsSuccess()
    {
        var result = new MfaVerificationResult();

        result.Success = true;

        result.IsSuccess.Should().BeTrue();
        result.Success.Should().BeTrue();
    }
}
