using FluentAssertions;
using GameGuild.Identity.Authorization;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests.Authorization;

public sealed class ProgramPermissionTests
{
    [Fact]
    public void Constructors_InitializeResourceIdentityAndOptionalPermission()
    {
        var empty = new ProgramPermission();
        empty.Permissions.Should().BeEmpty();

        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var permission = new ProgramPermission(userId, tenantId, programId);

        permission.UserId.Should().Be(userId);
        permission.TenantId.Should().NotBeNull();
        permission.ResourceId.Should().Be(programId);
        permission.ResourceType.Should().Be(nameof(Program));
        permission.Permissions.Should().BeEmpty();

        var initialized = new ProgramPermission(userId, null, programId, PermissionType.Publish);
        initialized.HasPermission(PermissionType.Publish).Should().BeTrue();
    }

    [Fact]
    public void CapabilityProperties_RequireTheirPermissionAndAnUnexpiredGrant()
    {
        var capabilities = new (PermissionType Permission, Func<ProgramPermission, bool> Read)[]
        {
            (PermissionType.Read, permission => permission.CanViewContent),
            (PermissionType.Edit, permission => permission.CanEditContent),
            (PermissionType.Review, permission => permission.CanReviewContent),
            (PermissionType.Draft, permission => permission.CanCreateDrafts),
            (PermissionType.Submit, permission => permission.CanSubmitForReview),
            (PermissionType.Archive, permission => permission.CanArchive),
            (PermissionType.Clone, permission => permission.CanClone),
            (PermissionType.Delete, permission => permission.CanDelete),
            (PermissionType.Edit, permission => permission.CanManageUsers),
            (PermissionType.Analytics, permission => permission.CanViewUserProgress),
            (PermissionType.Feedback, permission => permission.CanManageFeedback),
            (PermissionType.Publish, permission => permission.CanPublish),
            (PermissionType.Unpublish, permission => permission.CanUnpublish),
            (PermissionType.Schedule, permission => permission.CanSchedule),
            (PermissionType.Monetize, permission => permission.CanMonetize),
            (PermissionType.Pricing, permission => permission.CanSetPricing),
            (PermissionType.Paywall, permission => permission.CanAddPaywall),
            (PermissionType.Analytics, permission => permission.CanViewAnalytics),
            (PermissionType.Performance, permission => permission.CanViewPerformance),
            (PermissionType.Approve, permission => permission.CanApprove),
            (PermissionType.Reject, permission => permission.CanReject),
            (PermissionType.Categorize, permission => permission.CanCategorize),
            (PermissionType.Collection, permission => permission.CanAddToCollection),
            (PermissionType.Series, permission => permission.CanCreateSeries)
        };

        foreach (var (requiredPermission, readCapability) in capabilities)
        {
            var permission = new ProgramPermission(Guid.NewGuid(), null, Guid.NewGuid());
            readCapability(permission).Should().BeFalse($"{requiredPermission} has not been granted");

            permission.AddPermission(requiredPermission);
            readCapability(permission).Should().BeTrue($"{requiredPermission} is active and permanent");

            permission.ExpiresAt = SystemClock.UtcNow.AddMinutes(-1);
            readCapability(permission).Should().BeFalse($"{requiredPermission} is expired");
        }
    }
}
