using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;


// do not remove this, it's needed for IQueryable extensions

// do not remove this, it's needed for IQueryable extensions

namespace GameGuild.Modules.Permissions.Models;

// this is a base permission model that can be used to store permissions for any entity
[ObjectType]
public class WithPermissions : Entity {
  [GraphQLType(typeof(NonNullType<LongType>))]
  [GraphQLDescription("Permission flags for bits 0-63")]
  [Column(TypeName = "bigint")]
  public ulong PermissionFlags1 { get; set; }

  [GraphQLType(typeof(NonNullType<LongType>))]
  [GraphQLDescription("Permission flags for bits 64-127")]
  [Column(TypeName = "bigint")]
  public ulong PermissionFlags2 { get; set; }

  /// <summary>
  /// User relationship - NULL means default permissions
  /// </summary>
  [GraphQLType(typeof(UuidType))]
  [GraphQLDescription("The user ID this permission applies to (null for default permissions)")]
  public Guid? UserId { get; set; }

  /// <summary>
  /// Navigation property to the User entity
  /// </summary>
  [GraphQLIgnore]
  [ForeignKey(nameof(UserId))]
  public virtual User? User { get; set; }

  /// <summary>
  /// Tenant relationship - NULL means global defaults
  /// </summary>
  [GraphQLType(typeof(UuidType))]
  [GraphQLDescription("The tenant ID this permission applies to (null for global defaults)")]
  public Guid? TenantId { get; set; }

  /// <summary>
  /// Navigation property to the Tenant entity
  /// </summary>
  [GraphQLIgnore]
  [ForeignKey(nameof(TenantId))]
  public new virtual Tenant? Tenant { get; set; }

  /// <summary>
  /// Optional expiration date for this permission
  /// If null, permission never expires
  /// If date has passed, permission is expired
  /// </summary>
  [GraphQLType(typeof(DateTimeType))]
  [GraphQLDescription("When this permission expires (null if it never expires)")]
  public DateTime? ExpiresAt { get; set; }

  // Computed properties

  /// <summary>
  /// Check if the permission is expired based on ExpiresAt date
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Whether this permission has expired")]
  public bool IsExpired {
    get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow;
  }

  /// <summary>
  /// Check if the permission is valid (not deleted and not expired)
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Whether this permission is valid (not deleted and not expired)")]
  public bool IsValid {
    get => !IsDeleted && !IsExpired;
  }

  /// <summary>
  /// Check if this is a default permission for a specific tenant
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Whether this is a default permission for a specific tenant")]
  public bool IsDefaultPermission {
    get => UserId == null && TenantId != null;
  }

  /// <summary>
  /// Check if this is a global default permission
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Whether this is a global default permission")]
  public bool IsGlobalDefaultPermission {
    get => UserId == null && TenantId == null;
  }

  /// <summary>
  /// Check if this is a user-specific permission
  /// </summary>
  [GraphQLType(typeof(NonNullType<BooleanType>))]
  [GraphQLDescription("Whether this is a user-specific permission")]
  public bool IsUserPermission {
    get => UserId != null;
  }

  public bool HasPermission(PermissionType permission) {
    var bitPos = (int)permission;

    if (bitPos < 64)
      return (PermissionFlags1 & (1UL << bitPos)) != 0;
    else if (bitPos < 128) return (PermissionFlags2 & (1UL << (bitPos - 64))) != 0;

    return false;
  }

  public bool HasAllPermissions(Collection<PermissionType> permissions) { return permissions.All(HasPermission); }

  public bool HasAnyPermission(Collection<PermissionType> permissions) { return permissions.Any(HasPermission); }

  public void AddPermission(PermissionType permission) { SetPermission(permission, true); }

  public void RemovePermission(PermissionType permission) { SetPermission(permission, false); }

  public void RemovePermissions(Collection<PermissionType> permissions) {
    foreach (var permission in permissions) SetPermission(permission, false);
  }

  private void SetPermission(PermissionType permission, bool value) {
    var bitPos = (int)permission;

    if (bitPos < 64) {
      var mask = 1UL << bitPos;
      PermissionFlags1 = value ? (PermissionFlags1 | mask) : (PermissionFlags1 & ~mask);
    }
    else if (bitPos < 128) {
      var mask = 1UL << (bitPos - 64);
      PermissionFlags2 = value ? (PermissionFlags2 | mask) : (PermissionFlags2 & ~mask);
    }
  }
}
