namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Service interface for Learning Paths operations
/// </summary>
public interface ILearningPathService
{
    // Learning Path CRUD
    Task<LearningPath?> GetPathByIdAsync(Guid id, bool includeCourses = false);
    Task<LearningPath?> GetPathBySlugAsync(string slug, Guid? tenantId = null);
    Task<IEnumerable<LearningPath>> GetPublishedPathsAsync(Guid? tenantId = null, LearningPathDifficulty? difficulty = null, int skip = 0, int take = 50);
    Task<IEnumerable<LearningPath>> GetFeaturedPathsAsync(Guid? tenantId = null, int take = 10);
    Task<IEnumerable<LearningPath>> GetPathsByCreatorAsync(Guid creatorId, bool includeUnpublished = false, int skip = 0, int take = 50);
    Task<IEnumerable<LearningPath>> SearchPathsAsync(string searchTerm, Guid? tenantId = null, LearningPathDifficulty? difficulty = null, int skip = 0, int take = 50);
    Task<LearningPath> CreatePathAsync(CreateLearningPathDto dto, Guid creatorId, Guid? tenantId = null);
    Task<LearningPath?> UpdatePathAsync(Guid id, UpdateLearningPathDto dto);
    Task<bool> DeletePathAsync(Guid id);

    // Learning Path Lifecycle
    Task<LearningPath?> PublishPathAsync(Guid id);
    Task<LearningPath?> UnpublishPathAsync(Guid id);

    // Course Management
    Task<LearningPath?> AddCourseToPathAsync(Guid pathId, AddCourseToPathDto dto);
    Task<bool> RemoveCourseFromPathAsync(Guid pathId, Guid courseId);
    Task<LearningPath?> ReorderCoursesAsync(Guid pathId, ReorderCoursesDto dto);

    // Enrollment
    Task<LearningPathEnrollment> EnrollAsync(Guid pathId, Guid userId);
    Task<bool> UnenrollAsync(Guid pathId, Guid userId);
    Task<LearningPathEnrollment?> UpdateProgressAsync(Guid pathId, Guid userId, UpdatePathProgressDto dto);
    Task<LearningPathEnrollment?> CompletePathAsync(Guid pathId, Guid userId);
    Task<bool> AbandonPathAsync(Guid pathId, Guid userId);

    // Enrollment Queries
    Task<bool> IsEnrolledAsync(Guid pathId, Guid userId);
    Task<LearningPathEnrollment?> GetEnrollmentAsync(Guid pathId, Guid userId);
    Task<IEnumerable<LearningPathEnrollment>> GetUserEnrollmentsAsync(Guid userId, LearningPathEnrollmentStatus? status = null, int skip = 0, int take = 50);
    Task<IEnumerable<LearningPathEnrollment>> GetPathEnrollmentsAsync(Guid pathId, LearningPathEnrollmentStatus? status = null, int skip = 0, int take = 50);
    Task<IEnumerable<LearningPathEnrollment>> GetUserCompletedPathsAsync(Guid userId, int skip = 0, int take = 20);

    // Statistics
    Task<LearningPathStatisticsDto?> GetPathStatisticsAsync(Guid pathId);
    Task<IEnumerable<LearningPath>> GetPopularPathsAsync(Guid? tenantId = null, int daysBack = 30, int take = 10);
}
