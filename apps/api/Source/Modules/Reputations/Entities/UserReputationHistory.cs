using GameGuild.Core.Domain;
using GameGuild.Modules.Users.Entities;


namespace GameGuild.Modules.Reputations.Entities;

/// <summary>
///   Tracks the history of reputation changes for any reputation entity Provides audit trail and analytics for reputation system Supports polymorphic relationships with UserReputation, UserTenantReputation, and future reputation
///   entities
/// </summary>
[Table("UserReputationHistory")]
[Index(nameof(UserId), nameof(OccurredAt))]
[Index(nameof(TenantPermissionId), nameof(OccurredAt))]
[Index(nameof(ReputationActionId))]
[Index(nameof(OccurredAt))]
[Index(nameof(PointsChange))]
public class UserReputationHistory : Resource
{
    /// <summary> The user whose reputation changed (for direct user reputation tracking) </summary>
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    public Guid? UserId { get; set; }

    /// <summary> The user reputation entity that was changed (for global user reputation tracking) </summary>
    [ForeignKey(nameof(UserReputationId))]
    public UserReputation? UserReputation { get; set; }

    public Guid? UserReputationId { get; set; }

    /// <summary> The user-tenant reputation entity that was changed (for tenant-specific reputation tracking) </summary>
    [ForeignKey(nameof(UserTenantReputationId))]
    public UserTenantReputation? UserTenantReputation { get; set; }

    public Guid? UserTenantReputationId { get; set; }

    /// <summary> The user-tenant whose reputation changed (for tenant-specific reputation tracking) </summary>
    [ForeignKey(nameof(TenantPermissionId))]
    public TenantPermission? TenantPermission { get; set; }

    public Guid? TenantPermissionId { get; set; }

    /// <summary>
    ///   Polymorphic reference to the reputation entity that changed This can point to UserReputation, UserTenantReputation, or any future IReputation implementation Note: This is a computed property for convenience - the actual
    ///   relationship is handled through UserReputationId (for UserReputation) or UserTenantReputationId (for UserTenantReputation)
    /// </summary>
    [NotMapped]
    public IReputation? Reputation { get; set; }

    /// <summary> The action that caused this reputation change </summary>
    [ForeignKey(nameof(ReputationActionId))]
    public ReputationAction? ReputationAction { get; set; }

    public Guid? ReputationActionId { get; set; }

    /// <summary> Points gained or lost in this change </summary>
    public int PointsChange { get; set; }

    /// <summary> User's reputation score before this change </summary>
    public int PreviousScore { get; set; }

    /// <summary> User's reputation score after this change </summary>
    public int NewScore { get; set; }

    /// <summary> Previous reputation tier (if different) </summary>
    [ForeignKey(nameof(PreviousLevelId))]
    public ReputationTier? PreviousLevel { get; set; }

    public Guid? PreviousLevelId { get; set; }

    /// <summary> New reputation tier (if changed) </summary>
    [ForeignKey(nameof(NewLevelId))]
    public ReputationTier? NewLevel { get; set; }

    public Guid? NewLevelId { get; set; }

    /// <summary> Optional reason for this reputation change </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }

    /// <summary> User who triggered this change (null for system actions) </summary>
    [ForeignKey(nameof(TriggeredByUserId))]
    public User? TriggeredByUser { get; set; }

    public Guid? TriggeredByUserId { get; set; }

    /// <summary> Related resource that triggered this change (polymorphic relationship) EntityBase Framework will create a shadow RelatedResourceId foreign key property automatically </summary>
    public Resource? RelatedResource { get; set; }

    /// <summary> When this reputation change occurred </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}

/// <summary> EntityBase Framework configuration for UserReputationHistory Contains complex configurations that cannot be expressed with simple data annotations </summary>
public class UserReputationHistoryConfiguration : IEntityTypeConfiguration<UserReputationHistory>
{
    public void Configure(EntityTypeBuilder<UserReputationHistory> builder)
    {
        // Check constraint for polymorphic relationship (can't be done with annotations)
        builder.ToTable(
          "UserReputationHistory",
          t => t.HasCheckConstraint("CK_UserReputationHistory_UserOrUserTenant", "(\"UserReputationId\" IS NOT NULL AND \"UserTenantReputationId\" IS NULL) OR (\"UserReputationId\" IS NULL AND \"UserTenantReputationId\" IS NOT NULL)")
        );

        // Explicitly configure the RelatedResource polymorphic relationship
        builder.HasOne(h => h.RelatedResource).WithMany().HasForeignKey("RelatedResourceId").OnDelete(DeleteBehavior.SetNull);

        // Ensure proper cascade delete for related entities
        builder.HasOne(h => h.UserReputation).WithMany(u => u.History).HasForeignKey(h => h.UserReputationId).OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.UserTenantReputation).WithMany(u => u.History).HasForeignKey(h => h.UserTenantReputationId).OnDelete(DeleteBehavior.Cascade);

        // Additional indexes for performance
        builder.HasIndex(h => new { h.UserReputationId, h.OccurredAt });
        builder.HasIndex(h => new { h.UserTenantReputationId, h.OccurredAt });
    }
}
