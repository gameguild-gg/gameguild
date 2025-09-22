using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Permissions;
using GameGuild.Modules.Resources;
using Microsoft.EntityFrameworkCore;

namespace GameGuild.Modules.Products;

/// <summary> Resource-specific permissions for Product entities (Layer 3 of DAC permission system) Provides granular permission control for individual products </summary>
[Table("ProductPermissions")]
[Index(nameof(UserId), nameof(TenantId), nameof(ResourceId), IsUnique = true, Name = "IX_ProductPermissions_User_Tenant_Resource")]
[Index(nameof(ResourceId), nameof(UserId), Name = "IX_ProductPermissions_Resource_User")]
[Index(nameof(TenantId), Name = "IX_ProductPermissions_TenantId")]
[Index(nameof(ExpiresAt), Name = "IX_ProductPermissions_Expiration")]
public class ProductPermission : ResourcePermission<Product> {
  // Public parameterless constructor for EF and GraphQL
  public ProductPermission() : base() { }

  // Public constructor for creating instances
  public ProductPermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions)
    : base(userId, tenantId, resourceId, permissions) {
  }
  // Product-specific computed properties

  /// <summary> Check if user can edit this specific product </summary>
  public bool CanEdit { get => HasPermission(PermissionType.Edit) && !IsExpired; }

  /// <summary> Check if user can delete this specific product </summary>
  public bool CanDelete { get => HasPermission(PermissionType.Delete) && !IsExpired; }

  /// <summary> Check if user can publish this specific product </summary>
  public bool CanPublish { get => HasPermission(PermissionType.Publish) && !IsExpired; }

  /// <summary> Check if user can manage pricing for this specific product </summary>
  public bool CanManagePricing { get => HasPermission(PermissionType.Pricing) && !IsExpired; }

  /// <summary> Check if user can manage subscriptions for this specific product </summary>
  public bool CanManageSubscriptions { get => HasPermission(PermissionType.Subscription) && !IsExpired; }

  /// <summary> Check if user can view sales analytics for this specific product </summary>
  public bool CanViewAnalytics { get => HasPermission(PermissionType.Analytics) && !IsExpired; }

  /// <summary> Check if user can manage promo codes for this specific product </summary>
  public bool CanManagePromoCodes { get => HasPermission(PermissionType.Monetize) && !IsExpired; }

  /// <summary> Check if user can grant access to this specific product </summary>
  public bool CanGrantAccess { get => HasPermission(PermissionType.Share) && !IsExpired; }

  /// <summary> Check if user can moderate content for this specific product </summary>
  public bool CanModerate { get => HasPermission(PermissionType.Review) && !IsExpired; }
}
