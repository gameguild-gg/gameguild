using GameGuild.Abstractions;

namespace GameGuild.Learning.Experience.Recommendations;

/// <summary>
/// Represents a personalized course recommendation for a user
/// </summary>
public class CourseRecommendation : EntityBase
{
    public Guid UserId { get; private set; }
    public Guid CourseId { get; private set; }
    public RecommendationType Type { get; private set; }
    public double Score { get; private set; } // 0.0 - 1.0
    public string? Reason { get; private set; }
    public bool IsViewed { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTime ExpiresAt { get; private set; }

    private CourseRecommendation() { } // EF Core

    public static CourseRecommendation Create(
        Guid userId,
        Guid courseId,
        RecommendationType type,
        double score,
        string? reason = null,
        TimeSpan? validFor = null)
    {
        return new CourseRecommendation
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CourseId = courseId,
            Type = type,
            Score = Math.Clamp(score, 0.0, 1.0),
            Reason = reason,
            IsViewed = false,
            IsDismissed = false,
            ExpiresAt = DateTime.UtcNow.Add(validFor ?? TimeSpan.FromDays(30)),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void MarkViewed()
    {
        IsViewed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Dismiss()
    {
        IsDismissed = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool IsValid() => !IsDismissed && DateTime.UtcNow < ExpiresAt;
}

/// <summary>
/// Tracks user learning preferences for recommendation engine
/// </summary>
public class UserLearningProfile : EntityBase
{
    public Guid UserId { get; private set; }
    public string? PreferredCategories { get; private set; } // JSON array
    public string? PreferredDifficulty { get; private set; }
    public string? PreferredDuration { get; private set; } // short, medium, long
    public string? LearningGoals { get; private set; } // JSON array
    public string? Skills { get; private set; } // JSON array of skill tags
    public int TotalCoursesCompleted { get; private set; }
    public int TotalHoursLearned { get; private set; }
    public DateTime? LastActivityAt { get; private set; }

    private UserLearningProfile() { } // EF Core

    public static UserLearningProfile Create(Guid userId)
    {
        return new UserLearningProfile
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TotalCoursesCompleted = 0,
            TotalHoursLearned = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public void UpdateActivity()
    {
        LastActivityAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void IncrementCoursesCompleted(int hours)
    {
        TotalCoursesCompleted++;
        TotalHoursLearned += hours;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum RecommendationType
{
    PersonalizedAI,
    PopularInCategory,
    TrendingNow,
    BasedOnHistory,
    SimilarToCompleted,
    NextInPath,
    InstructorFollowed,
    PeerRecommended
}
