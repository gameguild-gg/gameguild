using FluentAssertions;

using GameGuild.Identity.Tenants;

using Xunit;

namespace GameGuild.Identity.Users.Tests;

/// <summary>
///     Coverage-boost tests for User entity methods not yet covered by existing tests.
///     Targets: SetPasswordHash, IncrementTokenVersion, HasPassword, RecordLogin,
///     VerifyEmail, Suspend, Unsuspend, MarkDeleted, RestoreUser, ValidatePurge,
///     CanPerformActions, CanSignIn, ValidateForAuthentication, ValidateForRegistration,
///     ValidateForTenantJoin, RequiresEmailVerification, GetRoleInTenant, IsMemberOfTenant,
///     GetActiveTenantIds, CreateWithPassword, CreateOAuthUser.
/// </summary>
public class UserCoverageBoostTests
{
    // ── Authentication Methods ───────────────────────────────────────────

    [Fact]
    public void SetPasswordHash_ValidHash_ShouldSetAndIncrementTokenVersion()
    {
        var user = User.Create("test@example.com", "Test");
        var initialVersion = user.TokenVersion;

        user.SetPasswordHash("$2a$10$somebcrypthash");

        user.PasswordHash.Should().Be("$2a$10$somebcrypthash");
        user.TokenVersion.Should().Be(initialVersion + 1);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SetPasswordHash_NullOrEmpty_ShouldThrow(string? hash)
    {
        var user = User.Create("test@example.com", "Test");
        var act = () => user.SetPasswordHash(hash!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void IncrementTokenVersion_ShouldIncrement()
    {
        var user = User.Create("test@example.com", "Test");
        var initial = user.TokenVersion;

        user.IncrementTokenVersion();
        user.IncrementTokenVersion();

        user.TokenVersion.Should().Be(initial + 2);
    }

    [Fact]
    public void HasPassword_WithHash_ShouldBeTrue()
    {
        var user = User.CreateWithPassword("test@example.com", "Test", "$2a$hash");
        user.HasPassword.Should().BeTrue();
    }

    [Fact]
    public void HasPassword_WithoutHash_ShouldBeFalse()
    {
        var user = User.CreateOAuthUser("test@example.com", "Test");
        user.HasPassword.Should().BeFalse();
    }

    [Fact]
    public void RecordLogin_ShouldUpdateTimestamps()
    {
        var user = User.Create("test@example.com", "Test");

        user.RecordLogin();

        user.LastLoginAt.Should().NotBeNull();
        user.LastSeenAt.Should().NotBeNull();
    }

    [Fact]
    public void VerifyEmail_ShouldSetVerified()
    {
        var user = User.Create("test@example.com", "Test");
        user.IsEmailVerified.Should().BeFalse();

        user.VerifyEmail();

        user.IsEmailVerified.Should().BeTrue();
    }

    // ── Status Methods ───────────────────────────────────────────────────

    [Fact]
    public void Suspend_ShouldSetSuspended()
    {
        var user = User.Create("test@example.com", "Test");

        user.Suspend();

        user.IsSuspended.Should().BeTrue();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Unsuspend_ShouldClearSuspended()
    {
        var user = User.Create("test@example.com", "Test");
        user.Suspend();

        user.Unsuspend();

        user.IsSuspended.Should().BeFalse();
    }

    [Fact]
    public void CanPerformActions_ActiveNotSuspended_ShouldBeTrue()
    {
        var user = User.Create("test@example.com", "Test");
        user.CanPerformActions.Should().BeTrue();
    }

    [Fact]
    public void CanPerformActions_Suspended_ShouldBeFalse()
    {
        var user = User.Create("test@example.com", "Test");
        user.Suspend();
        user.CanPerformActions.Should().BeFalse();
    }

    [Fact]
    public void CanSignIn_Active_ShouldBeTrue()
    {
        var user = User.Create("test@example.com", "Test");
        user.CanSignIn.Should().BeTrue();
    }

    [Fact]
    public void CanSignIn_Inactive_ShouldBeFalse()
    {
        var user = User.Create("test@example.com", "Test");
        user.Deactivate();
        user.CanSignIn.Should().BeFalse();
    }

    [Fact]
    public void Status_ShouldReflectCurrentState()
    {
        var user = User.Create("test@example.com", "Test");

        user.Status.StatusName.Should().Be("Active");

        user.Suspend();
        user.Status.StatusName.Should().Be("Suspended");

        user.Deactivate();
        user.Status.StatusName.Should().Be("Inactive (Suspended)");
    }

    // ── Lifecycle Methods ────────────────────────────────────────────────

    [Fact]
    public void MarkDeleted_ShouldSoftDeleteAndDeactivate()
    {
        var user = User.Create("test@example.com", "Test");
        typeof(EntityBase<Guid>).GetProperty(nameof(EntityBase.Version))!.SetValue(user, 1);

        user.MarkDeleted();

        user.IsDeleted.Should().BeTrue();
        user.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RestoreUser_ShouldRestoreAndActivate()
    {
        var user = User.Create("test@example.com", "Test");
        typeof(EntityBase<Guid>).GetProperty(nameof(EntityBase.Version))!.SetValue(user, 1);
        user.MarkDeleted();

        user.RestoreUser();

        user.IsDeleted.Should().BeFalse();
        user.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ValidatePurge_NotDeleted_ShouldThrow()
    {
        var user = User.Create("test@example.com", "Test");

        var act = () => user.ValidatePurge();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*soft-deleted*");
    }

    [Fact]
    public void ValidatePurge_DeletedNoMemberships_ShouldNotThrow()
    {
        var user = User.Create("test@example.com", "Test");
        typeof(EntityBase<Guid>).GetProperty(nameof(EntityBase.Version))!.SetValue(user, 1);
        user.MarkDeleted();

        var act = () => user.ValidatePurge();

        act.Should().NotThrow();
    }

    // ── ValidateForAuthentication ────────────────────────────────────────

    [Fact]
    public void ValidateForAuthentication_ValidUser_ShouldSucceed()
    {
        var user = User.Create("test@example.com", "Test");

        var result = user.ValidateForAuthentication(user.TokenVersion);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForAuthentication_InactiveUser_ShouldFail()
    {
        var user = User.Create("test@example.com", "Test");
        user.Deactivate();

        var result = user.ValidateForAuthentication(user.TokenVersion);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(UserAuthenticationFailure.Inactive);
    }

    [Fact]
    public void ValidateForAuthentication_SuspendedUser_ShouldFail()
    {
        var user = User.Create("test@example.com", "Test");
        user.Suspend();

        var result = user.ValidateForAuthentication(user.TokenVersion);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(UserAuthenticationFailure.Suspended);
    }

    [Fact]
    public void ValidateForAuthentication_WrongTokenVersion_ShouldFail()
    {
        var user = User.Create("test@example.com", "Test");

        var result = user.ValidateForAuthentication(user.TokenVersion + 1);

        result.IsSuccess.Should().BeFalse();
        result.FailureReason.Should().Be(UserAuthenticationFailure.TokenRevoked);
    }

    // ── ValidateForRegistration ──────────────────────────────────────────

    [Fact]
    public void ValidateForRegistration_ValidUser_ShouldSucceed()
    {
        var user = User.Create("test@example.com", "Test User");

        var result = user.ValidateForRegistration();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForRegistration_EmptyEmail_ShouldFail()
    {
        var user = new User { Email = "", Name = "Test User" };

        var result = user.ValidateForRegistration();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateForRegistration_InvalidEmail_ShouldFail()
    {
        var user = new User { Email = "notanemail", Name = "Test User" };

        var result = user.ValidateForRegistration();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateForRegistration_ShortName_ShouldFail()
    {
        var user = new User { Email = "test@example.com", Name = "A" };

        var result = user.ValidateForRegistration();

        result.IsSuccess.Should().BeFalse();
    }

    // ── ValidateForTenantJoin ────────────────────────────────────────────

    [Fact]
    public void ValidateForTenantJoin_ActiveUser_ShouldSucceed()
    {
        var user = User.Create("test@example.com", "Test");
        var tenantId = Guid.NewGuid();

        var result = user.ValidateForTenantJoin(tenantId);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateForTenantJoin_InactiveUser_ShouldFail()
    {
        var user = User.Create("test@example.com", "Test");
        user.Deactivate();

        var result = user.ValidateForTenantJoin(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public void ValidateForTenantJoin_SuspendedUser_ShouldFail()
    {
        var user = User.Create("test@example.com", "Test");
        user.Suspend();

        var result = user.ValidateForTenantJoin(Guid.NewGuid());

        result.IsSuccess.Should().BeFalse();
    }

    // ── RequiresEmailVerification ────────────────────────────────────────

    [Fact]
    public void RequiresEmailVerification_Unverified_ShouldReturnTrue()
    {
        var user = User.Create("test@example.com", "Test");

        user.RequiresEmailVerification().Should().BeTrue();
    }

    [Fact]
    public void RequiresEmailVerification_Verified_ShouldReturnFalse()
    {
        var user = User.Create("test@example.com", "Test");
        user.VerifyEmail();

        user.RequiresEmailVerification().Should().BeFalse();
    }

    [Fact]
    public void RequiresEmailVerification_FalseParam_ShouldReturnFalse()
    {
        var user = User.Create("test@example.com", "Test");

        user.RequiresEmailVerification(false).Should().BeFalse();
    }

    // ── Tenant Membership Methods ────────────────────────────────────────

    [Fact]
    public void GetRoleInTenant_NoMemberships_ShouldReturnNull()
    {
        var user = User.Create("test@example.com", "Test");

        user.GetRoleInTenant(Guid.NewGuid()).Should().BeNull();
    }

    [Fact]
    public void IsMemberOfTenant_NoMemberships_ShouldReturnFalse()
    {
        var user = User.Create("test@example.com", "Test");

        user.IsMemberOfTenant(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void GetActiveTenantIds_NoMemberships_ShouldReturnEmpty()
    {
        var user = User.Create("test@example.com", "Test");

        user.GetActiveTenantIds().Should().BeEmpty();
    }

    // ── Factory Methods ──────────────────────────────────────────────────

    [Fact]
    public void CreateWithPassword_ValidArgs_ShouldCreateUser()
    {
        var user = User.CreateWithPassword("test@example.com", "Test User", "$2a$hash", "testuser");

        user.Email.Should().Be("test@example.com");
        user.Name.Should().Be("Test User");
        user.PasswordHash.Should().Be("$2a$hash");
        user.Username.Should().Be("testuser");
        user.IsActive.Should().BeTrue();
        user.HasPassword.Should().BeTrue();
    }

    [Fact]
    public void CreateWithPassword_NoUsername_ShouldWork()
    {
        var user = User.CreateWithPassword("test@example.com", "Test User", "$2a$hash");

        user.Username.Should().BeNull();
    }

    [Theory]
    [InlineData(null, "Name", "hash")]
    [InlineData("email@test.com", null, "hash")]
    [InlineData("email@test.com", "Name", null)]
    public void CreateWithPassword_NullArgs_ShouldThrow(string? email, string? name, string? hash)
    {
        var act = () => User.CreateWithPassword(email!, name!, hash!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateOAuthUser_ValidArgs_ShouldCreateUserWithoutPassword()
    {
        var user = User.CreateOAuthUser("oauth@example.com", "OAuth User");

        user.Email.Should().Be("oauth@example.com");
        user.Name.Should().Be("OAuth User");
        user.PasswordHash.Should().BeNull();
        user.HasPassword.Should().BeFalse();
        user.IsActive.Should().BeTrue();
        user.IsEmailVerified.Should().BeTrue(); // OAuth emails pre-verified
    }

    [Theory]
    [InlineData(null, "Name")]
    [InlineData("email@test.com", null)]
    public void CreateOAuthUser_NullArgs_ShouldThrow(string? email, string? name)
    {
        var act = () => User.CreateOAuthUser(email!, name!);
        act.Should().Throw<ArgumentException>();
    }
}
