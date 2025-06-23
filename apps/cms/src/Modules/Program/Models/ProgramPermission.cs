using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using GameGuild.Common.Entities;

namespace GameGuild.Modules.Program.Models;

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
    // Content Management Permissions
    /// <summary>
    /// Check if user can view this specific program's content
    /// </summary>
    public bool CanViewContent => HasPermission(PermissionType.Read) && IsValid;
    
    /// <summary>
    /// Check if user can edit this specific program's content
    /// </summary>
    public bool CanEditContent => HasPermission(PermissionType.Edit) && IsValid;
    
    /// <summary>
    /// Check if user can review this specific program's content
    /// </summary>
    public bool CanReviewContent => HasPermission(PermissionType.Review) && IsValid;
    
    // Lifecycle Management Permissions
    /// <summary>
    /// Check if user can create drafts for this specific program
    /// </summary>
    public bool CanCreateDrafts => HasPermission(PermissionType.Draft) && IsValid;
    
    /// <summary>
    /// Check if user can submit this specific program for review
    /// </summary>
    public bool CanSubmitForReview => HasPermission(PermissionType.Submit) && IsValid;
    
    /// <summary>
    /// Check if user can archive this specific program
    /// </summary>
    public bool CanArchive => HasPermission(PermissionType.Archive) && IsValid;
    
    /// <summary>
    /// Check if user can clone this specific program
    /// </summary>
    public bool CanClone => HasPermission(PermissionType.Clone) && IsValid;
    
    /// <summary>
    /// Check if user can delete this specific program
    /// </summary>
    public bool CanDelete => HasPermission(PermissionType.Delete) && IsValid;
    
    // User/Participant Management Permissions
    /// <summary>
    /// Check if user can manage participants in this specific program
    /// </summary>
    public bool CanManageUsers => HasPermission(PermissionType.Edit) && IsValid;
    
    /// <summary>
    /// Check if user can view user progress for this specific program
    /// </summary>
    public bool CanViewUserProgress => HasPermission(PermissionType.Analytics) && IsValid;
    
    /// <summary>
    /// Check if user can manage feedback for this specific program
    /// </summary>
    public bool CanManageFeedback => HasPermission(PermissionType.Feedback) && IsValid;
    
    // Publishing Permissions
    /// <summary>
    /// Check if user can publish this specific program
    /// </summary>
    public bool CanPublish => HasPermission(PermissionType.Publish) && IsValid;
    
    /// <summary>
    /// Check if user can unpublish this specific program
    /// </summary>
    public bool CanUnpublish => HasPermission(PermissionType.Unpublish) && IsValid;
    
    /// <summary>
    /// Check if user can schedule publishing for this specific program
    /// </summary>
    public bool CanSchedule => HasPermission(PermissionType.Schedule) && IsValid;
    
    // Monetization Permissions  
    /// <summary>
    /// Check if user can monetize this specific program
    /// </summary>
    public bool CanMonetize => HasPermission(PermissionType.Monetize) && IsValid;
    
    /// <summary>
    /// Check if user can set pricing for this specific program
    /// </summary>
    public bool CanSetPricing => HasPermission(PermissionType.Pricing) && IsValid;
    
    /// <summary>
    /// Check if user can add paywall to this specific program
    /// </summary>
    public bool CanAddPaywall => HasPermission(PermissionType.Paywall) && IsValid;
    
    // Analytics & Performance Permissions
    /// <summary>
    /// Check if user can view analytics for this specific program
    /// </summary>
    public bool CanViewAnalytics => HasPermission(PermissionType.Analytics) && IsValid;
    
    /// <summary>
    /// Check if user can view performance metrics for this specific program
    /// </summary>
    public bool CanViewPerformance => HasPermission(PermissionType.Performance) && IsValid;
    
    // Approval Workflow Permissions
    /// <summary>
    /// Check if user can approve this specific program
    /// </summary>
    public bool CanApprove => HasPermission(PermissionType.Approve) && IsValid;
    
    /// <summary>
    /// Check if user can reject this specific program
    /// </summary>
    public bool CanReject => HasPermission(PermissionType.Reject) && IsValid;
    
    // Curation Permissions
    /// <summary>
    /// Check if user can categorize this specific program
    /// </summary>
    public bool CanCategorize => HasPermission(PermissionType.Categorize) && IsValid;
    
    /// <summary>
    /// Check if user can add this program to collections
    /// </summary>
    public bool CanAddToCollection => HasPermission(PermissionType.Collection) && IsValid;
    
    /// <summary>
    /// Check if user can create series with this specific program
    /// </summary>
    public bool CanCreateSeries => HasPermission(PermissionType.Series) && IsValid;
}
