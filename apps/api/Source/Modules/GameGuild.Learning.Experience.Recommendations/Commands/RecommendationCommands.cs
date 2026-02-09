using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.Recommendations;

// ===== USER LEARNING PROFILE COMMANDS =====

/// <summary>
/// Create or update a user's learning profile
/// </summary>
public sealed record CreateOrUpdateLearningProfileCommand(
    Guid UserId,
    string[]? PreferredCategories,
    string? PreferredDifficulty,
    string? PreferredDuration,
    string[]? LearningGoals,
    string[]? Skills) : ICommand<UserLearningProfile>;

/// <summary>
/// Add a skill interest to user's profile
/// </summary>
public sealed record AddSkillToProfileCommand(Guid UserId, string Skill) : ICommand<UserLearningProfile>;

/// <summary>
/// Remove a skill from user's profile
/// </summary>
public sealed record RemoveSkillFromProfileCommand(Guid UserId, string Skill) : ICommand<UserLearningProfile>;

/// <summary>
/// Update user activity timestamp
/// </summary>
public sealed record UpdateUserActivityCommand(Guid UserId) : ICommand;

/// <summary>
/// Increment completed courses count
/// </summary>
public sealed record IncrementCompletedCoursesCommand(Guid UserId, int Hours) : ICommand;

// ===== RECOMMENDATION COMMANDS =====

/// <summary>
/// Generate recommendations for a user
/// </summary>
public sealed record GenerateRecommendationsCommand(
    Guid UserId,
    Guid? TenantId,
    int MaxResults = 10,
    RecommendationType[]? Types = null) : ICommand<IEnumerable<CourseRecommendation>>;

/// <summary>
/// Mark a recommendation as viewed
/// </summary>
public sealed record MarkRecommendationViewedCommand(Guid RecommendationId, Guid UserId) : ICommand;

/// <summary>
/// Dismiss a recommendation
/// </summary>
public sealed record DismissRecommendationCommand(Guid RecommendationId, Guid UserId) : ICommand;

/// <summary>
/// Refresh recommendations for a user (remove expired, generate new)
/// </summary>
public sealed record RefreshRecommendationsCommand(Guid UserId, Guid? TenantId) : ICommand;

/// <summary>
/// Clear all recommendations for a user
/// </summary>
public sealed record ClearUserRecommendationsCommand(Guid UserId) : ICommand<int>;
