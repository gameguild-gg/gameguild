namespace GameGuild.Learning;

/// <summary>
/// Constants used across all Learning modules
/// </summary>
public static class LearningConstants
{
    /// <summary>
    /// Difficulty levels for courses and content
    /// </summary>
    public static class DifficultyLevels
    {
        public const string Beginner = "beginner";
        public const string Intermediate = "intermediate";
        public const string Advanced = "advanced";
        public const string Expert = "expert";
        
        public static readonly IReadOnlyList<string> All = new[]
        {
            Beginner, Intermediate, Advanced, Expert
        };
        
        public static bool IsValid(string? level) =>
            !string.IsNullOrWhiteSpace(level) && All.Contains(level.ToLowerInvariant());
    }
    
    /// <summary>
    /// Content types for learning materials
    /// </summary>
    public static class ContentTypes
    {
        public const string Video = "video";
        public const string Article = "article";
        public const string Quiz = "quiz";
        public const string Assignment = "assignment";
        public const string Interactive = "interactive";
        public const string Document = "document";
        public const string Audio = "audio";
        public const string LiveSession = "live-session";
        
        public static readonly IReadOnlyList<string> All = new[]
        {
            Video, Article, Quiz, Assignment, Interactive, Document, Audio, LiveSession
        };
        
        public static bool IsValid(string? type) =>
            !string.IsNullOrWhiteSpace(type) && All.Contains(type.ToLowerInvariant());
    }
    
    /// <summary>
    /// Course status values
    /// </summary>
    public static class CourseStatus
    {
        public const string Draft = "draft";
        public const string Review = "review";
        public const string Published = "published";
        public const string Archived = "archived";
        public const string Suspended = "suspended";
        
        public static readonly IReadOnlyList<string> All = new[]
        {
            Draft, Review, Published, Archived, Suspended
        };
    }
    
    /// <summary>
    /// Learning path status values
    /// </summary>
    public static class LearningPathStatus
    {
        public const string Draft = "draft";
        public const string Published = "published";
        public const string Archived = "archived";
        
        public static readonly IReadOnlyList<string> All = new[]
        {
            Draft, Published, Archived
        };
    }
    
    /// <summary>
    /// Pagination defaults
    /// </summary>
    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
        public const int MinPageSize = 1;
        public const int DefaultPage = 1;
    }
    
    /// <summary>
    /// Cache key prefixes for learning modules
    /// </summary>
    public static class CacheKeys
    {
        public const string CoursePrefix = "learning:course:";
        public const string EnrollmentPrefix = "learning:enrollment:";
        public const string ProgressPrefix = "learning:progress:";
        public const string LearningPathPrefix = "learning:path:";
        public const string RecommendationPrefix = "learning:recommendation:";
        public const string DiscoveryPrefix = "learning:discovery:";
        public const string SocialPrefix = "learning:social:";
        public const string FeedPrefix = "learning:feed:";
        
        public static string ForCourse(Guid courseId) => $"{CoursePrefix}{courseId}";
        public static string ForEnrollment(Guid userId, Guid courseId) => $"{EnrollmentPrefix}{userId}:{courseId}";
        public static string ForProgress(Guid userId, Guid entityId) => $"{ProgressPrefix}{userId}:{entityId}";
        public static string ForLearningPath(Guid pathId) => $"{LearningPathPrefix}{pathId}";
        public static string ForRecommendations(Guid userId) => $"{RecommendationPrefix}{userId}";
        public static string ForDiscovery(Guid tenantId, string category) => $"{DiscoveryPrefix}{tenantId}:{category}";
        public static string ForSocialActivity(Guid userId) => $"{SocialPrefix}{userId}";
        public static string ForFeed(Guid userId) => $"{FeedPrefix}{userId}";
    }
    
    /// <summary>
    /// Event types for learning activities
    /// </summary>
    public static class EventTypes
    {
        // Course events
        public const string CourseCreated = "course.created";
        public const string CourseUpdated = "course.updated";
        public const string CoursePublished = "course.published";
        public const string CourseArchived = "course.archived";
        
        // Enrollment events
        public const string UserEnrolled = "enrollment.created";
        public const string EnrollmentCompleted = "enrollment.completed";
        public const string EnrollmentCancelled = "enrollment.cancelled";
        
        // Progress events
        public const string ProgressUpdated = "progress.updated";
        public const string ContentCompleted = "content.completed";
        public const string QuizCompleted = "quiz.completed";
        public const string AssignmentSubmitted = "assignment.submitted";
        
        // Learning path events
        public const string LearningPathCreated = "learning-path.created";
        public const string LearningPathUpdated = "learning-path.updated";
        public const string LearningPathEnrolled = "learning-path.enrolled";
        public const string LearningPathCompleted = "learning-path.completed";
        
        // Social events
        public const string ReviewCreated = "review.created";
        public const string CommentCreated = "comment.created";
        public const string BookmarkCreated = "bookmark.created";
        public const string AchievementEarned = "achievement.earned";
        public const string CertificateIssued = "certificate.issued";
    }
    
    /// <summary>
    /// Capability names for learning modules
    /// </summary>
    public static class Capabilities
    {
        public const string Discovery = "learning:discovery";
        public const string LearningPaths = "learning:paths";
        public const string Recommendations = "learning:recommendations";
        public const string Social = "learning:social";
        public const string PersonalizedFeed = "learning:feed";
        public const string Bookmarks = "learning:bookmarks";
        public const string SocialProof = "learning:social-proof";
        public const string AdvancedAnalytics = "learning:analytics";
        public const string Certifications = "learning:certifications";
        public const string Gamification = "learning:gamification";
    }
}
