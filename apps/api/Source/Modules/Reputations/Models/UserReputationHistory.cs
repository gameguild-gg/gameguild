using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using GameGuild.Modules.Resources;
using GameGuild.Modules.Tenants;
using GameGuild.Modules.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;


namespace GameGuild.Modules.Reputations;

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
  /// The user reputation entity that was changed (for global user reputation tracking)
  /// </summary>
  [ForeignKey(nameof(UserReputationId))]
  public UserReputation? UserReputation { get; set; }

  public Guid? UserReputationId { get; set; }

  /// <summary>
  /// The user-tenant reputation entity that was changed (for tenant-specific reputation tracking)
  /// </summary>
  [ForeignKey(nameof(UserTenantReputationId))]
  public UserTenantReputation? UserTenantReputation { get; set; }

  public Guid? UserTenantReputationId { get; set; }

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
  /// through UserReputationId (for UserReputation) or UserTenantReputationId (for UserTenantReputation)
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

/// <summary>
/// Entity Framework configuration for UserReputationHistory
/// Contains complex configurations that cannot be expressed with simple data annotations
/// </summary>
public class UserReputationHistoryConfiguration : IEntityTypeConfiguration<UserReputationHistory> {
  public void Configure(EntityTypeBuilder<UserReputationHistory> builder) {
    // Check constraint for polymorphic relationship (can't be done with annotations)
    builder.ToTable(
      "UserReputationHistory",
      t => t.HasCheckConstraint(
        "CK_UserReputationHistory_UserOrUserTenant",
        "(\"UserReputationId\" IS NOT NULL AND \"UserTenantReputationId\" IS NULL) OR (\"UserReputationId\" IS NULL AND \"UserTenantReputationId\" IS NOT NULL)"
      )
    );

    // Configure an optional relationship with User (can't be done with annotations)
    builder.HasOne(urh => urh.User).WithMany().HasForeignKey(urh => urh.UserId).OnDelete(DeleteBehavior.SetNull);

    // Configure an optional relationship with UserReputation (can't be done with annotations)
    builder.HasOne(urh => urh.UserReputation)
           .WithMany(ur => ur.History)
           .HasForeignKey(urh => urh.UserReputationId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure an optional relationship with UserTenantReputation (can't be done with annotations)
    builder.HasOne(urh => urh.UserTenantReputation)
           .WithMany(utr => utr.History)
           .HasForeignKey(urh => urh.UserTenantReputationId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure an optional relationship with UserTenant (can't be done with annotations)
    builder.HasOne(urh => urh.TenantPermission)
           .WithMany()
           .HasForeignKey(urh => urh.TenantPermissionId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with ReputationAction (can't be done with annotations)
    builder.HasOne(urh => urh.ReputationAction)
           .WithMany(ra => ra.ReputationHistory)
           .HasForeignKey(urh => urh.ReputationActionId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with TriggeredByUser (can't be done with annotations)
    builder.HasOne(urh => urh.TriggeredByUser)
           .WithMany()
           .HasForeignKey(urh => urh.TriggeredByUserId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with PreviousLevel (can't be done with annotations)
    builder.HasOne(urh => urh.PreviousLevel)
           .WithMany()
           .HasForeignKey(urh => urh.PreviousLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a relationship with NewLevel (can't be done with annotations)
    builder.HasOne(urh => urh.NewLevel)
           .WithMany()
           .HasForeignKey(urh => urh.NewLevelId)
           .OnDelete(DeleteBehavior.SetNull);

    // Configure a polymorphic relationship with RelatedResource (can't be done with annotations)
    builder.HasOne(urh => urh.RelatedResource)
           .WithMany()
           .HasForeignKey("RelatedResourceId")
           .OnDelete(DeleteBehavior.SetNull);
  }
}
