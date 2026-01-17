using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Tenants.UnitTests.Entities;

public class TenantResultTests
{
    [Fact]
    public void TenantMembershipResult_Should_Create_Success_And_Failure()
    {
        var success = TenantMembershipResult.Success();
        var failure = TenantMembershipResult.Failure("error");

        success.IsSuccess.Should().BeTrue();
        success.Error.Should().BeNull();
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("error");
    }

    [Fact]
    public void TenantValidationResult_Should_Create_Success_And_Failure()
    {
        var success = TenantValidationResult.Success();
        var failure = TenantValidationResult.Failure(new[] { "invalid" });

        success.IsSuccess.Should().BeTrue();
        success.Errors.Should().BeEmpty();
        failure.IsSuccess.Should().BeFalse();
        failure.Errors.Should().Contain("invalid");
    }

    [Fact]
    public void TenantArchiveResult_Should_Create_Success_And_Failure()
    {
        var success = TenantArchiveResult.Success(5);
        var failure = TenantArchiveResult.Failure("blocked");

        success.IsSuccess.Should().BeTrue();
        success.AffectedMemberCount.Should().Be(5);
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("blocked");
    }

    [Fact]
    public void TenantDeleteResult_Should_Create_Success_And_Failure()
    {
        var success = TenantDeleteResult.Success();
        var failure = TenantDeleteResult.Failure("blocked");

        success.IsSuccess.Should().BeTrue();
        failure.IsSuccess.Should().BeFalse();
        failure.Error.Should().Be("blocked");
    }
}
