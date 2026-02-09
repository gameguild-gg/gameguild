namespace GameGuild.Gamification.Achievements;

/// <summary>
/// Domain events for the achievements system.
/// These events can be used to trigger cross-module actions and notifications.
/// </summary>
/// 
/// <summary>
/// Event raised when a user earns a new achievement.
/// </summary>
public sealed record AchievementEarnedEvent(
    Guid UserId,
    Guid AchievementId,
    string AchievementName,
    int PointsEarned,
    int Level,
    DateTime EarnedAt,
    Guid? TenantId = null);

/// <summary>
/// Event raised when a user's progress toward an achievement is updated.
/// </summary>
public sealed record AchievementProgressUpdatedEvent(
    Guid UserId,
    Guid AchievementId,
    int CurrentProgress,
    int TargetProgress,
    bool IsCompleted,
    Guid? TenantId = null);

/// <summary>
/// Event raised when a user reaches a new level in a tiered achievement.
/// </summary>
public sealed record AchievementLevelUpEvent(
    Guid UserId,
    Guid AchievementId,
    string AchievementName,
    int PreviousLevel,
    int NewLevel,
    string? LevelName,
    int BonusPoints,
    Guid? TenantId = null);

/// <summary>
/// Event raised when a user earns achievement points (for leaderboard updates).
/// </summary>
public sealed record AchievementPointsEarnedEvent(
    Guid UserId,
    int PointsEarned,
    int TotalPoints,
    string Source,
    Guid? TenantId = null);

/// <summary>
/// Event raised when an achievement becomes available (e.g., prerequisites met).
/// </summary>
public sealed record AchievementUnlockedEvent(
    Guid UserId,
    Guid AchievementId,
    string AchievementName,
    string? Reason,
    Guid? TenantId = null);

/// <summary>
/// Event raised when a new achievement is created by an admin.
/// </summary>
public sealed record AchievementCreatedEvent(
    Guid AchievementId,
    string Name,
    string? Category,
    int Points,
    Guid CreatedBy,
    Guid? TenantId = null);

/// <summary>
/// Event raised when an achievement is modified.
/// </summary>
public sealed record AchievementModifiedEvent(
    Guid AchievementId,
    string Name,
    string ChangeDescription,
    Guid ModifiedBy,
    Guid? TenantId = null);

/// <summary>
/// Event raised for milestone achievements (e.g., "First 100 users to complete").
/// </summary>
public sealed record MilestoneAchievementEvent(
    Guid UserId,
    Guid AchievementId,
    string AchievementName,
    int MilestoneRank,
    string MilestoneDescription,
    Guid? TenantId = null);
