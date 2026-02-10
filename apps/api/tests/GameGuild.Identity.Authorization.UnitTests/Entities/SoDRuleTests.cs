using FluentAssertions;
using GameGuild.CQRS.Models;
using Xunit;

namespace GameGuild.Identity.Authorization.UnitTests.Entities;

public class SoDRuleTests
{
    [Fact]
    public void SoDRule_DefaultValues_ShouldBeCorrect()
    {
        var rule = new SoDRule();

        rule.Id.Should().NotBeEmpty();
        rule.Name.Should().BeEmpty();
        rule.Description.Should().BeNull();
        rule.IsEnabled.Should().BeTrue();
        rule.ConflictingPermissions.Should().BeEmpty();
        rule.ConflictingRoles.Should().BeNull();
        rule.ConflictingResources.Should().BeNull();
        rule.AllowedExceptions.Should().BeNull();
        rule.RequireApproval.Should().BeFalse();
        rule.ApproverRoles.Should().BeNull();
        rule.MitigationStrategy.Should().BeNull();
        rule.ViolationCount.Should().Be(0);
        rule.LastViolationDetected.Should().BeNull();
        rule.Violations.Should().BeEmpty();
    }

    [Fact]
    public void IsActive_WhenEnabled_ShouldReturnTrue()
    {
        var rule = new SoDRule { IsEnabled = true };

        rule.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenDisabled_ShouldReturnFalse()
    {
        var rule = new SoDRule { IsEnabled = false };

        rule.IsActive().Should().BeFalse();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabledTrue()
    {
        var rule = new SoDRule { IsEnabled = false };

        rule.Enable();

        rule.IsEnabled.Should().BeTrue();
        rule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Disable_ShouldSetIsEnabledFalse()
    {
        var rule = new SoDRule { IsEnabled = true };

        rule.Disable();

        rule.IsEnabled.Should().BeFalse();
        rule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordViolation_ShouldIncrementCountAndSetTimestamp()
    {
        var rule = new SoDRule { ViolationCount = 5 };

        rule.RecordViolation();

        rule.ViolationCount.Should().Be(6);
        rule.LastViolationDetected.Should().NotBeNull();
        rule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void RecordViolation_CalledMultipleTimes_ShouldIncrementEachTime()
    {
        var rule = new SoDRule { ViolationCount = 0 };

        rule.RecordViolation();
        rule.RecordViolation();
        rule.RecordViolation();

        rule.ViolationCount.Should().Be(3);
    }

    [Theory]
    [InlineData(SoDSeverity.Critical, true)]
    [InlineData(SoDSeverity.High, false)]
    [InlineData(SoDSeverity.Medium, false)]
    [InlineData(SoDSeverity.Low, false)]
    [InlineData(SoDSeverity.None, false)]
    public void IsCritical_ShouldReturnCorrectResult(SoDSeverity severity, bool expected)
    {
        var rule = new SoDRule { Severity = severity };

        rule.IsCritical().Should().Be(expected);
    }

    [Theory]
    [InlineData(SoDSeverity.Critical, true)]
    [InlineData(SoDSeverity.High, true)]
    [InlineData(SoDSeverity.Medium, false)]
    [InlineData(SoDSeverity.Low, false)]
    [InlineData(SoDSeverity.None, false)]
    public void IsHighSeverity_ShouldReturnCorrectResult(SoDSeverity severity, bool expected)
    {
        var rule = new SoDRule { Severity = severity };

        rule.IsHighSeverity().Should().Be(expected);
    }

    [Fact]
    public void SoDRule_ShouldStoreAllProperties()
    {
        var tenantId = new TenantId(Guid.NewGuid());
        var createdBy = Guid.NewGuid();

        var rule = new SoDRule
        {
            TenantId = tenantId,
            Name = "Finance Conflict",
            Description = "Cannot have both approve and create",
            RuleType = SoDRuleType.PermissionConflict,
            Severity = SoDSeverity.High,
            ConflictingPermissions = "[\"Approve\", \"Create\"]",
            ConflictingRoles = "[\"Approver\", \"Creator\"]",
            ConflictingResources = "[\"Invoice\"]",
            AllowedExceptions = "[\"SuperAdmin\"]",
            RequireApproval = true,
            ApproverRoles = "[\"SecurityAdmin\"]",
            MitigationStrategy = "Implement dual control",
            CreatedBy = createdBy
        };

        rule.Name.Should().Be("Finance Conflict");
        rule.RuleType.Should().Be(SoDRuleType.PermissionConflict);
        rule.Severity.Should().Be(SoDSeverity.High);
        rule.RequireApproval.Should().BeTrue();
        rule.CreatedBy.Should().Be(createdBy);
    }
}

public class SoDViolationTests
{
    [Fact]
    public void SoDViolation_DefaultValues_ShouldBeCorrect()
    {
        var violation = new SoDViolation();

        violation.Id.Should().NotBeEmpty();
        violation.Status.Should().Be(SoDViolationStatus.Active);
        violation.ViolationDetails.Should().BeEmpty();
        violation.ConflictingItems.Should().BeEmpty();
        violation.DetectedBy.Should().BeNull();
        violation.ResolvedAt.Should().BeNull();
        violation.ResolvedBy.Should().BeNull();
        violation.ResolutionNotes.Should().BeNull();
        violation.ResolutionAction.Should().BeNull();
        violation.IsException.Should().BeFalse();
        violation.ExceptionJustification.Should().BeNull();
        violation.ApprovedBy.Should().BeNull();
        violation.ApprovedAt.Should().BeNull();
    }

    [Fact]
    public void IsActive_WhenStatusIsActive_ShouldReturnTrue()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Active };

        violation.IsActive().Should().BeTrue();
    }

    [Theory]
    [InlineData(SoDViolationStatus.Resolved)]
    [InlineData(SoDViolationStatus.Acknowledged)]
    [InlineData(SoDViolationStatus.Mitigated)]
    [InlineData(SoDViolationStatus.Excepted)]
    [InlineData(SoDViolationStatus.FalsePositive)]
    public void IsActive_WhenStatusIsNotActive_ShouldReturnFalse(SoDViolationStatus status)
    {
        var violation = new SoDViolation { Status = status };

        violation.IsActive().Should().BeFalse();
    }

    [Fact]
    public void IsResolved_WhenStatusIsResolved_ShouldReturnTrue()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Resolved };

        violation.IsResolved().Should().BeTrue();
    }

    [Fact]
    public void IsResolved_WhenStatusIsActive_ShouldReturnFalse()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Active };

        violation.IsResolved().Should().BeFalse();
    }

    [Fact]
    public void Resolve_ShouldSetStatusAndDetails()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Active };
        var resolvedBy = Guid.NewGuid();

        violation.Resolve(resolvedBy, SoDResolutionAction.RevokePermission, "Removed conflicting permission");

        violation.Status.Should().Be(SoDViolationStatus.Resolved);
        violation.ResolvedBy.Should().Be(resolvedBy);
        violation.ResolvedAt.Should().NotBeNull();
        violation.ResolutionAction.Should().Be(SoDResolutionAction.RevokePermission);
        violation.ResolutionNotes.Should().Be("Removed conflicting permission");
        violation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Resolve_WithNullNotes_ShouldWork()
    {
        var violation = new SoDViolation();
        var resolvedBy = Guid.NewGuid();

        violation.Resolve(resolvedBy, SoDResolutionAction.NoAction);

        violation.Status.Should().Be(SoDViolationStatus.Resolved);
        violation.ResolutionNotes.Should().BeNull();
    }

    [Fact]
    public void MarkAsException_ShouldSetExceptionDetails()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Active };
        var approvedBy = Guid.NewGuid();
        var justification = "CEO approved temporary access";

        violation.MarkAsException(approvedBy, justification);

        violation.Status.Should().Be(SoDViolationStatus.Excepted);
        violation.IsException.Should().BeTrue();
        violation.ApprovedBy.Should().Be(approvedBy);
        violation.ApprovedAt.Should().NotBeNull();
        violation.ExceptionJustification.Should().Be(justification);
        violation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void Acknowledge_ShouldSetStatusToAcknowledged()
    {
        var violation = new SoDViolation { Status = SoDViolationStatus.Active };

        violation.Acknowledge();

        violation.Status.Should().Be(SoDViolationStatus.Acknowledged);
        violation.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void SoDViolation_ShouldStoreAllProperties()
    {
        var ruleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var detectedBy = Guid.NewGuid();

        var violation = new SoDViolation
        {
            RuleId = ruleId,
            UserId = userId,
            ViolationDetails = "User has both Approve and Create permissions on Invoice",
            ConflictingItems = "[\"Approve\", \"Create\"]",
            DetectedBy = detectedBy
        };

        violation.RuleId.Should().Be(ruleId);
        violation.UserId.Should().Be(userId);
        violation.ViolationDetails.Should().Contain("Approve");
        violation.ConflictingItems.Should().Contain("Create");
        violation.DetectedBy.Should().Be(detectedBy);
    }
}
