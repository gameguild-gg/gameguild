namespace GameGuild;

/// <summary> Enumeration of permission types in the system Represents the various operations that can be controlled through permissions </summary>
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

  SoftDelete = 26, // Only the owners of a resource can soft delete it at resource level, it still can be deleted by admins at tenant or content type level

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

  #region Administrative Permissions

  // Administrative permissions
  Manage = 100,

  Admin = 101,

  // Special permissions
  Execute = 110,

  Export = 111,

  Import = 112,

  // System permissions
  SystemAdmin = 200,

  TenantAdmin = 201,

  UserManagement = 202,

  #endregion
}

/// <summary> Base class for all permission entities following DDD principles </summary>
public abstract class PermissionBase : EntityBase {
  protected PermissionBase() { }

  protected PermissionBase(Guid? userId, Guid? tenantId, PermissionType permissions) {
    UserId = userId;
    TenantId = tenantId;
    Permissions = permissions;
    CreatedAt = DateTime.UtcNow;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> User ID (null means it's a default permission) </summary>
  public Guid? UserId { get; protected set; }

  /// <summary> Tenant ID (null means it's a global permission) </summary>
  public Guid? TenantId { get; protected set; }

  /// <summary> Bitwise combination of permission types </summary>
  public PermissionType Permissions { get; protected set; }

  /// <summary> When the permission was granted </summary>
  public override DateTime CreatedAt { get; set; }

  /// <summary> When the permission was last updated </summary>
  public override DateTime UpdatedAt { get; set; }

  /// <summary> Optional expiration date for temporary permissions </summary>
  public DateTime? ExpiresAt { get; protected set; }

  /// <summary> Check if this permission has expired </summary>
  public virtual bool IsExpired { get => ExpiresAt.HasValue && ExpiresAt.Value <= DateTime.UtcNow; }

  /// <summary> Update the permissions for this entity </summary>
  public virtual void UpdatePermissions(PermissionType permissions) {
    Permissions = permissions;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> Set expiration date for this permission </summary>
  public virtual void SetExpiration(DateTime? expiresAt) {
    ExpiresAt = expiresAt;
    UpdatedAt = DateTime.UtcNow;
  }

  /// <summary> Check if this permission includes a specific permission type </summary>
  public virtual bool HasPermission(PermissionType permission) { return !IsExpired && (Permissions & permission) == permission; }
}

/// <summary> Tenant-wide permissions </summary>
public class TenantPermission : PermissionBase {
  private TenantPermission() { }

  public TenantPermission(Guid? userId, Guid? tenantId, PermissionType permissions) : base(userId, tenantId, permissions) { }

  public static TenantPermission CreateUserPermission(Guid userId, Guid tenantId, PermissionType permissions) { return new(userId, tenantId, permissions); }

  public static TenantPermission CreateTenantDefault(Guid tenantId, PermissionType permissions) { return new(null, tenantId, permissions); }

  public static TenantPermission CreateGlobalDefault(PermissionType permissions) { return new(null, null, permissions); }
}

/// <summary> Content-type permissions (e.g., permissions for all Posts, all Comments, etc.) </summary>
public class ContentTypePermission : PermissionBase {
  private ContentTypePermission() { }

  public ContentTypePermission(Guid userId, Guid? tenantId, string contentTypeName, PermissionType permissions) : base(userId, tenantId, permissions) {
    ContentTypeName = contentTypeName ?? throw new ArgumentNullException(nameof(contentTypeName));
  }

  /// <summary> Name of the content type (e.g., "Post", "Comment", "User") </summary>
  public string ContentTypeName { get; private set; } = string.Empty;

  public static ContentTypePermission Create(Guid userId, Guid? tenantId, string contentTypeName, PermissionType permissions) { return new(userId, tenantId, contentTypeName, permissions); }
}

/// <summary> Resource-specific permissions (permissions for a specific resource instance) </summary>
public class ResourcePermission<TResource> : PermissionBase where TResource : EntityBase {
  public ResourcePermission() { }

  public ResourcePermission(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions) : base(userId, tenantId, permissions) { ResourceId = resourceId; }

  /// <summary> ID of the specific resource </summary>
  public Guid ResourceId { get; private set; }

  /// <summary> Resource type name for querying purposes </summary>
  public string ResourceType { get => typeof(TResource).Name; }

  public static ResourcePermission<TResource> Create(Guid userId, Guid? tenantId, Guid resourceId, PermissionType permissions) { return new(userId, tenantId, resourceId, permissions); }
}
