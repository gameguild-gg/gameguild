namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Interface for accessing user skill/interest profile information across modules
/// </summary>
public interface ILearnerProfileProvider
{
    /// <summary>
    /// Gets a user's skill interests
    /// </summary>
    Task<IReadOnlyList<SkillInterest>> GetUserSkillInterestsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a user's learning preferences
    /// </summary>
    Task<LearningPreferences?> GetUserLearningPreferencesAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets users with similar skill interests
    /// </summary>
    Task<IReadOnlyList<Guid>> GetUsersWithSimilarInterestsAsync(
        Guid userId,
        int maxResults = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the user's acquired skills based on completed content
    /// </summary>
    Task<IReadOnlyList<AcquiredSkill>> GetUserAcquiredSkillsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Checks if a user exists in the learning system
    /// </summary>
    Task<bool> UserExistsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// User's interest in a particular skill
/// </summary>
public record SkillInterest
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public int InterestLevel { get; init; } // 1-5 scale
    public DateTime AddedAt { get; init; }
    public bool IsExplicit { get; init; } // User explicitly added vs inferred
}

/// <summary>
/// User's acquired skill with proficiency
/// </summary>
public record AcquiredSkill
{
    public Guid SkillId { get; init; }
    public string SkillName { get; init; } = string.Empty;
    public string? Category { get; init; }
    public SkillProficiency Proficiency { get; init; }
    public int CoursesCompleted { get; init; }
    public int AssessmentsCompleted { get; init; }
    public DateTime? LastPracticed { get; init; }
    public DateTime AcquiredAt { get; init; }
}

/// <summary>
/// Skill proficiency level
/// </summary>
public enum SkillProficiency
{
    Beginner = 0,
    Elementary = 1,
    Intermediate = 2,
    Advanced = 3,
    Expert = 4
}

/// <summary>
/// User's learning preferences
/// </summary>
public record LearningPreferences
{
    public Guid UserId { get; init; }
    public IReadOnlyList<string> PreferredContentTypes { get; init; } = Array.Empty<string>(); // video, article, quiz, etc.
    public IReadOnlyList<string> PreferredDifficultyLevels { get; init; } = Array.Empty<string>();
    public int? PreferredSessionLengthMinutes { get; init; }
    public IReadOnlyList<string> PreferredLearningTimes { get; init; } = Array.Empty<string>(); // morning, afternoon, evening
    public string? PreferredLanguage { get; init; }
    public bool NotificationsEnabled { get; init; }
    public bool EmailDigestEnabled { get; init; }
    public bool ShowProgressPublicly { get; init; }
}
