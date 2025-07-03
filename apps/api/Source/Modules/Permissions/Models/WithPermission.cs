using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Common.Entities;
using GameGuild.Modules.Tenants.Models;
using GameGuild.Modules.Users.Models;


// do not remove this, it's needed for IQueryable extensions

// do not remove this, it's needed for IQueryable extensions

namespace GameGuild.Modules.Permissions.Models;

public enum PermissionType {
  #region Interaction Permissions

  Read = 1,
  Comment = 2,
  Reply = 3,
  Vote = 4,
  Share = 5,
  Report = 6,
  Follow = 7,
  Bookmark = 8,
  React = 9,
  Subscribe = 10,
  Mention = 11,
  Tag = 12,

  #endregion

  #region Curation Permissions

  Categorize = 13,
  Collection = 14,
  Series = 15,
  CrossReference = 16,
  Translate = 17,
  Version = 18,
  Template = 19,

  #endregion

  #region Lifecycle Permissions

  Create = 20,
  Draft = 21,
  Submit = 22,
  Withdraw = 23,
  Archive = 24,
  Restore = 25,
  Delete = 26, // Delete is an alias for SoftDelete, so they share the same value

  SoftDelete =
    26, // Only the owners of a resource can soft delete it at resource level, it still can be deleted by admins at tenant or content type level

  HardDelete = 27,
  Backup = 28,
  Migrate = 29,
  Clone = 30,

  #endregion

  #region Editorial Permissions

  Edit = 31,
  Proofread = 32,
  FactCheck = 33,
  StyleGuide = 34,
  Plagiarism = 35,
  Seo = 36,
  Accessibility = 37,
  Legal = 38,
  Brand = 39,
  Guidelines = 40,

  #endregion

  #region Moderation Permissions

  Review = 41,
  Approve = 42,
  Reject = 43,
  Hide = 44,
  Quarantine = 45,
  Flag = 46,
  Warning = 47,
  Suspend = 48,
  Ban = 49,
  Escalate = 50,

  #endregion

  #region Monetization Permissions

  Monetize = 51,
  Paywall = 52,
  Subscription = 53,
  Advertisement = 54,
  Sponsorship = 55,
  Affiliate = 56,
  Commission = 57,
  License = 58,
  Pricing = 59,
  Revenue = 60,

  #endregion

  #region Promotion Permissions

  Feature = 61,
  Pin = 62,
  Trending = 63,
  Recommend = 64,
  Spotlight = 65,
  Banner = 66,
  Carousel = 67,
  Widget = 68,
  Email = 69,
  Push = 70,
  Sms = 71,

  #endregion

  #region Publishing Permissions

  Publish = 72,
  Unpublish = 73,
  Schedule = 74,
  Reschedule = 75,
  Distribute = 76,
  Syndicate = 77,
  Rss = 78,
  Newsletter = 79,
  SocialMedia = 80,
  Api = 81,

  #endregion

  #region Quality Control Permissions

  Score = 82,
  Rate = 83,
  Benchmark = 84,
  Metrics = 85,
  Analytics = 86,
  Performance = 87,
  Feedback = 88,
  Audit = 89,
  Standards = 90,
  Improvement = 91,

  #endregion
}

// this is a base permission model that can be used to store permissions for any entity
[ObjectType]
public class WithPermissions : BaseEntity {
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

/// <summary>
/// Helper methods for permission queries without fluent API
/// </summary>
public static class PermissionQueryHelper {
  public static IQueryable<T> GetWithPermission<T>(IQueryable<T> query, PermissionType permission)
    where T : WithPermissions {
    var bitPos = (int)permission;

    if (bitPos < 64) {
      var mask = 1UL << bitPos;

      return query.Where(x => (x.PermissionFlags1 & mask) == mask);
    }
    else if (bitPos < 128) {
      var mask = 1UL << (bitPos - 64);

      return query.Where(x => (x.PermissionFlags2 & mask) == mask);
    }

    return query.Where(x => false);
  }

  public static IQueryable<T> GetWithAllPermissions<T>(IQueryable<T> query, params PermissionType[] permissions)
    where T : WithPermissions {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    var result = query;

    if (mask1 > 0) result = result.Where(x => (x.PermissionFlags1 & mask1) == mask1);

    if (mask2 > 0) result = result.Where(x => (x.PermissionFlags2 & mask2) == mask2);

    return result;
  }

  public static IQueryable<T> GetWithAnyPermission<T>(IQueryable<T> query, params PermissionType[] permissions)
    where T : WithPermissions {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    var result = query.Where(x => false); // Start with empty result

    if (mask1 > 0) result = result.Union(query.Where(x => (x.PermissionFlags1 & mask1) != 0));

    if (mask2 > 0) result = result.Union(query.Where(x => (x.PermissionFlags2 & mask2) != 0));

    return result;
  }

  /// <summary>
  /// Calculate permission masks for manual queries
  /// </summary>
  public static (ulong mask1, ulong mask2) CalculatePermissionMasks(params PermissionType[] permissions) {
    var mask1 = 0UL;
    var mask2 = 0UL;

    foreach (var permission in permissions) {
      var bitPos = (int)permission;

      if (bitPos < 64)
        mask1 |= 1UL << bitPos;
      else if (bitPos < 128) mask2 |= 1UL << (bitPos - 64);
    }

    return (mask1, mask2);
  }
}
