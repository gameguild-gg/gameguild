using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Permissions.Models;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Resource-specific permissions for Program entities (Layer 3 of DAC permission system)
/// Provides granular permission control for individual programs
/// </summary>
[Table("ProgramPermissions")]
[Index(
  nameof(UserId),
  nameof(TenantId),
  nameof(ResourceId),
  IsUnique = true,
  Name = "IX_ProgramPermissions_User_Tenant_Resource"
)]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProgramPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProgramPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProgramPermissions_Expiration")]
public class ProgramPermission : ResourcePermission<Program> {
  // Content Management Permissions
  /// <summary>
  /// Check if user can view this specific program's content
  /// </summary>
  public bool CanViewContent {
    get => HasPermission(PermissionType.Read) && IsValid;
  }

  /// <summary>
  /// Check if user can edit this specific program's content
  /// </summary>
  public bool CanEditContent {
    get => HasPermission(PermissionType.Edit) && IsValid;
  }

  /// <summary>
  /// Check if user can review this specific program's content
  /// </summary>
  public bool CanReviewContent {
    get => HasPermission(PermissionType.Review) && IsValid;
  }

  // Lifecycle Management Permissions
  /// <summary>
  /// Check if user can create drafts for this specific program
  /// </summary>
  public bool CanCreateDrafts {
    get => HasPermission(PermissionType.Draft) && IsValid;
  }

  /// <summary>
  /// Check if user can submit this specific program for review
  /// </summary>
  public bool CanSubmitForReview {
    get => HasPermission(PermissionType.Submit) && IsValid;
  }

  /// <summary>
  /// Check if user can archive this specific program
  /// </summary>
  public bool CanArchive {
    get => HasPermission(PermissionType.Archive) && IsValid;
  }

  /// <summary>
  /// Check if user can clone this specific program
  /// </summary>
  public bool CanClone {
    get => HasPermission(PermissionType.Clone) && IsValid;
  }

  /// <summary>
  /// Check if user can delete this specific program
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete) && IsValid;
  }

  // User/Participant Management Permissions
  /// <summary>
  /// Check if user can manage participants in this specific program
  /// </summary>
  public bool CanManageUsers {
    get => HasPermission(PermissionType.Edit) && IsValid;
  }

  /// <summary>
  /// Check if user can view user progress for this specific program
  /// </summary>
  public bool CanViewUserProgress {
    get => HasPermission(PermissionType.Analytics) && IsValid;
  }

  /// <summary>
  /// Check if user can manage feedback for this specific program
  /// </summary>
  public bool CanManageFeedback {
    get => HasPermission(PermissionType.Feedback) && IsValid;
  }

  // Publishing Permissions
  /// <summary>
  /// Check if user can publish this specific program
  /// </summary>
  public bool CanPublish {
    get => HasPermission(PermissionType.Publish) && IsValid;
  }

  /// <summary>
  /// Check if user can unpublish this specific program
  /// </summary>
  public bool CanUnpublish {
    get => HasPermission(PermissionType.Unpublish) && IsValid;
  }

  /// <summary>
  /// Check if user can schedule publishing for this specific program
  /// </summary>
  public bool CanSchedule {
    get => HasPermission(PermissionType.Schedule) && IsValid;
  }

  // Monetization Permissions  
  /// <summary>
  /// Check if user can monetize this specific program
  /// </summary>
  public bool CanMonetize {
    get => HasPermission(PermissionType.Monetize) && IsValid;
  }

  /// <summary>
  /// Check if user can set pricing for this specific program
  /// </summary>
  public bool CanSetPricing {
    get => HasPermission(PermissionType.Pricing) && IsValid;
  }

  /// <summary>
  /// Check if user can add paywall to this specific program
  /// </summary>
  public bool CanAddPaywall {
    get => HasPermission(PermissionType.Paywall) && IsValid;
  }

  // Analytics & Performance Permissions
  /// <summary>
  /// Check if user can view analytics for this specific program
  /// </summary>
  public bool CanViewAnalytics {
    get => HasPermission(PermissionType.Analytics) && IsValid;
  }

  /// <summary>
  /// Check if user can view performance metrics for this specific program
  /// </summary>
  public bool CanViewPerformance {
    get => HasPermission(PermissionType.Performance) && IsValid;
  }

  // Approval Workflow Permissions
  /// <summary>
  /// Check if user can approve this specific program
  /// </summary>
  public bool CanApprove {
    get => HasPermission(PermissionType.Approve) && IsValid;
  }

  /// <summary>
  /// Check if user can reject this specific program
  /// </summary>
  public bool CanReject {
    get => HasPermission(PermissionType.Reject) && IsValid;
  }

  // Curation Permissions
  /// <summary>
  /// Check if user can categorize this specific program
  /// </summary>
  public bool CanCategorize {
    get => HasPermission(PermissionType.Categorize) && IsValid;
  }

  /// <summary>
  /// Check if user can add this program to collections
  /// </summary>
  public bool CanAddToCollection {
    get => HasPermission(PermissionType.Collection) && IsValid;
  }

  /// <summary>
  /// Check if user can create series with this specific program
  /// </summary>
  public bool CanCreateSeries {
    get => HasPermission(PermissionType.Series) && IsValid;
  }
}
