using GameGuild.Common;


namespace GameGuild.Modules.UserAchievements;

/// <summary>
/// Event raised when an achievement is created
/// </summary>
public class AchievementCreatedEvent : DomainEventBase {
  public Guid AchievementId { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public string Type { get; set; } = string.Empty;
  public int Points { get; set; }
  public Guid? TenantId { get; set; }
  public Guid CreatedByUserId { get; set; }

  public AchievementCreatedEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}

/// <summary>
/// Event raised when an achievement is updated
/// </summary>
public class AchievementUpdatedEvent : DomainEventBase {
  public Guid AchievementId { get; set; }
  public string Name { get; set; } = string.Empty;
  public bool IsActive { get; set; }
  public Guid? TenantId { get; set; }
  public Guid UpdatedByUserId { get; set; }

  public AchievementUpdatedEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}

/// <summary>
/// Event raised when an achievement is deleted
/// </summary>
public class AchievementDeletedEvent : DomainEventBase {
  public Guid AchievementId { get; set; }
  public string Name { get; set; } = string.Empty;
  public Guid? TenantId { get; set; }
  public Guid DeletedByUserId { get; set; }

  public AchievementDeletedEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}

/// <summary>
/// Event raised when a user earns an achievement
/// </summary>
public class AchievementEarnedEvent : DomainEventBase {
  public Guid UserAchievementId { get; set; }
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public string AchievementName { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty;
  public int PointsEarned { get; set; }
  public int? Level { get; set; }
  public DateTime EarnedAt { get; set; }
  public bool IsRepeatable { get; set; }
  public int EarnCount { get; set; }
  public string? Context { get; set; }
  public Guid? TenantId { get; set; }
  public Guid? AwardedByUserId { get; set; }

  public AchievementEarnedEvent(Guid userAchievementId) : base(userAchievementId, nameof(UserAchievement)) {
    UserAchievementId = userAchievementId;
  }
}

/// <summary>
/// Event raised when an achievement is revoked from a user
/// </summary>
public class AchievementRevokedEvent : DomainEventBase {
  public Guid UserAchievementId { get; set; }
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public string AchievementName { get; set; } = string.Empty;
  public int PointsLost { get; set; }
  public string? Reason { get; set; }
  public Guid? TenantId { get; set; }
  public Guid RevokedByUserId { get; set; }

  public AchievementRevokedEvent(Guid userAchievementId) : base(userAchievementId, nameof(UserAchievement)) {
    UserAchievementId = userAchievementId;
  }
}

/// <summary>
/// Event raised when a user's progress towards an achievement is updated
/// </summary>
public class AchievementProgressUpdatedEvent : DomainEventBase {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public string AchievementName { get; set; } = string.Empty;
  public int PreviousProgress { get; set; }
  public int NewProgress { get; set; }
  public int TargetProgress { get; set; }
  public double ProgressPercentage { get; set; }
  public bool IsCompleted { get; set; }
  public string? Context { get; set; }
  public Guid? TenantId { get; set; }

  public AchievementProgressUpdatedEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}

/// <summary>
/// Event raised when a user reaches a milestone in their achievement journey
/// </summary>
public class AchievementMilestoneReachedEvent : DomainEventBase {
  public Guid UserId { get; set; }
  public string MilestoneType { get; set; } = string.Empty; // e.g., "first_achievement", "10_achievements", "1000_points"
  public int Value { get; set; } // The milestone value reached
  public int TotalAchievements { get; set; }
  public int TotalPoints { get; set; }
  public string? Category { get; set; } // If milestone is category-specific
  public Guid? TenantId { get; set; }

  public AchievementMilestoneReachedEvent(Guid userId) : base(userId, "User") {
    UserId = userId;
  }
}

/// <summary>
/// Event raised when a user levels up in an achievement
/// </summary>
public class AchievementLevelUpEvent : DomainEventBase {
  public Guid UserId { get; set; }
  public Guid AchievementId { get; set; }
  public string AchievementName { get; set; } = string.Empty;
  public int PreviousLevel { get; set; }
  public int NewLevel { get; set; }
  public string LevelName { get; set; } = string.Empty;
  public int PointsEarned { get; set; }
  public Guid? TenantId { get; set; }

  public AchievementLevelUpEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}

/// <summary>
/// Event raised when achievements are bulk awarded
/// </summary>
public class BulkAchievementAwardedEvent : DomainEventBase {
  public Guid AchievementId { get; set; }
  public string AchievementName { get; set; } = string.Empty;
  public List<Guid> UserIds { get; set; } = new();
  public int TotalUsersAwarded { get; set; }
  public string? Context { get; set; }
  public Guid? TenantId { get; set; }
  public Guid AwardedByUserId { get; set; }

  public BulkAchievementAwardedEvent(Guid achievementId) : base(achievementId, nameof(Achievement)) {
    AchievementId = achievementId;
  }
}
