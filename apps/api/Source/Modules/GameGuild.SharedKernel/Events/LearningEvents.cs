using GameGuild.CQRS;

namespace GameGuild.Learning;

/// <summary>
/// Domain event raised when a user views a course detail page
/// </summary>
public class CourseViewedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }
    public string Source { get; } // "search", "browse", "recommendation", "learning-path"
    public Guid? ReferrerId { get; } // Source recommendation ID or learning path ID

    public CourseViewedEvent(Guid userId, Guid courseId, Guid? tenantId, string source, Guid? referrerId = null)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
        Source = source;
        ReferrerId = referrerId;
    }
}

/// <summary>
/// Domain event raised when a user enrolls in a course
/// </summary>
public class CourseEnrolledEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }
    public string Source { get; } // "direct", "recommendation", "learning-path"
    public Guid? ReferrerId { get; }

    public CourseEnrolledEvent(Guid userId, Guid courseId, Guid? tenantId, string source, Guid? referrerId = null)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
        Source = source;
        ReferrerId = referrerId;
    }
}

/// <summary>
/// Domain event raised when a user starts a content item within a course
/// </summary>
public class ContentStartedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid ContentId { get; }
    public Guid? TenantId { get; }
    public string ContentType { get; } // "video", "article", "quiz", "assignment"

    public ContentStartedEvent(Guid userId, Guid courseId, Guid contentId, Guid? tenantId, string contentType)
    {
        UserId = userId;
        CourseId = courseId;
        ContentId = contentId;
        TenantId = tenantId;
        ContentType = contentType;
    }
}

/// <summary>
/// Domain event raised when a user completes a content item
/// </summary>
public class ContentCompletedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid ContentId { get; }
    public Guid? TenantId { get; }
    public string ContentType { get; }
    public int TimeSpentSeconds { get; }
    public int? Score { get; } // For quizzes/assignments

    public ContentCompletedEvent(Guid userId, Guid courseId, Guid contentId, Guid? tenantId, string contentType, int timeSpentSeconds, int? score = null)
    {
        UserId = userId;
        CourseId = courseId;
        ContentId = contentId;
        TenantId = tenantId;
        ContentType = contentType;
        TimeSpentSeconds = timeSpentSeconds;
        Score = score;
    }
}

/// <summary>
/// Domain event raised when a user completes a course
/// </summary>
public class CourseCompletedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }
    public int TotalTimeSpentSeconds { get; }
    public int TotalContentItems { get; }
    public decimal? FinalScore { get; }

    public CourseCompletedEvent(Guid userId, Guid courseId, Guid? tenantId, int totalTimeSpentSeconds, int totalContentItems, decimal? finalScore = null)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
        TotalTimeSpentSeconds = totalTimeSpentSeconds;
        TotalContentItems = totalContentItems;
        FinalScore = finalScore;
    }
}

/// <summary>
/// Domain event raised when a user drops/unenrolls from a course
/// </summary>
public class CourseDroppedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }
    public int ProgressPercent { get; }
    public string? Reason { get; }

    public CourseDroppedEvent(Guid userId, Guid courseId, Guid? tenantId, int progressPercent, string? reason = null)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
        ProgressPercent = progressPercent;
        Reason = reason;
    }
}

/// <summary>
/// Domain event raised when a search is performed
/// </summary>
public class SearchPerformedEvent : DomainEvent
{
    public Guid? UserId { get; } // Can be null for anonymous searches
    public string Query { get; }
    public int ResultCount { get; }
    public Guid? TenantId { get; }
    public string? Filters { get; } // JSON-serialized filter criteria

    public SearchPerformedEvent(Guid? userId, string query, int resultCount, Guid? tenantId, string? filters = null)
    {
        UserId = userId;
        Query = query;
        ResultCount = resultCount;
        TenantId = tenantId;
        Filters = filters;
    }
}

/// <summary>
/// Domain event raised when a search result is clicked
/// </summary>
public class SearchResultClickedEvent : DomainEvent
{
    public Guid? UserId { get; }
    public string Query { get; }
    public Guid ClickedCourseId { get; }
    public int Position { get; } // Position in search results (1-based)
    public Guid? TenantId { get; }

    public SearchResultClickedEvent(Guid? userId, string query, Guid clickedCourseId, int position, Guid? tenantId)
    {
        UserId = userId;
        Query = query;
        ClickedCourseId = clickedCourseId;
        Position = position;
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when a recommendation is viewed
/// </summary>
public class RecommendationViewedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid RecommendationId { get; }
    public Guid CourseId { get; }
    public string RecommendationType { get; }
    public int Position { get; } // Position in recommendations list
    public Guid? TenantId { get; }

    public RecommendationViewedEvent(Guid userId, Guid recommendationId, Guid courseId, string recommendationType, int position, Guid? tenantId)
    {
        UserId = userId;
        RecommendationId = recommendationId;
        CourseId = courseId;
        RecommendationType = recommendationType;
        Position = position;
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when a recommendation is clicked (user navigates to the course)
/// </summary>
public class RecommendationClickedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid RecommendationId { get; }
    public Guid CourseId { get; }
    public string RecommendationType { get; }
    public int Position { get; }
    public Guid? TenantId { get; }

    public RecommendationClickedEvent(Guid userId, Guid recommendationId, Guid courseId, string recommendationType, int position, Guid? tenantId)
    {
        UserId = userId;
        RecommendationId = recommendationId;
        CourseId = courseId;
        RecommendationType = recommendationType;
        Position = position;
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when a recommendation leads to enrollment
/// </summary>
public class RecommendationConvertedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid RecommendationId { get; }
    public Guid CourseId { get; }
    public string RecommendationType { get; }
    public Guid? TenantId { get; }

    public RecommendationConvertedEvent(Guid userId, Guid recommendationId, Guid courseId, string recommendationType, Guid? tenantId)
    {
        UserId = userId;
        RecommendationId = recommendationId;
        CourseId = courseId;
        RecommendationType = recommendationType;
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when a user enrolls in a learning path
/// </summary>
public class LearningPathEnrolledEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid LearningPathId { get; }
    public Guid? TenantId { get; }
    public int TotalCourses { get; }

    public LearningPathEnrolledEvent(Guid userId, Guid learningPathId, Guid? tenantId, int totalCourses)
    {
        UserId = userId;
        LearningPathId = learningPathId;
        TenantId = tenantId;
        TotalCourses = totalCourses;
    }
}

/// <summary>
/// Domain event raised when a user completes a learning path
/// </summary>
public class LearningPathCompletedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid LearningPathId { get; }
    public Guid? TenantId { get; }
    public int TotalCoursesCompleted { get; }
    public int TotalTimeSpentSeconds { get; }

    public LearningPathCompletedEvent(Guid userId, Guid learningPathId, Guid? tenantId, int totalCoursesCompleted, int totalTimeSpentSeconds)
    {
        UserId = userId;
        LearningPathId = learningPathId;
        TenantId = tenantId;
        TotalCoursesCompleted = totalCoursesCompleted;
        TotalTimeSpentSeconds = totalTimeSpentSeconds;
    }
}

/// <summary>
/// Domain event raised when a user updates their learning progress
/// </summary>
public class LearningProgressUpdatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? ContentId { get; }
    public Guid? TenantId { get; }
    public int OldProgress { get; }
    public int NewProgress { get; }

    public LearningProgressUpdatedEvent(Guid userId, Guid courseId, Guid? contentId, Guid? tenantId, int oldProgress, int newProgress)
    {
        UserId = userId;
        CourseId = courseId;
        ContentId = contentId;
        TenantId = tenantId;
        OldProgress = oldProgress;
        NewProgress = newProgress;
    }
}

/// <summary>
/// Domain event raised when a user rates a course
/// </summary>
public class CourseRatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }
    public int Rating { get; } // 1-5
    public string? ReviewText { get; }

    public CourseRatedEvent(Guid userId, Guid courseId, Guid? tenantId, int rating, string? reviewText = null)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
        Rating = rating;
        ReviewText = reviewText;
    }
}

/// <summary>
/// Domain event raised when a user adds a course to wishlist
/// </summary>
public class CourseWishlistedEvent : DomainEvent
{
    public Guid UserId { get; }
    public Guid CourseId { get; }
    public Guid? TenantId { get; }

    public CourseWishlistedEvent(Guid userId, Guid courseId, Guid? tenantId)
    {
        UserId = userId;
        CourseId = courseId;
        TenantId = tenantId;
    }
}

/// <summary>
/// Domain event raised when a user's skill is updated (either gained or improved)
/// </summary>
public class UserSkillUpdatedEvent : DomainEvent
{
    public Guid UserId { get; }
    public string SkillName { get; }
    public string? ProficiencyLevel { get; } // "Beginner", "Intermediate", "Advanced", "Expert"
    public Guid? SourceCourseId { get; }
    public Guid? TenantId { get; }

    public UserSkillUpdatedEvent(Guid userId, string skillName, string? proficiencyLevel, Guid? sourceCourseId, Guid? tenantId)
    {
        UserId = userId;
        SkillName = skillName;
        ProficiencyLevel = proficiencyLevel;
        SourceCourseId = sourceCourseId;
        TenantId = tenantId;
    }
}
