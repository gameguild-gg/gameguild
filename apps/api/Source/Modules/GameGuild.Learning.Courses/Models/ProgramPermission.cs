using GameGuild.Identity.Authentication;
using GameGuild.Identity.Authorization;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Resource-specific permissions for Program entities (Layer 3 of DAC permission system)
/// Provides granular permission control for individual programs
/// </summary>
[Table("ProgramPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true, Name = "IX_ProgramPermissions_User_Tenant_Resource")]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProgramPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProgramPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProgramPermissions_Expiration")]
public class ProgramPermission : ResourcePermission<Program>
{
    /// <summary>
    /// Default parameterless constructor (required by Entity Framework and GraphQL)
    /// </summary>
    public ProgramPermission() : base() { }

    /// <summary>
    /// Constructor for creating instances (permissions added separately)
    /// </summary>
    public ProgramPermission(Guid userId, Guid? tenantId, Guid programId)
        : base(userId, tenantId, programId)
    {
    }

    /// <summary>
    /// Constructor with initial permissions
    /// </summary>
    public ProgramPermission(Guid userId, Guid? tenantId, Guid programId, PermissionType permissions)
        : base(userId, tenantId, programId)
    {
        AddPermission(permissions);
    }

    // Content Management Permissions

    /// <summary>
    /// Check if user can view this specific program's content
    /// </summary>
    public bool CanViewContent => HasPermission(PermissionType.Read) && !IsExpired();

    /// <summary>
    /// Check if user can edit this specific program's content
    /// </summary>
    public bool CanEditContent => HasPermission(PermissionType.Edit) && !IsExpired();

    /// <summary>
    /// Check if user can review this specific program's content
    /// </summary>
    public bool CanReviewContent => HasPermission(PermissionType.Review) && !IsExpired();

    // Lifecycle Management Permissions

    /// <summary>
    /// Check if user can create drafts for this specific program
    /// </summary>
    public bool CanCreateDrafts => HasPermission(PermissionType.Draft) && !IsExpired();

    /// <summary>
    /// Check if user can submit this specific program for review
    /// </summary>
    public bool CanSubmitForReview => HasPermission(PermissionType.Submit) && !IsExpired();

    /// <summary>
    /// Check if user can archive this specific program
    /// </summary>
    public bool CanArchive => HasPermission(PermissionType.Archive) && !IsExpired();

    /// <summary>
    /// Check if user can clone this specific program
    /// </summary>
    public bool CanClone => HasPermission(PermissionType.Clone) && !IsExpired();

    /// <summary>
    /// Check if user can delete this specific program
    /// </summary>
    public bool CanDelete => HasPermission(PermissionType.Delete) && !IsExpired();

    // User/Participant Management Permissions

    /// <summary>
    /// Check if user can manage participants in this specific program
    /// </summary>
    public bool CanManageUsers => HasPermission(PermissionType.Edit) && !IsExpired();

    /// <summary>
    /// Check if user can view user progress for this specific program
    /// </summary>
    public bool CanViewUserProgress => HasPermission(PermissionType.Analytics) && !IsExpired();

    /// <summary>
    /// Check if user can manage feedback for this specific program
    /// </summary>
    public bool CanManageFeedback => HasPermission(PermissionType.Feedback) && !IsExpired();

    // Publishing Permissions

    /// <summary>
    /// Check if user can publish this specific program
    /// </summary>
    public bool CanPublish => HasPermission(PermissionType.Publish) && !IsExpired();

    /// <summary>
    /// Check if user can unpublish this specific program
    /// </summary>
    public bool CanUnpublish => HasPermission(PermissionType.Unpublish) && !IsExpired();

    /// <summary>
    /// Check if user can schedule publishing for this specific program
    /// </summary>
    public bool CanSchedule => HasPermission(PermissionType.Schedule) && !IsExpired();

    // Monetization Permissions

    /// <summary>
    /// Check if user can monetize this specific program
    /// </summary>
    public bool CanMonetize => HasPermission(PermissionType.Monetize) && !IsExpired();

    /// <summary>
    /// Check if user can set pricing for this specific program
    /// </summary>
    public bool CanSetPricing => HasPermission(PermissionType.Pricing) && !IsExpired();

    /// <summary>
    /// Check if user can add paywall to this specific program
    /// </summary>
    public bool CanAddPaywall => HasPermission(PermissionType.Paywall) && !IsExpired();

    // Analytics & Performance Permissions

    /// <summary>
    /// Check if user can view analytics for this specific program
    /// </summary>
    public bool CanViewAnalytics => HasPermission(PermissionType.Analytics) && !IsExpired();

    /// <summary>
    /// Check if user can view performance metrics for this specific program
    /// </summary>
    public bool CanViewPerformance => HasPermission(PermissionType.Performance) && !IsExpired();

    // Approval Workflow Permissions

    /// <summary>
    /// Check if user can approve this specific program
    /// </summary>
    public bool CanApprove => HasPermission(PermissionType.Approve) && !IsExpired();

    /// <summary>
    /// Check if user can reject this specific program
    /// </summary>
    public bool CanReject => HasPermission(PermissionType.Reject) && !IsExpired();

    // Curation Permissions

    /// <summary>
    /// Check if user can categorize this specific program
    /// </summary>
    public bool CanCategorize => HasPermission(PermissionType.Categorize) && !IsExpired();

    /// <summary>
    /// Check if user can add this program to collections
    /// </summary>
    public bool CanAddToCollection => HasPermission(PermissionType.Collection) && !IsExpired();

    /// <summary>
    /// Check if user can create series with this specific program
    /// </summary>
    public bool CanCreateSeries => HasPermission(PermissionType.Series) && !IsExpired();
}
