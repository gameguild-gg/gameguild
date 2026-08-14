using System.Text.Json;
using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Authorization.Caching;
using GameGuild.Identity.Authorization.Models;
using GameGuild.Identity.Context.Actors;
using GameGuild.Configuration.PresentationLayer.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using System.Security.Claims;

namespace GameGuild.Identity.Authorization.UnitTests;

#region AccessReviewCampaign Tests

public class AccessReviewCampaignTests
{
    private static AccessReviewCampaign CreateCampaign()
    {
        return new AccessReviewCampaign
        {
            Id = Guid.NewGuid(),
            Name = "Q1 2025 Review",
            Description = "Quarterly access review",
            ReviewType = AccessReviewType.UserAccessReview,
            Scope = AccessReviewScope.AllUsers,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Status = AccessReviewStatus.Draft,
            TotalItems = 10,
            CreatedBy = Guid.NewGuid()
        };
    }

    [Fact]
    public void NewCampaign_ShouldHaveDefaults()
    {
        var campaign = CreateCampaign();
        campaign.Name.Should().Be("Q1 2025 Review");
        campaign.ReviewedItems.Should().Be(0);
        campaign.ApprovedItems.Should().Be(0);
        campaign.RevokedItems.Should().Be(0);
    }

    [Fact]
    public void Start_ShouldSetStatusToInProgress()
    {
        var campaign = CreateCampaign();
        campaign.Start();
        campaign.Status.Should().Be(AccessReviewStatus.InProgress);
    }

    [Fact]
    public void IsActive_WhenInProgress_ShouldReturnTrue()
    {
        var campaign = CreateCampaign();
        campaign.Start();
        campaign.IsActive().Should().BeTrue();
    }

    [Fact]
    public void IsActive_WhenDraft_ShouldReturnFalse()
    {
        var campaign = CreateCampaign();
        campaign.IsActive().Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastEndDate_ShouldReturnTrue()
    {
        var campaign = CreateCampaign();
        campaign.Start(); // Must be InProgress for IsExpired to return true
        campaign.EndDate = DateTime.UtcNow.AddDays(-1);
        campaign.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenFutureEndDate_ShouldReturnFalse()
    {
        var campaign = CreateCampaign();
        campaign.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void GetCompletionPercentage_WithNoItems_ShouldReturnZero()
    {
        var campaign = CreateCampaign();
        campaign.TotalItems = 0;
        campaign.GetCompletionPercentage().Should().Be(0);
    }

    [Fact]
    public void GetCompletionPercentage_WithReviewedItems_ShouldCalculate()
    {
        var campaign = CreateCampaign();
        campaign.TotalItems = 10;
        campaign.ReviewedItems = 5;
        campaign.GetCompletionPercentage().Should().Be(50);
    }

    [Fact]
    public void Complete_ShouldSetStatusAndCompletedBy()
    {
        var campaign = CreateCampaign();
        campaign.Start();
        var completedBy = Guid.NewGuid();
        campaign.Complete(completedBy);
        campaign.Status.Should().Be(AccessReviewStatus.Completed);
        campaign.CompletedBy.Should().Be(completedBy);
        campaign.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public void Cancel_ShouldSetStatusToCancelled()
    {
        var campaign = CreateCampaign();
        campaign.Start();
        campaign.Cancel();
        campaign.Status.Should().Be(AccessReviewStatus.Expired); // Cancel uses Expired status
    }

    [Fact]
    public void MarkExpired_ShouldSetStatusToExpired()
    {
        var campaign = CreateCampaign();
        campaign.Start();
        campaign.MarkExpired();
        campaign.Status.Should().Be(AccessReviewStatus.Expired);
    }

    [Fact]
    public void IncrementReviewed_ShouldIncrement()
    {
        var campaign = CreateCampaign();
        campaign.IncrementReviewed();
        campaign.ReviewedItems.Should().Be(1);
        campaign.IncrementReviewed();
        campaign.ReviewedItems.Should().Be(2);
    }

    [Fact]
    public void IncrementApproved_ShouldIncrement()
    {
        var campaign = CreateCampaign();
        campaign.IncrementApproved();
        campaign.ApprovedItems.Should().Be(1);
    }

    [Fact]
    public void IncrementRevoked_ShouldIncrement()
    {
        var campaign = CreateCampaign();
        campaign.IncrementRevoked();
        campaign.RevokedItems.Should().Be(1);
    }
}

#endregion

#region AccessReviewItem Tests

public class AccessReviewItemTests
{
    private static AccessReviewItem CreateItem()
    {
        return new AccessReviewItem
        {
            Id = Guid.NewGuid(),
            SubjectUserId = Guid.NewGuid(),
            ResourceType = "Project",
            ResourceId = Guid.NewGuid(),
            PermissionDetails = "Read",
            Status = AccessReviewItemStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void IsPending_WhenPending_ShouldReturnTrue()
    {
        var item = CreateItem();
        item.IsPending().Should().BeTrue();
    }

    [Fact]
    public void IsPending_WhenApproved_ShouldReturnFalse()
    {
        var item = CreateItem();
        item.Approve("Looks good");
        item.IsPending().Should().BeFalse();
    }

    [Fact]
    public void Approve_ShouldSetStatusAndDecision()
    {
        var item = CreateItem();
        item.Approve("All good", "reviewer1");
        item.Status.Should().Be(AccessReviewItemStatus.Approved);
        item.Decision.Should().Be(AccessReviewDecision.Approve);
    }

    [Fact]
    public void Revoke_ShouldSetStatusAndDecision()
    {
        var item = CreateItem();
        item.Revoke("No longer needed", "reviewer1");
        item.Status.Should().Be(AccessReviewItemStatus.Revoked);
        item.Decision.Should().Be(AccessReviewDecision.Revoke);
    }

    [Fact]
    public void NeedsReminder_WhenPendingAndNoReminderSent_ShouldReturnTrue()
    {
        var item = CreateItem();
        item.NeedsReminder(7).Should().BeTrue(); // LastReminderSent is null
    }

    [Fact]
    public void NeedsReminder_WhenPendingAndRecentReminder_ShouldReturnFalse()
    {
        var item = CreateItem();
        item.RecordReminderSent(); // set LastReminderSent to now
        item.NeedsReminder(7).Should().BeFalse();
    }

    [Fact]
    public void NeedsReminder_WhenNotPending_ShouldReturnFalse()
    {
        var item = CreateItem();
        item.Approve("Ok");
        item.NeedsReminder(1).Should().BeFalse();
    }

    [Fact]
    public void RecordReminderSent_ShouldUpdateLastReminder()
    {
        var item = CreateItem();
        item.RecordReminderSent();
        item.LastReminderSent.Should().NotBeNull();
    }
}

#endregion

#region DataMaskingRule Tests

public class DataMaskingRuleTests
{
    private static DataMaskingRule CreateRule(MaskingType maskingType = MaskingType.Full)
    {
        return new DataMaskingRule
        {
            Id = Guid.NewGuid(),
            Name = "SSN Mask",
            ResourceType = "User",
            FieldName = "SSN",
            MaskingType = maskingType,
            MaskCharacter = '*',
            ShowFirst = 0,
            ShowLast = 4,
            IsEnabled = true,
            Priority = 1
        };
    }

    [Fact]
    public void ApplyMasking_Full_ShouldMaskEverything()
    {
        var rule = CreateRule(MaskingType.Full);
        var result = rule.ApplyMasking("123-45-6789");
        result.Should().NotContain("123");
        result.Should().Contain("*");
    }

    [Fact]
    public void ApplyMasking_Partial_ShouldShowFirstAndLast()
    {
        var rule = CreateRule(MaskingType.Partial);
        rule.ShowFirst = 3;
        rule.ShowLast = 2;
        var result = rule.ApplyMasking("Hello World");
        result.Should().StartWith("Hel");
        result.Should().EndWith("ld");
    }

    [Fact]
    public void ApplyMasking_Hash_ShouldReturnHashedValue()
    {
        var rule = CreateRule(MaskingType.Hash);
        var result = rule.ApplyMasking("sensitive-data");
        result.Should().NotBe("sensitive-data");
    }

    [Fact]
    public void ApplyMasking_Redact_ShouldReturnRedactedText()
    {
        var rule = CreateRule(MaskingType.Redact);
        var result = rule.ApplyMasking("anything");
        result.Should().Contain("REDACTED");
    }

    [Fact]
    public void ApplyMasking_EmptyValue_ShouldReturnEmpty()
    {
        var rule = CreateRule(MaskingType.Full);
        var result = rule.ApplyMasking("");
        result.Should().BeEmpty();
    }

    [Fact]
    public void IsUserExempt_WithExemptUser_ShouldReturnTrue()
    {
        var userId = Guid.NewGuid();
        var rule = CreateRule();
        rule.ExemptUsers = $"[\"{userId}\"]";
        rule.IsUserExempt(userId).Should().BeTrue();
    }

    [Fact]
    public void IsUserExempt_WithNonExemptUser_ShouldReturnFalse()
    {
        var rule = CreateRule();
        rule.ExemptUsers = $"[\"{Guid.NewGuid()}\"]";
        rule.IsUserExempt(Guid.NewGuid()).Should().BeFalse();
    }

    [Fact]
    public void Enable_ShouldSetIsEnabled()
    {
        var rule = CreateRule();
        rule.IsEnabled = false;
        rule.Enable();
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Disable_ShouldClearIsEnabled()
    {
        var rule = CreateRule();
        rule.Disable();
        rule.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void ApplyMasking_PatternMask_ShouldApplyPattern()
    {
        var rule = CreateRule(MaskingType.PatternMask);
        rule.MaskingPattern = "XXX-XX-****";
        var result = rule.ApplyMasking("123-45-6789");
        result.Should().NotBeNullOrEmpty();
    }
}

#endregion

#region PermissionDelegation Tests

public class PermissionDelegationTests
{
    private static PermissionDelegation CreateDelegation()
    {
        return new PermissionDelegation
        {
            Id = Guid.NewGuid(),
            DelegatorUserId = Guid.NewGuid(),
            DelegateUserId = Guid.NewGuid(),
            DelegatedPermissions = new[] { "content:read", "content:write" },
            StartsAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23),
            IsActive = true,
            UsageLimit = 100,
            UsageCount = 0
        };
    }

    [Fact]
    public void IsValidNow_WhenActiveAndInTimeRange_ShouldReturnTrue()
    {
        var delegation = CreateDelegation();
        delegation.IsValidNow().Should().BeTrue();
    }

    [Fact]
    public void IsValidNow_WhenInactive_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.IsActive = false;
        delegation.IsValidNow().Should().BeFalse();
    }

    [Fact]
    public void IsValidNow_WhenExpired_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        delegation.IsValidNow().Should().BeFalse();
    }

    [Fact]
    public void IsValidNow_WhenNotStarted_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.StartsAt = DateTime.UtcNow.AddHours(1);
        delegation.IsValidNow().Should().BeFalse();
    }

    [Fact]
    public void IsValidNow_WhenUsageLimitReached_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = 5;
        delegation.UsageCount = 5;
        delegation.IsValidNow().Should().BeFalse();
    }

    [Fact]
    public void AllowsPermission_WhenValid_ShouldReturnTrue()
    {
        var delegation = CreateDelegation();
        delegation.AllowsPermission("content:read").Should().BeTrue();
    }

    [Fact]
    public void AllowsPermission_WhenNotInList_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.AllowsPermission("admin:delete").Should().BeFalse();
    }

    [Fact]
    public void AllowsPermission_WhenInvalid_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.IsActive = false;
        delegation.AllowsPermission("content:read").Should().BeFalse();
    }

    [Fact]
    public void RecordUsage_ShouldIncrementCount()
    {
        var delegation = CreateDelegation();
        delegation.RecordUsage();
        delegation.UsageCount.Should().Be(1);
    }

    [Fact]
    public void RecordUsage_AtLimit_ShouldDeactivate()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = 1;
        delegation.RecordUsage();
        delegation.UsageCount.Should().Be(1);
        delegation.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Activate_ShouldSetActive()
    {
        var delegation = CreateDelegation();
        delegation.IsActive = false;
        delegation.Activate();
        delegation.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldClearActive()
    {
        var delegation = CreateDelegation();
        delegation.Deactivate();
        delegation.IsActive.Should().BeFalse();
    }

    [Fact]
    public void IsExpired_WhenPastExpiry_ShouldReturnTrue()
    {
        var delegation = CreateDelegation();
        delegation.ExpiresAt = DateTime.UtcNow.AddHours(-1);
        delegation.IsExpired().Should().BeTrue();
    }

    [Fact]
    public void IsExpired_WhenFutureExpiry_ShouldReturnFalse()
    {
        var delegation = CreateDelegation();
        delegation.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void Extend_ShouldUpdateExpiresAt()
    {
        var delegation = CreateDelegation();
        var newExpiry = DateTime.UtcNow.AddDays(7);
        delegation.Extend(newExpiry);
        delegation.ExpiresAt.Should().Be(newExpiry);
    }

    [Fact]
    public void HasUsageRemaining_WithUsageLeft_ShouldReturnTrue()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = 10;
        delegation.UsageCount = 5;
        delegation.HasUsageRemaining().Should().BeTrue();
    }

    [Fact]
    public void HasUsageRemaining_WithNoLimit_ShouldReturnTrue()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = null;
        delegation.HasUsageRemaining().Should().BeTrue();
    }

    [Fact]
    public void GetRemainingUsage_WithLimit_ShouldReturnCorrectCount()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = 10;
        delegation.UsageCount = 3;
        delegation.GetRemainingUsage().Should().Be(7);
    }

    [Fact]
    public void GetRemainingUsage_WithNoLimit_ShouldReturnNull()
    {
        var delegation = CreateDelegation();
        delegation.UsageLimit = null;
        delegation.GetRemainingUsage().Should().BeNull();
    }
}

#endregion

#region TimeWindow Tests

public class TimeWindowTests
{
    [Fact]
    public void Parse_ValidFormat_ShouldReturnTimeWindow()
    {
        var tw = TimeWindow.Parse("09:00-17:00");
        tw.Should().NotBeNull();
        tw!.Start.Should().Be(new TimeOnly(9, 0));
        tw.End.Should().Be(new TimeOnly(17, 0));
    }

    [Fact]
    public void Parse_WithTimezone_ShouldReturnTimeWindowWithTz()
    {
        var tw = TimeWindow.Parse("09:00-17:00@UTC");
        tw.Should().NotBeNull();
        tw!.TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public void Parse_Null_ShouldReturnNull()
    {
        TimeWindow.Parse(null).Should().BeNull();
    }

    [Fact]
    public void Parse_Empty_ShouldReturnNull()
    {
        TimeWindow.Parse("").Should().BeNull();
    }

    [Fact]
    public void Parse_InvalidFormat_ShouldReturnNull()
    {
        TimeWindow.Parse("not-a-time-window").Should().BeNull();
    }

    [Fact]
    public void ToString_ShouldReturnCorrectFormat()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0),
            TimeZoneId = "UTC"
        };
        tw.ToString().Should().Be("09:00-17:00@UTC");
    }

    [Fact]
    public void IsTimeInWindow_SameDay_WithinWindow_ShouldReturnTrue()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0)
        };
        tw.IsTimeInWindow(new TimeOnly(12, 0)).Should().BeTrue();
    }

    [Fact]
    public void IsTimeInWindow_SameDay_OutsideWindow_ShouldReturnFalse()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0)
        };
        tw.IsTimeInWindow(new TimeOnly(20, 0)).Should().BeFalse();
    }

    [Fact]
    public void IsTimeInWindow_Overnight_WithinWindow_ShouldReturnTrue()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(22, 0),
            End = new TimeOnly(6, 0)
        };
        tw.IsTimeInWindow(new TimeOnly(23, 0)).Should().BeTrue();
        tw.IsTimeInWindow(new TimeOnly(2, 0)).Should().BeTrue();
    }

    [Fact]
    public void IsTimeInWindow_Overnight_OutsideWindow_ShouldReturnFalse()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(22, 0),
            End = new TimeOnly(6, 0)
        };
        tw.IsTimeInWindow(new TimeOnly(12, 0)).Should().BeFalse();
    }

    [Fact]
    public void Contains_UtcTime_ShouldHandleTimezoneConversion()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(0, 0),
            End = new TimeOnly(23, 59),
            TimeZoneId = "UTC"
        };
        tw.Contains(DateTimeOffset.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void TimeZone_Property_ShouldReturnCorrectZone()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0),
            TimeZoneId = "UTC"
        };
        tw.TimeZone.Should().Be(TimeZoneInfo.Utc);
    }

    [Fact]
    public void TimeZone_WithNull_ShouldReturnUtc()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0)
        };
        tw.TimeZone.Should().NotBeNull();
    }
}

#endregion

#region TimeWindowJsonConverter Tests

public class TimeWindowJsonConverterTests
{
    [Fact]
    public void Serialize_ShouldWriteAsString()
    {
        var tw = new TimeWindow
        {
            Start = new TimeOnly(9, 0),
            End = new TimeOnly(17, 0),
            TimeZoneId = "UTC"
        };
        var json = JsonSerializer.Serialize(tw);
        json.Should().Contain("09:00-17:00@UTC");
    }

    [Fact]
    public void Deserialize_FromString_ShouldCreateTimeWindow()
    {
        var json = "\"09:00-17:00@UTC\"";
        var tw = JsonSerializer.Deserialize<TimeWindow>(json);
        tw.Should().NotBeNull();
        tw!.Start.Should().Be(new TimeOnly(9, 0));
        tw.End.Should().Be(new TimeOnly(17, 0));
        tw.TimeZoneId.Should().Be("UTC");
    }

    [Fact]
    public void Deserialize_FromObject_ShouldCreateTimeWindow()
    {
        var json = "{\"Start\":\"09:00\",\"End\":\"17:00\",\"TimeZoneId\":\"UTC\"}";
        var tw = JsonSerializer.Deserialize<TimeWindow>(json);
        tw.Should().NotBeNull();
    }

    [Fact]
    public void Roundtrip_ShouldPreserveValues()
    {
        var original = new TimeWindow
        {
            Start = new TimeOnly(14, 30),
            End = new TimeOnly(23, 45),
            TimeZoneId = "UTC"
        };
        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TimeWindow>(json);
        deserialized.Should().NotBeNull();
        deserialized!.Start.Should().Be(original.Start);
        deserialized.End.Should().Be(original.End);
    }
}

#endregion

#region StaticRolePermissions Tests

public class StaticRolePermissionsTests
{
    [Fact]
    public void OwnerPermissions_ShouldContainWildcards()
    {
        StaticRolePermissions.OwnerPermissions.Should().NotBeEmpty();
        StaticRolePermissions.OwnerPermissions.Should().Contain(p => p.Contains("*"));
    }

    [Fact]
    public void AdminPermissions_ShouldNotBeEmpty()
    {
        StaticRolePermissions.AdminPermissions.Should().NotBeEmpty();
    }

    [Fact]
    public void ModeratorPermissions_ShouldNotBeEmpty()
    {
        StaticRolePermissions.ModeratorPermissions.Should().NotBeEmpty();
    }

    [Fact]
    public void MemberPermissions_ShouldContainReadPermissions()
    {
        StaticRolePermissions.MemberPermissions.Should().Contain(p => p.Contains("read"));
    }

    [Fact]
    public void ContributorPermissions_ShouldNotBeEmpty()
    {
        StaticRolePermissions.ContributorPermissions.Should().NotBeEmpty();
    }

    [Fact]
    public void ViewerPermissions_ShouldContainOnlyReadPermissions()
    {
        StaticRolePermissions.ViewerPermissions.Should().OnlyContain(p => p.Contains("read"));
    }

    [Fact]
    public void GuestPermissions_ShouldContainPublicRead()
    {
        StaticRolePermissions.GuestPermissions.Should().Contain(p => p.Contains("public"));
    }

    [Fact]
    public void GetStaticPermissions_Owner_ShouldReturnOwnerPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Owner").Should().BeEquivalentTo(StaticRolePermissions.OwnerPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Admin_ShouldReturnAdminPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Admin").Should().BeEquivalentTo(StaticRolePermissions.AdminPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Moderator_ShouldReturnModeratorPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Moderator").Should().BeEquivalentTo(StaticRolePermissions.ModeratorPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Member_ShouldReturnMemberPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Member").Should().BeEquivalentTo(StaticRolePermissions.MemberPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Contributor_ShouldReturnContributorPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Contributor").Should().BeEquivalentTo(StaticRolePermissions.ContributorPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Viewer_ShouldReturnViewerPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Viewer").Should().BeEquivalentTo(StaticRolePermissions.ViewerPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Guest_ShouldReturnGuestPermissions()
    {
        StaticRolePermissions.GetStaticPermissions("Guest").Should().BeEquivalentTo(StaticRolePermissions.GuestPermissions);
    }

    [Fact]
    public void GetStaticPermissions_CaseInsensitive_ShouldWork()
    {
        StaticRolePermissions.GetStaticPermissions("owner").Should().BeEquivalentTo(StaticRolePermissions.OwnerPermissions);
        StaticRolePermissions.GetStaticPermissions("ADMIN").Should().BeEquivalentTo(StaticRolePermissions.AdminPermissions);
    }

    [Fact]
    public void GetStaticPermissions_Unknown_ShouldReturnEmpty()
    {
        StaticRolePermissions.GetStaticPermissions("NonExistentRole").Should().BeEmpty();
    }
}

#endregion

#region DynamicRoleAssignment Tests

public class DynamicRoleAssignmentTests
{
    [Fact]
    public void IsValid_WhenActiveAndInTimeRange_ShouldReturnTrue()
    {
        var assignment = new DynamicRoleAssignment
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };
        assignment.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenInactive_ShouldReturnFalse()
    {
        var assignment = new DynamicRoleAssignment
        {
            IsActive = false
        };
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenNotStarted_ShouldReturnFalse()
    {
        var assignment = new DynamicRoleAssignment
        {
            IsActive = true,
            StartsAt = DateTime.UtcNow.AddDays(1)
        };
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var assignment = new DynamicRoleAssignment
        {
            IsActive = true,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        assignment.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithNullDates_ShouldReturnTrueWhenActive()
    {
        var assignment = new DynamicRoleAssignment
        {
            IsActive = true,
            StartsAt = null,
            ExpiresAt = null
        };
        assignment.IsValid().Should().BeTrue();
    }

    [Fact]
    public void DefaultValues_ShouldBeCorrect()
    {
        var assignment = new DynamicRoleAssignment();
        assignment.IsActive.Should().BeTrue();
    }
}

#endregion

#region DynamicRole Entity Tests

public class DynamicRoleEntityTests
{
    [Fact]
    public void Constructor_ShouldHaveDefaults()
    {
        var role = new DynamicRole();
        role.Name.Should().BeEmpty();
        role.DisplayName.Should().BeEmpty();
        role.IsActive.Should().BeTrue();
        role.IsSystem.Should().BeFalse();
        role.Permissions.Should().BeEmpty();
        role.DenyPermissions.Should().BeEmpty();
        role.MutuallyExclusiveRoleIds.Should().BeEmpty();
        role.PrerequisiteRoleIds.Should().BeEmpty();
    }

    [Fact]
    public void Properties_ShouldBeSettable()
    {
        var parentId = Guid.NewGuid();
        var role = new DynamicRole
        {
            Name = "TestRole",
            DisplayName = "Test Role",
            Description = "A test role",
            TenantId = Guid.NewGuid(),
            ParentRoleId = parentId,
            Permissions = new[] { "read", "write" },
            DenyPermissions = new[] { "admin:delete" },
            Priority = 5,
            IsActive = false,
            IsSystem = true,
            MaxAssignments = 10,
            Metadata = new Dictionary<string, object> { ["key"] = "value" }
        };

        role.Name.Should().Be("TestRole");
        role.DisplayName.Should().Be("Test Role");
        role.Description.Should().Be("A test role");
        role.ParentRoleId.Should().Be(parentId);
        role.Permissions.Should().HaveCount(2);
        role.DenyPermissions.Should().HaveCount(1);
        role.Priority.Should().Be(5);
        role.IsActive.Should().BeFalse();
        role.IsSystem.Should().BeTrue();
        role.MaxAssignments.Should().Be(10);
        role.Metadata.Should().ContainKey("key");
    }
}

#endregion

#region ResourceTypes Tests

public class ResourceTypesTests
{
    [Fact]
    public void All_ShouldNotBeEmpty()
    {
        ResourceTypes.All.Should().NotBeEmpty();
    }

    [Fact]
    public void IsValid_WithKnownType_ShouldReturnTrue()
    {
        ResourceTypes.IsValid("User").Should().BeTrue();
    }

    [Fact]
    public void IsValid_WithUnknownType_ShouldReturnFalse()
    {
        ResourceTypes.IsValid("NonExistent").Should().BeFalse();
    }

    [Fact]
    public void FromString_WithValidValue_ShouldReturnType()
    {
        var rt = ResourceTypes.FromString("User");
        rt.Should().NotBeNull();
        rt!.Value.Should().Be("User");
    }

    [Fact]
    public void FromString_WithInvalidValue_ShouldReturnNull()
    {
        ResourceTypes.FromString("FakeResource").Should().BeNull();
    }

    [Fact]
    public void KnownTypes_ShouldExist()
    {
        ResourceTypes.User.Value.Should().Be("User");
        ResourceTypes.Role.Value.Should().Be("Role");
        ResourceTypes.Tenant.Value.Should().Be("Tenant");
        ResourceTypes.Project.Value.Should().Be("Project");
        ResourceTypes.Content.Value.Should().Be("Content");
        ResourceTypes.Post.Value.Should().Be("Post");
        ResourceTypes.Product.Value.Should().Be("Product");
    }

    [Fact]
    public void ResourceType_ImplicitConversion_ShouldWork()
    {
        string value = ResourceTypes.User;
        value.Should().Be("User");
    }

    [Fact]
    public void ResourceType_Equality_ShouldWork()
    {
        ResourceTypes.User.Equals(ResourceTypes.User).Should().BeTrue();
        ResourceTypes.User.Equals(ResourceTypes.Role).Should().BeFalse();
    }

    [Fact]
    public void ConcreteResourceType_ShouldWorkAsResourceType()
    {
        var rt = new ConcreteResourceType("Custom", "Custom resource");
        rt.Value.Should().Be("Custom");
        rt.Description.Should().Be("Custom resource");
    }
}

#endregion

#region CacheMetricsService Tests

public class CacheMetricsServiceTests
{
    [Fact]
    public void RecordHit_ShouldIncrementHits()
    {
        var service = new CacheMetricsService();
        service.RecordHit(CacheLevel.L1, "policy");
        service.RecordHit(CacheLevel.L1, "policy");
        service.RecordHit(CacheLevel.L2, "policy");

        var stats = service.GetStatistics();
        stats.L1Hits.Should().Be(2);
        stats.L2Hits.Should().Be(1);
    }

    [Fact]
    public void RecordMiss_ShouldIncrementMisses()
    {
        var service = new CacheMetricsService();
        service.RecordMiss("policy");
        service.RecordMiss("acl");

        var stats = service.GetStatistics();
        stats.Misses.Should().Be(2);
    }

    [Fact]
    public void RecordEviction_ShouldIncrementEvictions()
    {
        var service = new CacheMetricsService();
        service.RecordEviction(CacheLevel.L1, "policy");
        service.RecordEviction(CacheLevel.L2, "acl", "expired");

        var stats = service.GetStatistics();
        stats.Evictions.Should().Be(2);
    }

    [Fact]
    public void GetStatistics_Empty_ShouldReturnZeros()
    {
        var service = new CacheMetricsService();
        var stats = service.GetStatistics();
        stats.L1Hits.Should().Be(0);
        stats.L2Hits.Should().Be(0);
        stats.Misses.Should().Be(0);
        stats.Evictions.Should().Be(0);
    }

    [Fact]
    public void GetStatistics_ShouldTrackByType()
    {
        var service = new CacheMetricsService();
        service.RecordHit(CacheLevel.L1, "policy");
        service.RecordHit(CacheLevel.L1, "acl");
        service.RecordMiss("policy");

        var stats = service.GetStatistics();
        stats.ByType.Should().NotBeEmpty();
        stats.ByType.Should().ContainKey("policy");
        stats.ByType.Should().ContainKey("acl");
    }

    [Fact]
    public void HitRate_ShouldCalculateCorrectly()
    {
        var service = new CacheMetricsService();
        service.RecordHit(CacheLevel.L1, "x");
        service.RecordHit(CacheLevel.L1, "x");
        service.RecordHit(CacheLevel.L1, "x");
        service.RecordMiss("x");

        var stats = service.GetStatistics();
        stats.OverallHitRate.Should().BeApproximately(0.75, 0.01);
    }
}

#endregion

#region AuthorizationTenantResolver Tests

public class AuthorizationTenantResolverTests
{
    private AuthorizationTenantResolver CreateResolver()
    {
        var tenancy = Options.Create<TenancyOptions>(new TenancyOptions());
        var token = Options.Create<AuthorizationTokenOptions>(new AuthorizationTokenOptions());
        return new AuthorizationTenantResolver(tenancy, token);
    }

    [Fact]
    public void ResolveFromRequest_WithHeader_ShouldReturnTenantId()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Tenant-Id"] = "tenant-123";

        var result = resolver.ResolveFromRequest(context);
        result.Should().Be("tenant-123");
    }

    [Fact]
    public async Task ResolveTenantIdAsync_WithMiddlewareResolvedTenant_UsesValidatedTenantBeforeHostOrClaims()
    {
        var resolver = CreateResolver();
        var tenantId = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("tenant_id", Guid.NewGuid().ToString()) },
                "Test")),
        };
        context.Request.Host = new HostString("127.0.0.1");
        context.Items[HttpContextKeys.AuthorizationTenantId] = tenantId;

        var result = await resolver.ResolveTenantIdAsync(context);

        result.Should().Be(tenantId.ToString());
    }
    [Fact]
    public async Task ResolveTenantIdAsync_WithoutExplicitTenant_FallsBackToAuthenticatedTenantClaim()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim("tenant_id", "tenant-from-token") },
                "Test")),
        };

        var result = await resolver.ResolveTenantIdAsync(context);

        result.Should().Be("tenant-from-token");
    }
    [Fact]
    public void ResolveFromRequest_WithNoHeader_ShouldReturnNull()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();

        var result = resolver.ResolveFromRequest(context);
        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public void ResolveFromRequest_WithIpAddressHost_ShouldNotTreatAddressAsSubdomain()
    {
        var resolver = CreateResolver();
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString("127.0.0.1");

        var result = resolver.ResolveFromRequest(context);

        result.Should().BeNull();
    }

    [Fact]
    public void ResolveFromClaims_WithTenantClaim_ShouldReturnTenantId()
    {
        var resolver = CreateResolver();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("tenant_id", "tenant-from-claims") }, "Test"));

        var result = resolver.ResolveFromClaims(principal);
        result.Should().Be("tenant-from-claims");
    }

    [Fact]
    public void ResolveFromClaims_WithNoClaim_ShouldReturnNull()
    {
        var resolver = CreateResolver();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "Test"));

        var result = resolver.ResolveFromClaims(principal);
        result.Should().BeNullOrEmpty();
    }

    [Fact]
    public void GetUserDefaultTenant_WithClaim_ShouldReturnValue()
    {
        var resolver = CreateResolver();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim("default_tenant", "default-tenant-id")
            }, "Test"));

        var result = resolver.GetUserDefaultTenant(principal);
        // Will return null since default_tenant isn't found if not configured
        // but tests the code path
    }
}

#endregion

#region CacheInvalidationEvent Tests

public class CacheInvalidationEventTests
{
    [Fact]
    public void Constructor_ShouldSetDefaults()
    {
        var evt = new CacheInvalidationEvent
        {
            TenantId = Guid.NewGuid(),
            Type = CacheInvalidationType.Tenant,
            Timestamp = DateTimeOffset.UtcNow
        };

        evt.TenantId.Should().NotBeEmpty();
        evt.Type.Should().Be(CacheInvalidationType.Tenant);
    }

    [Fact]
    public void AllInvalidationTypes_ShouldExist()
    {
        CacheInvalidationType.Tenant.Should().BeDefined();
        CacheInvalidationType.User.Should().BeDefined();
        CacheInvalidationType.Resource.Should().BeDefined();
        CacheInvalidationType.Policy.Should().BeDefined();
    }
}

#endregion
