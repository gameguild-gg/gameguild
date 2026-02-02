using GameGuild.CQRS;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Service implementation for Learning Paths operations
/// </summary>
public class LearningPathService(IMediator mediator) : ILearningPathService
{
    // ===== LEARNING PATH CRUD =====

    public async Task<LearningPath?> GetPathByIdAsync(Guid id, bool includeCourses = false)
    {
        return await mediator.Send(new GetPathByIdQuery(id, includeCourses));
    }

    public async Task<LearningPath?> GetPathBySlugAsync(string slug, Guid? tenantId = null)
    {
        return await mediator.Send(new GetPathBySlugQuery(slug, tenantId));
    }

    public async Task<IEnumerable<LearningPath>> GetPublishedPathsAsync(Guid? tenantId = null, LearningPathDifficulty? difficulty = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetPublishedPathsQuery(tenantId, difficulty, skip, take));
    }

    public async Task<IEnumerable<LearningPath>> GetFeaturedPathsAsync(Guid? tenantId = null, int take = 10)
    {
        return await mediator.Send(new GetFeaturedPathsQuery(tenantId, take));
    }

    public async Task<IEnumerable<LearningPath>> GetPathsByCreatorAsync(Guid creatorId, bool includeUnpublished = false, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetPathsByCreatorQuery(creatorId, includeUnpublished, skip, take));
    }

    public async Task<IEnumerable<LearningPath>> SearchPathsAsync(string searchTerm, Guid? tenantId = null, LearningPathDifficulty? difficulty = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new SearchPathsQuery(searchTerm, tenantId, difficulty, skip, take));
    }

    public async Task<LearningPath> CreatePathAsync(CreateLearningPathDto dto, Guid creatorId, Guid? tenantId = null)
    {
        return await mediator.Send(new CreateLearningPathCommand(
            CreatorId: creatorId,
            Title: dto.Title,
            Difficulty: dto.Difficulty,
            TenantId: tenantId,
            Description: dto.Description,
            ImageUrl: dto.ImageUrl,
            EstimatedHours: dto.EstimatedHours
        ));
    }

    public async Task<LearningPath?> UpdatePathAsync(Guid id, UpdateLearningPathDto dto)
    {
        return await mediator.Send(new UpdateLearningPathCommand(
            Id: id,
            Title: dto.Title,
            Description: dto.Description,
            ImageUrl: dto.ImageUrl,
            EstimatedHours: dto.EstimatedHours,
            Difficulty: dto.Difficulty,
            IsFeatured: dto.IsFeatured
        ));
    }

    public async Task<bool> DeletePathAsync(Guid id)
    {
        return await mediator.Send(new DeleteLearningPathCommand(id));
    }

    // ===== LIFECYCLE =====

    public async Task<LearningPath?> PublishPathAsync(Guid id)
    {
        return await mediator.Send(new PublishLearningPathCommand(id));
    }

    public async Task<LearningPath?> UnpublishPathAsync(Guid id)
    {
        return await mediator.Send(new UnpublishLearningPathCommand(id));
    }

    // ===== COURSE MANAGEMENT =====

    public async Task<LearningPath?> AddCourseToPathAsync(Guid pathId, AddCourseToPathDto dto)
    {
        return await mediator.Send(new AddCourseToPathCommand(
            LearningPathId: pathId,
            CourseId: dto.CourseId,
            Order: dto.Order,
            IsRequired: dto.IsRequired
        ));
    }

    public async Task<bool> RemoveCourseFromPathAsync(Guid pathId, Guid courseId)
    {
        return await mediator.Send(new RemoveCourseFromPathCommand(pathId, courseId));
    }

    public async Task<LearningPath?> ReorderCoursesAsync(Guid pathId, ReorderCoursesDto dto)
    {
        return await mediator.Send(new ReorderPathCoursesCommand(pathId, dto.Courses));
    }

    // ===== ENROLLMENT =====

    public async Task<LearningPathEnrollment> EnrollAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new EnrollInPathCommand(pathId, userId));
    }

    public async Task<bool> UnenrollAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new UnenrollFromPathCommand(pathId, userId));
    }

    public async Task<LearningPathEnrollment?> UpdateProgressAsync(Guid pathId, Guid userId, UpdatePathProgressDto dto)
    {
        return await mediator.Send(new UpdatePathProgressCommand(pathId, userId, dto.CoursesCompleted));
    }

    public async Task<LearningPathEnrollment?> CompletePathAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new CompletePathCommand(pathId, userId));
    }

    public async Task<bool> AbandonPathAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new AbandonPathCommand(pathId, userId));
    }

    // ===== ENROLLMENT QUERIES =====

    public async Task<bool> IsEnrolledAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new CheckPathEnrollmentQuery(userId, pathId));
    }

    public async Task<LearningPathEnrollment?> GetEnrollmentAsync(Guid pathId, Guid userId)
    {
        return await mediator.Send(new GetUserPathEnrollmentQuery(userId, pathId));
    }

    public async Task<IEnumerable<LearningPathEnrollment>> GetUserEnrollmentsAsync(Guid userId, LearningPathEnrollmentStatus? status = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetUserEnrolledPathsQuery(userId, status, skip, take));
    }

    public async Task<IEnumerable<LearningPathEnrollment>> GetPathEnrollmentsAsync(Guid pathId, LearningPathEnrollmentStatus? status = null, int skip = 0, int take = 50)
    {
        return await mediator.Send(new GetPathEnrollmentsQuery(pathId, status, skip, take));
    }

    public async Task<IEnumerable<LearningPathEnrollment>> GetUserCompletedPathsAsync(Guid userId, int skip = 0, int take = 20)
    {
        return await mediator.Send(new GetUserCompletedPathsQuery(userId, skip, take));
    }

    // ===== STATISTICS =====

    public async Task<LearningPathStatisticsDto?> GetPathStatisticsAsync(Guid pathId)
    {
        return await mediator.Send(new GetPathStatisticsQuery(pathId));
    }

    public async Task<IEnumerable<LearningPath>> GetPopularPathsAsync(Guid? tenantId = null, int daysBack = 30, int take = 10)
    {
        return await mediator.Send(new GetPopularPathsQuery(tenantId, daysBack, take));
    }
}
