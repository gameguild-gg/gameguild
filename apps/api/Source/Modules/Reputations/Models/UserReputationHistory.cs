using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants.Entities;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Reputations.Models;

/// <summary>
/// Tracks the history of reputation changes for any reputation entity
/// Provides audit trail and analytics for reputation system
/// Supports polymorphic relationships with UserReputation, UserTenantReputation, and future reputation entities
/// </summary>
[Table("UserReputationHistory")]
[Index(nameof(UserId), nameof(OccurredAt))]
[Index(nameof(TenantPermissionId), nameof(OccurredAt))]
[Index(nameof(ReputationActionId))]
[Index(nameof(OccurredAt))]
[Index(nameof(PointsChange))]
public class UserReputationHistory : Resource {
  /// <summary>
  /// The user whose reputation changed (for direct user reputation tracking)
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public User? User { get; set; }

  public Guid? UserId { get; set; }

  /// <summary>
  /// The user-tenant whose reputation changed (for tenant-specific reputation tracking)
  /// </summary>
  [ForeignKey(nameof(TenantPermissionId))]
  public TenantPermission? TenantPermission { get; set; }

  public Guid? TenantPermissionId { get; set; }

  /// <summary>
  /// Polymorphic reference to the reputation entity that changed
  /// This can point to UserReputation, UserTenantReputation, or any future IReputation implementation
  /// Note: This is a computed property for convenience - the actual relationship is handled
  /// through UserId (for UserReputation) or UserTenantId (for UserTenantReputation)
  /// </summary>
  [NotMapped]
  public IReputation? Reputation { get; set; }

  /// <summary>
  /// The action that caused this reputation change
  /// </summary>
  [ForeignKey(nameof(ReputationActionId))]
  public ReputationAction? ReputationAction { get; set; }

  public Guid? ReputationActionId { get; set; }

  /// <summary>
  /// Points gained or lost in this change
  /// </summary>
  public int PointsChange { get; set; }

  /// <summary>
  /// User's reputation score before this change
  /// </summary>
  public int PreviousScore { get; set; }

  /// <summary>
  /// User's reputation score after this change
  /// </summary>
  public int NewScore { get; set; }

  /// <summary>
  /// Previous reputation tier (if different)
  /// </summary>
  [ForeignKey(nameof(PreviousLevelId))]
  public ReputationTier? PreviousLevel { get; set; }

  public Guid? PreviousLevelId { get; set; }

  /// <summary>
  /// New reputation tier (if changed)
  /// </summary>
  [ForeignKey(nameof(NewLevelId))]
  public ReputationTier? NewLevel { get; set; }

  public Guid? NewLevelId { get; set; }

  /// <summary>
  /// Optional reason for this reputation change
  /// </summary>
  [MaxLength(500)]
  public string? Reason { get; set; }

  /// <summary>
  /// User who triggered this change (null for system actions)
  /// </summary>
  [ForeignKey(nameof(TriggeredByUserId))]
  public User? TriggeredByUser { get; set; }

  public Guid? TriggeredByUserId { get; set; }

  /// <summary>
  /// Related resource that triggered this change (polymorphic relationship)
  /// Entity Framework will create a shadow RelatedResourceId foreign key property automatically
  /// </summary>
  public Resource? RelatedResource { get; set; }

  /// <summary>
  /// When this reputation change occurred
  /// </summary>
  public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
