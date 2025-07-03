using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common.Entities;
using GameGuild.Modules.Permissions.Models;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Products.Models;

/// <summary>
/// Resource-specific permissions for Product entities (Layer 3 of DAC permission system)
/// Provides granular permission control for individual products
/// </summary>
[Table("ProductPermissions")]
[Index(
  nameof(UserId),
  nameof(TenantId),
  nameof(ResourceId),
  IsUnique = true,
  Name = "IX_ProductPermissions_User_Tenant_Resource"
)]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProductPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProductPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProductPermissions_Expiration")]
public class ProductPermission : ResourcePermission<Product> {
  // Product-specific computed properties

  /// <summary>
  /// Check if user can edit this specific product
  /// </summary>
  public bool CanEdit {
    get => HasPermission(PermissionType.Edit) && IsValid;
  }

  /// <summary>
  /// Check if user can delete this specific product
  /// </summary>
  public bool CanDelete {
    get => HasPermission(PermissionType.Delete) && IsValid;
  }

  /// <summary>
  /// Check if user can publish this specific product
  /// </summary>
  public bool CanPublish {
    get => HasPermission(PermissionType.Publish) && IsValid;
  }

  /// <summary>
  /// Check if user can manage pricing for this specific product
  /// </summary>
  public bool CanManagePricing {
    get => HasPermission(PermissionType.Pricing) && IsValid;
  }

  /// <summary>
  /// Check if user can manage subscriptions for this specific product
  /// </summary>
  public bool CanManageSubscriptions {
    get => HasPermission(PermissionType.Subscription) && IsValid;
  }

  /// <summary>
  /// Check if user can view sales analytics for this specific product
  /// </summary>
  public bool CanViewAnalytics {
    get => HasPermission(PermissionType.Analytics) && IsValid;
  }

  /// <summary>
  /// Check if user can manage promo codes for this specific product
  /// </summary>
  public bool CanManagePromoCodes {
    get => HasPermission(PermissionType.Monetize) && IsValid;
  }

  /// <summary>
  /// Check if user can grant access to other users for this specific product
  /// </summary>
  public bool CanGrantAccess {
    get => HasPermission(PermissionType.Share) && IsValid;
  }

  /// <summary>
  /// Check if user can moderate this product (e.g., handle reports, moderate reviews)
  /// </summary>
  public bool CanModerate {
    get => HasPermission(PermissionType.Review) && IsValid;
  }
}
