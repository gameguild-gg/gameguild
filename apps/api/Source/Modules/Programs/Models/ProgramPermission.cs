using System.ComponentModel.DataAnnotations.Schema;
using GameGuild;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Programs;

/// <summary>
/// Resource-specific permissions for Program entities (Layer 3 of DAC permission system)
/// Provides granular permission control for individual programs
/// </summary>
[Table("ProgramPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true, Name = "IX_ProgramPermissions_User_Tenant_Resource")]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProgramPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProgramPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProgramPermissions_Expiration")]
public class ProgramPermission : ResourcePermission<Program> {
  /// <summary>
  /// Default parameterless constructor (required by Entity Framework and GraphQL)
  /// </summary>
  public ProgramPermission() : base() { }

  /// <summary>
  /// Constructor for creating instances (permissions added separately)
  /// </summary>
  public ProgramPermission(Guid userId, Guid? tenantId, Guid programId)
    : base(userId, tenantId, programId) {
  }

  public ProgramPermission(Guid userId, Guid? tenantId, Guid programId, PermissionType permissions)
    : base(userId, tenantId, programId) {
    AddPermission(permissions);
  }

  // Content Management Permissions
  /// <summary>
  /// Check if user can view this specific program's content
  /// </summary>
  public bool CanViewContent { get => HasPermission(PermissionType.Read) && !IsExpired; }

  /// <summary>
  /// Check if user can edit this specific program's content
  /// </summary>
  public bool CanEditContent { get => HasPermission(PermissionType.Edit) && !IsExpired; }

  /// <summary>
  /// Check if user can review this specific program's content
  /// </summary>
  public bool CanReviewContent { get => HasPermission(PermissionType.Review) && !IsExpired; }

  // Lifecycle Management Permissions
  /// <summary>
  /// Check if user can create drafts for this specific program
  /// </summary>
  public bool CanCreateDrafts { get => HasPermission(PermissionType.Draft) && !IsExpired; }

  /// <summary>
  /// Check if user can submit this specific program for review
  /// </summary>
  public bool CanSubmitForReview { get => HasPermission(PermissionType.Submit) && !IsExpired; }

  /// <summary>
  /// Check if user can archive this specific program
  /// </summary>
  public bool CanArchive { get => HasPermission(PermissionType.Archive) && !IsExpired; }

  /// <summary>
  /// Check if user can clone this specific program
  /// </summary>
  public bool CanClone { get => HasPermission(PermissionType.Clone) && !IsExpired; }

  /// <summary>
  /// Check if user can delete this specific program
  /// </summary>
  public bool CanDelete { get => HasPermission(PermissionType.Delete) && !IsExpired; }

  // User/Participant Management Permissions
  /// <summary>
  /// Check if user can manage participants in this specific program
  /// </summary>
  public bool CanManageUsers { get => HasPermission(PermissionType.Edit) && !IsExpired; }

  /// <summary>
  /// Check if user can view user progress for this specific program
  /// </summary>
  public bool CanViewUserProgress { get => HasPermission(PermissionType.Analytics) && !IsExpired; }

  /// <summary>
  /// Check if user can manage feedback for this specific program
  /// </summary>
  public bool CanManageFeedback { get => HasPermission(PermissionType.Feedback) && !IsExpired; }

  // Publishing Permissions
  /// <summary>
  /// Check if user can publish this specific program
  /// </summary>
  public bool CanPublish { get => HasPermission(PermissionType.Publish) && !IsExpired; }

  /// <summary>
  /// Check if user can unpublish this specific program
  /// </summary>
  public bool CanUnpublish { get => HasPermission(PermissionType.Unpublish) && !IsExpired; }

  /// <summary>
  /// Check if user can schedule publishing for this specific program
  /// </summary>
  public bool CanSchedule { get => HasPermission(PermissionType.Schedule) && !IsExpired; }

  // Monetization Permissions  
  /// <summary>
  /// Check if user can monetize this specific program
  /// </summary>
  public bool CanMonetize { get => HasPermission(PermissionType.Monetize) && !IsExpired; }

  /// <summary>
  /// Check if user can set pricing for this specific program
  /// </summary>
  public bool CanSetPricing { get => HasPermission(PermissionType.Pricing) && !IsExpired; }

  /// <summary>
  /// Check if user can add paywall to this specific program
  /// </summary>
  public bool CanAddPaywall { get => HasPermission(PermissionType.Paywall) && !IsExpired; }

  // Analytics & Performance Permissions
  /// <summary>
  /// Check if user can view analytics for this specific program
  /// </summary>
  public bool CanViewAnalytics { get => HasPermission(PermissionType.Analytics) && !IsExpired; }

  /// <summary>
  /// Check if user can view performance metrics for this specific program
  /// </summary>
  public bool CanViewPerformance { get => HasPermission(PermissionType.Performance) && !IsExpired; }

  // Approval Workflow Permissions
  /// <summary>
  /// Check if user can approve this specific program
  /// </summary>
  public bool CanApprove { get => HasPermission(PermissionType.Approve) && !IsExpired; }

  /// <summary>
  /// Check if user can reject this specific program
  /// </summary>
  public bool CanReject { get => HasPermission(PermissionType.Reject) && !IsExpired; }

  // Curation Permissions
  /// <summary>
  /// Check if user can categorize this specific program
  /// </summary>
  public bool CanCategorize { get => HasPermission(PermissionType.Categorize) && !IsExpired; }

  /// <summary>
  /// Check if user can add this program to collections
  /// </summary>
  public bool CanAddToCollection { get => HasPermission(PermissionType.Collection) && !IsExpired; }

  /// <summary>
  /// Check if user can create series with this specific program
  /// </summary>
  public bool CanCreateSeries { get => HasPermission(PermissionType.Series) && !IsExpired; }
}
