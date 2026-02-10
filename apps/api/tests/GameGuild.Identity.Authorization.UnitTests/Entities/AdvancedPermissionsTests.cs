using FluentAssertions;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

public class AbacPolicyTests
{
    [Fact]
    public void AbacPolicy_DefaultValues_ShouldBeCorrect()
    {
        var policy = new AbacPolicy();

        policy.Id.Should().NotBeEmpty();
        policy.Name.Should().BeEmpty();
        policy.Description.Should().BeNull();
        policy.Effect.Should().Be(AbacPolicyEffect.Allow);
        policy.IsEnabled.Should().BeTrue();
        policy.Priority.Should().Be(0);
        policy.ResourceType.Should().BeNull();
        policy.Version.Should().Be(1);
        policy.SubjectConditions.Should().BeNull();
        policy.ResourceConditions.Should().BeNull();
        policy.EnvironmentConditions.Should().BeNull();
        policy.ActionConditions.Should().BeNull();
        policy.TargetResources.Should().BeNull();
        policy.TargetActions.Should().BeNull();
        policy.AttributeExpression.Should().BeNull();
        policy.ConditionExpression.Should().BeNull();
        policy.TimeConditions.Should().BeNull();
        policy.LocationConditions.Should().BeNull();
        policy.Obligations.Should().BeNull();
        policy.EffectiveFrom.Should().BeNull();
        policy.EffectiveUntil.Should().BeNull();
        policy.Tags.Should().BeNull();
    }

    [Fact]
    public void IsActive_WhenEnabled_ShouldReturnTrue()
    {
        var policy = new AbacPolicy { IsEnabled = true };

        policy.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenDisabled_ShouldReturnFalse()
    {
        var policy = new AbacPolicy { IsEnabled = false };

        policy.IsActive().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenEnabledAndNoDateRange_ShouldReturnTrue()
    {
        var policy = new AbacPolicy { IsEnabled = true };

        policy.IsEffective().Should().BeTrue();
    }

    [Fact]
    public void IsEffective_WhenDisabled_ShouldReturnFalse()
    {
        var policy = new AbacPolicy { IsEnabled = false };

        policy.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenEffectiveFromInFuture_ShouldReturnFalse()
    {
        var policy = new AbacPolicy
        {
            IsEnabled = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(1)
        };

        policy.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenEffectiveUntilInPast_ShouldReturnFalse()
    {
        var policy = new AbacPolicy
        {
            IsEnabled = true,
            EffectiveUntil = DateTime.UtcNow.AddDays(-1)
        };

        policy.IsEffective().Should().BeFalse();
    }

    [Fact]
    public void IsEffective_WhenWithinDateRange_ShouldReturnTrue()
    {
        var policy = new AbacPolicy
        {
            IsEnabled = true,
            EffectiveFrom = DateTime.UtcNow.AddDays(-1),
            EffectiveUntil = DateTime.UtcNow.AddDays(1)
        };

        policy.IsEffective().Should().BeTrue();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        var policy = new AbacPolicy { IsEnabled = false };

        policy.Enable();

        policy.IsEnabled.Should().BeTrue();
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var policy = new AbacPolicy { IsEnabled = true };

        policy.Disable();

        policy.IsEnabled.Should().BeFalse();
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPriority_WithValidValue_ShouldUpdatePriority()
    {
        var policy = new AbacPolicy();

        policy.SetPriority(10);

        policy.Priority.Should().Be(10);
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetPriority_WithNegativeValue_ShouldThrowArgumentException()
    {
        var policy = new AbacPolicy();

        var act = () => policy.SetPriority(-1);

        act.Should().Throw<ArgumentException>().WithParameterName("priority");
    }

    [Fact]
    public void SetPriority_WithZero_ShouldSucceed()
    {
        var policy = new AbacPolicy();

        policy.SetPriority(0);

        policy.Priority.Should().Be(0);
    }

    [Fact]
    public void SetActive_ShouldUpdateEnabledAndUpdatedBy()
    {
        var policy = new AbacPolicy();
        var userId = Guid.NewGuid();

        policy.SetActive(false, userId);

        policy.IsEnabled.Should().BeFalse();
        policy.UpdatedBy.Should().Be(userId);
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetEffectivePeriod_ShouldUpdateDatesAndUpdatedBy()
    {
        var policy = new AbacPolicy();
        var userId = Guid.NewGuid();
        var from = DateTime.UtcNow;
        var until = DateTime.UtcNow.AddDays(30);

        policy.SetEffectivePeriod(from, until, userId);

        policy.EffectiveFrom.Should().Be(from);
        policy.EffectiveUntil.Should().Be(until);
        policy.UpdatedBy.Should().Be(userId);
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SetEffectivePeriod_WithNulls_ShouldClearDates()
    {
        var policy = new AbacPolicy
        {
            EffectiveFrom = DateTime.UtcNow,
            EffectiveUntil = DateTime.UtcNow.AddDays(30)
        };
        var userId = Guid.NewGuid();

        policy.SetEffectivePeriod(null, null, userId);

        policy.EffectiveFrom.Should().BeNull();
        policy.EffectiveUntil.Should().BeNull();
    }

    [Fact]
    public void UpdateExpression_ShouldUpdateExpressionsAndIncrementVersion()
    {
        var policy = new AbacPolicy { Version = 1 };
        var userId = Guid.NewGuid();

        policy.UpdateExpression("{\"dept\": \"IT\"}", "user.level >= 3", userId);

        policy.AttributeExpression.Should().Be("{\"dept\": \"IT\"}");
        policy.ConditionExpression.Should().Be("user.level >= 3");
        policy.UpdatedBy.Should().Be(userId);
        policy.Version.Should().Be(2);
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateMetadata_ShouldUpdateNameDescriptionPriority()
    {
        var policy = new AbacPolicy();
        var userId = Guid.NewGuid();

        policy.UpdateMetadata("Test Policy", "A test description", 5, userId);

        policy.Name.Should().Be("Test Policy");
        policy.Description.Should().Be("A test description");
        policy.Priority.Should().Be(5);
        policy.UpdatedBy.Should().Be(userId);
        policy.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateMetadata_WithNullName_ShouldThrowArgumentNullException()
    {
        var policy = new AbacPolicy();

        var act = () => policy.UpdateMetadata(null!, "desc", 1, Guid.NewGuid());

        act.Should().Throw<ArgumentNullException>().WithParameterName("name");
    }

    [Fact]
    public void IsDenyPolicy_WhenDenyEffect_ShouldReturnTrue()
    {
        var policy = new AbacPolicy { Effect = AbacPolicyEffect.Deny };

        policy.IsDenyPolicy().Should().BeTrue();
    }

    [Fact]
    public void IsDenyPolicy_WhenAllowEffect_ShouldReturnFalse()
    {
        var policy = new AbacPolicy { Effect = AbacPolicyEffect.Allow };

        policy.IsDenyPolicy().Should().BeFalse();
    }

    [Fact]
    public void IsAllowPolicy_WhenAllowEffect_ShouldReturnTrue()
    {
        var policy = new AbacPolicy { Effect = AbacPolicyEffect.Allow };

        policy.IsAllowPolicy().Should().BeTrue();
    }

    [Fact]
    public void IsAllowPolicy_WhenDenyEffect_ShouldReturnFalse()
    {
        var policy = new AbacPolicy { Effect = AbacPolicyEffect.Deny };

        policy.IsAllowPolicy().Should().BeFalse();
    }
}

public class DelegatedAdminScopeTests
{
    [Fact]
    public void DelegatedAdminScope_DefaultValues_ShouldBeCorrect()
    {
        var scope = new DelegatedAdminScope();

        scope.Id.Should().NotBeEmpty();
        scope.Name.Should().BeEmpty();
        scope.Description.Should().BeNull();
        scope.IsActive.Should().BeTrue();
        scope.CanManageUsers.Should().BeFalse();
        scope.CanManagePermissions.Should().BeFalse();
        scope.CanManageResources.Should().BeFalse();
        scope.CanViewAuditLogs.Should().BeFalse();
        scope.AllowedResourceTypes.Should().BeNull();
        scope.AllowedUserIds.Should().BeNull();
        scope.AllowedDepartments.Should().BeNull();
        scope.AllowedTeams.Should().BeNull();
        scope.AllowedRoles.Should().BeNull();
        scope.GrantablePermissions.Should().BeNull();
        scope.DeniedPermissions.Should().BeNull();
        scope.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(1)
        };

        scope.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope { IsActive = false };

        scope.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-2),
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };

        scope.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenBeforeStartDate_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(1)
        };

        scope.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenNoExpiration_ShouldReturnTrue()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = null
        };

        scope.IsValid().Should().BeTrue();
    }

    [Fact]
    public void CanManageUser_WhenValidAndCanManageUsersAndUserInList_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageUsers = true,
            AllowedUserIds = $"[\"{userId}\"]"
        };

        scope.CanManageUser(userId).Should().BeTrue();
    }

    [Fact]
    public void CanManageUser_WhenCanNotManageUsers_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageUsers = false
        };

        scope.CanManageUser(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanManageUser_WhenInvalid_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = false,
            CanManageUsers = true,
            AllowedUserIds = $"[\"{Guid.NewGuid()}\"]"
        };

        scope.CanManageUser(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanManageUser_WhenAllowedUserIdsNull_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageUsers = true,
            AllowedUserIds = null
        };

        scope.CanManageUser(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void CanManageResourceType_WhenValidAndCanManageResourcesAndTypeInList_ShouldReturnTrue()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageResources = true,
            AllowedResourceTypes = "[\"Course\", \"Project\"]"
        };

        scope.CanManageResourceType("Course").Should().BeTrue();
    }

    [Fact]
    public void CanManageResourceType_WhenTypeNotInList_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageResources = true,
            AllowedResourceTypes = "[\"Course\"]"
        };

        scope.CanManageResourceType("Document").Should().BeFalse();
    }

    [Fact]
    public void CanManageResourceType_WhenCannotManageResources_ShouldReturnFalse()
    {
        var scope = new DelegatedAdminScope
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            CanManageResources = false,
            AllowedResourceTypes = "[\"Course\"]"
        };

        scope.CanManageResourceType("Course").Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetIsActiveTrue()
    {
        var scope = new DelegatedAdminScope { IsActive = false };

        scope.Activate();

        scope.IsActive.Should().BeTrue();
        scope.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveFalse()
    {
        var scope = new DelegatedAdminScope { IsActive = true };

        scope.Deactivate();

        scope.IsActive.Should().BeFalse();
        scope.UpdatedAt.Should().NotBeNull();
    }
}

public class PermissionTemplateVersionTests
{
    [Fact]
    public void PermissionTemplateVersion_DefaultValues_ShouldBeCorrect()
    {
        var version = new PermissionTemplateVersion();

        version.Id.Should().NotBeEmpty();
        version.Name.Should().BeEmpty();
        version.Description.Should().BeEmpty();
        version.Permissions.Should().BeEmpty();
        version.IsActive.Should().BeFalse();
        version.ChangeNotes.Should().BeNull();
        version.AddedPermissions.Should().BeNull();
        version.RemovedPermissions.Should().BeNull();
        version.UnchangedPermissions.Should().BeNull();
        version.PreviousVersion.Should().BeNull();
        version.Metadata.Should().BeNull();
        version.PermissionHash.Should().BeNull();
        version.Tags.Should().BeNull();
    }

    [Fact]
    public void CalculateHash_WithEmptyPermissions_ShouldReturnConsistentHash()
    {
        var version = new PermissionTemplateVersion { Permissions = Array.Empty<string>() };

        var hash1 = version.CalculateHash();
        var hash2 = version.CalculateHash();

        hash1.Should().NotBeNullOrEmpty();
        hash1.Should().Be(hash2);
    }

    [Fact]
    public void CalculateHash_WithPermissions_ShouldReturnHash()
    {
        var version = new PermissionTemplateVersion
        {
            Permissions = new[] { "Read", "Write", "Delete" }
        };

        var hash = version.CalculateHash();

        hash.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void CalculateHash_ShouldBeDeterministicRegardlessOfOrder()
    {
        var version1 = new PermissionTemplateVersion
        {
            Permissions = new[] { "Read", "Write", "Delete" }
        };
        var version2 = new PermissionTemplateVersion
        {
            Permissions = new[] { "Delete", "Read", "Write" }
        };

        version1.CalculateHash().Should().Be(version2.CalculateHash());
    }

    [Fact]
    public void CalculateHash_DifferentPermissions_ShouldReturnDifferentHashes()
    {
        var version1 = new PermissionTemplateVersion
        {
            Permissions = new[] { "Read", "Write" }
        };
        var version2 = new PermissionTemplateVersion
        {
            Permissions = new[] { "Read", "Delete" }
        };

        version1.CalculateHash().Should().NotBe(version2.CalculateHash());
    }
}

public class PermissionTemplateMigrationTests
{
    [Fact]
    public void PermissionTemplateMigration_DefaultValues_ShouldBeCorrect()
    {
        var migration = new PermissionTemplateMigration();

        migration.Id.Should().NotBeEmpty();
        migration.Status.Should().Be(MigrationStatus.Planned);
        migration.SuccessCount.Should().Be(0);
        migration.FailureCount.Should().Be(0);
        migration.SkippedCount.Should().Be(0);
        migration.TotalCount.Should().Be(0);
        migration.IsDryRun.Should().BeFalse();
        migration.ScheduledFor.Should().BeNull();
        migration.StartedAt.Should().BeNull();
        migration.CompletedAt.Should().BeNull();
        migration.Errors.Should().BeNull();
        migration.Log.Should().BeNull();
        migration.RollbackPlan.Should().BeNull();
        migration.DryRunResult.Should().BeNull();
        migration.Notes.Should().BeNull();
    }

    [Fact]
    public void GetProgressPercentage_WhenTotalCountZero_ShouldReturnZero()
    {
        var migration = new PermissionTemplateMigration { TotalCount = 0 };

        migration.GetProgressPercentage().Should().Be(0);
    }

    [Fact]
    public void GetProgressPercentage_WhenAllCompleted_ShouldReturn100()
    {
        var migration = new PermissionTemplateMigration
        {
            TotalCount = 10,
            SuccessCount = 10,
            FailureCount = 0,
            SkippedCount = 0
        };

        migration.GetProgressPercentage().Should().Be(100);
    }

    [Fact]
    public void GetProgressPercentage_WhenPartiallyComplete_ShouldReturnCorrectPercentage()
    {
        var migration = new PermissionTemplateMigration
        {
            TotalCount = 100,
            SuccessCount = 30,
            FailureCount = 10,
            SkippedCount = 10
        };

        migration.GetProgressPercentage().Should().Be(50);
    }

    [Fact]
    public void GetProgressPercentage_WithMixedResults_ShouldIncludeAllProcessedCounts()
    {
        var migration = new PermissionTemplateMigration
        {
            TotalCount = 200,
            SuccessCount = 50,
            FailureCount = 25,
            SkippedCount = 25
        };

        migration.GetProgressPercentage().Should().Be(50);
    }

    [Theory]
    [InlineData(MigrationStatus.Completed, true)]
    [InlineData(MigrationStatus.Failed, true)]
    [InlineData(MigrationStatus.RolledBack, true)]
    [InlineData(MigrationStatus.Planned, false)]
    [InlineData(MigrationStatus.Scheduled, false)]
    [InlineData(MigrationStatus.InProgress, false)]
    [InlineData(MigrationStatus.Cancelled, false)]
    public void IsComplete_ShouldReturnCorrectResult(MigrationStatus status, bool expected)
    {
        var migration = new PermissionTemplateMigration { Status = status };

        migration.IsComplete().Should().Be(expected);
    }
}

public class PolicyBundleTests
{
    [Fact]
    public void PolicyBundle_DefaultValues_ShouldBeCorrect()
    {
        var bundle = new PolicyBundle();

        bundle.Id.Should().NotBeEmpty();
        bundle.Name.Should().BeEmpty();
        bundle.Description.Should().BeNull();
        bundle.Version.Should().Be("1.0.0");
        bundle.Status.Should().Be(PolicyBundleStatus.Draft);
        bundle.PolicyData.Should().BeEmpty();
        bundle.ContentHash.Should().BeEmpty();
        bundle.IsGlobal.Should().BeFalse();
        bundle.DigitalSignature.Should().BeNull();
        bundle.Metadata.Should().BeNull();
        bundle.EffectiveFrom.Should().BeNull();
        bundle.EffectiveUntil.Should().BeNull();
        bundle.PreviousVersionId.Should().BeNull();
        bundle.ApprovedBy.Should().BeNull();
        bundle.ApprovedAt.Should().BeNull();
        bundle.DeploymentCount.Should().Be(0);
        bundle.LastDeployedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(PolicyBundleStatus.Approved, true)]
    [InlineData(PolicyBundleStatus.Active, true)]
    [InlineData(PolicyBundleStatus.Draft, false)]
    [InlineData(PolicyBundleStatus.PendingApproval, false)]
    [InlineData(PolicyBundleStatus.Deprecated, false)]
    [InlineData(PolicyBundleStatus.Revoked, false)]
    public void IsApproved_ShouldReturnCorrectResult(PolicyBundleStatus status, bool expected)
    {
        var bundle = new PolicyBundle { Status = status };

        bundle.IsApproved().Should().Be(expected);
    }

    [Theory]
    [InlineData(PolicyBundleStatus.Active, true)]
    [InlineData(PolicyBundleStatus.Draft, false)]
    [InlineData(PolicyBundleStatus.Approved, false)]
    [InlineData(PolicyBundleStatus.PendingApproval, false)]
    [InlineData(PolicyBundleStatus.Deprecated, false)]
    [InlineData(PolicyBundleStatus.Revoked, false)]
    public void IsActive_ShouldReturnCorrectResult(PolicyBundleStatus status, bool expected)
    {
        var bundle = new PolicyBundle { Status = status };

        bundle.IsActive().Should().Be(expected);
    }
}

public class PolicyBundleDeploymentTests
{
    [Fact]
    public void PolicyBundleDeployment_DefaultValues_ShouldBeCorrect()
    {
        var deployment = new PolicyBundleDeployment();

        deployment.Id.Should().NotBeEmpty();
        deployment.Environment.Should().BeEmpty();
        deployment.Status.Should().Be(PolicyDeploymentStatus.Pending);
        deployment.VerificationPassed.Should().BeFalse();
        deployment.VerificationDetails.Should().BeNull();
        deployment.DeploymentNotes.Should().BeNull();
        deployment.ActivatedAt.Should().BeNull();
        deployment.RolledBackAt.Should().BeNull();
        deployment.RolledBackBy.Should().BeNull();
        deployment.RollbackReason.Should().BeNull();
        deployment.Bundle.Should().BeNull();
    }

    [Theory]
    [InlineData(PolicyDeploymentStatus.Active, true)]
    [InlineData(PolicyDeploymentStatus.Pending, false)]
    [InlineData(PolicyDeploymentStatus.Deploying, false)]
    [InlineData(PolicyDeploymentStatus.Failed, false)]
    [InlineData(PolicyDeploymentStatus.RolledBack, false)]
    public void IsActive_ShouldReturnCorrectResult(PolicyDeploymentStatus status, bool expected)
    {
        var deployment = new PolicyBundleDeployment { Status = status };

        deployment.IsActive().Should().Be(expected);
    }

    [Fact]
    public void Activate_ShouldSetStatusToActiveAndSetActivatedAt()
    {
        var deployment = new PolicyBundleDeployment();

        deployment.Activate();

        deployment.Status.Should().Be(PolicyDeploymentStatus.Active);
        deployment.ActivatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Rollback_ShouldSetStatusAndRecordDetails()
    {
        var deployment = new PolicyBundleDeployment { Status = PolicyDeploymentStatus.Active };
        var userId = Guid.NewGuid();
        var reason = "Security issue detected";

        deployment.Rollback(userId, reason);

        deployment.Status.Should().Be(PolicyDeploymentStatus.RolledBack);
        deployment.RolledBackBy.Should().Be(userId);
        deployment.RolledBackAt.Should().NotBeNull();
        deployment.RollbackReason.Should().Be(reason);
    }
}

public class PolicyRegistryAuditLogTests
{
    [Fact]
    public void PolicyRegistryAuditLog_DefaultValues_ShouldBeCorrect()
    {
        var log = new PolicyRegistryAuditLog();

        log.Id.Should().NotBeEmpty();
        log.BundleId.Should().BeNull();
        log.Details.Should().BeNull();
        log.IpAddress.Should().BeNull();
        log.UserAgent.Should().BeNull();
        log.Success.Should().BeTrue();
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void PolicyRegistryAuditLog_ShouldStoreAllProperties()
    {
        var bundleId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var log = new PolicyRegistryAuditLog
        {
            BundleId = bundleId,
            Action = PolicyRegistryAction.Deploy,
            PerformedBy = userId,
            Details = "Deployed to production",
            IpAddress = "192.168.1.1",
            UserAgent = "TestAgent/1.0",
            Success = false,
            ErrorMessage = "Timeout occurred"
        };

        log.BundleId.Should().Be(bundleId);
        log.Action.Should().Be(PolicyRegistryAction.Deploy);
        log.PerformedBy.Should().Be(userId);
        log.Details.Should().Be("Deployed to production");
        log.IpAddress.Should().Be("192.168.1.1");
        log.UserAgent.Should().Be("TestAgent/1.0");
        log.Success.Should().BeFalse();
        log.ErrorMessage.Should().Be("Timeout occurred");
    }
}
