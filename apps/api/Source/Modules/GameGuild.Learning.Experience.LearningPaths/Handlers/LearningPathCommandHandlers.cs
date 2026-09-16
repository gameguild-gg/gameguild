using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Experience.LearningPaths;

/// <summary>
/// Command handlers for Learning Paths module
/// </summary>
public sealed class LearningPathCommandHandlers(IApplicationDbContext context, ILogger<LearningPathCommandHandlers> logger)
    : ICommandHandler<CreateLearningPathCommand, LearningPath>,
      ICommandHandler<UpdateLearningPathCommand, LearningPath?>,
      ICommandHandler<DeleteLearningPathCommand, bool>,
      ICommandHandler<PublishLearningPathCommand, LearningPath?>,
      ICommandHandler<UnpublishLearningPathCommand, LearningPath?>,
      ICommandHandler<AddCourseToPathCommand, LearningPath?>,
      ICommandHandler<RemoveCourseFromPathCommand, bool>,
      ICommandHandler<ReorderPathCoursesCommand, LearningPath?>,
      ICommandHandler<EnrollInPathCommand, LearningPathEnrollment>,
      ICommandHandler<UnenrollFromPathCommand, bool>,
      ICommandHandler<UpdatePathProgressCommand, LearningPathEnrollment?>,
      ICommandHandler<CompletePathCommand, LearningPathEnrollment?>,
      ICommandHandler<AbandonPathCommand, bool>
{
    // ===== LEARNING PATH CRUD HANDLERS =====

    public async Task<LearningPath> Handle(CreateLearningPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Creating learning path: {Title}", request.Title);

        // Generate slug from title
        var slug = GenerateSlug(request.Title);

        // Ensure slug uniqueness
        var existingSlug = await context.Set<LearningPath>()
            .Where(lp => lp.Slug == slug && lp.TenantId == request.TenantId && lp.DeletedAt == null)
            .FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);

        if (existingSlug != null)
        {
            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..8]}";
        }

        var learningPath = LearningPath.Create(
            creatorId: request.CreatorId,
            title: request.Title,
            slug: slug,
            difficulty: request.Difficulty,
            tenantId: request.TenantId,
            description: request.Description,
            imageUrl: request.ImageUrl,
            estimatedHours: request.EstimatedHours
        );

        context.Set<LearningPath>().Add(learningPath);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Created learning path with ID: {Id}", learningPath.Id);
        return learningPath;
    }

    public async Task<LearningPath?> Handle(UpdateLearningPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating learning path: {Id}", request.Id);

        var learningPath = await context.Set<LearningPath>()
            .Where(lp => lp.DeletedAt == null)
            .FirstOrDefaultAsync(lp => lp.Id == request.Id, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.Id);
            return null;
        }

        learningPath.Update(
            request.Title,
            request.Description,
            request.ImageUrl,
            request.EstimatedHours,
            request.Difficulty,
            request.IsFeatured);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated learning path: {Id}", request.Id);
        return learningPath;
    }

    public async Task<bool> Handle(DeleteLearningPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Deleting learning path: {Id}", request.Id);

        var learningPath = await context.Set<LearningPath>()
            .FirstOrDefaultAsync(lp => lp.Id == request.Id, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.Id);
            return false;
        }

        learningPath.SoftDelete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Deleted learning path: {Id}", request.Id);
        return true;
    }

    // ===== LIFECYCLE HANDLERS =====

    public async Task<LearningPath?> Handle(PublishLearningPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Publishing learning path: {Id}", request.Id);

        var learningPath = await context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null)
            .FirstOrDefaultAsync(lp => lp.Id == request.Id, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.Id);
            return null;
        }

        // Validate: path should have at least one course
        if (!learningPath.Courses.Any())
        {
            logger.LogWarning("Cannot publish learning path with no courses: {Id}", request.Id);
            throw new InvalidOperationException("Learning path must have at least one course to be published");
        }

        learningPath.Publish();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Published learning path: {Id}", request.Id);
        return learningPath;
    }

    public async Task<LearningPath?> Handle(UnpublishLearningPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unpublishing learning path: {Id}", request.Id);

        var learningPath = await context.Set<LearningPath>()
            .Where(lp => lp.DeletedAt == null)
            .FirstOrDefaultAsync(lp => lp.Id == request.Id, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.Id);
            return null;
        }

        learningPath.Unpublish();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Unpublished learning path: {Id}", request.Id);
        return learningPath;
    }

    // ===== COURSE MANAGEMENT HANDLERS =====

    public async Task<LearningPath?> Handle(AddCourseToPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding course {CourseId} to path {PathId}", request.CourseId, request.LearningPathId);

        var learningPath = await context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null)
            .FirstOrDefaultAsync(lp => lp.Id == request.LearningPathId, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.LearningPathId);
            return null;
        }

        // Check if course already exists in path
        if (learningPath.Courses.Any(c => c.CourseId == request.CourseId))
        {
            logger.LogWarning("Course {CourseId} already exists in path {PathId}", request.CourseId, request.LearningPathId);
            throw new InvalidOperationException("Course already exists in this learning path");
        }

        learningPath.AddCourse(request.CourseId, request.Order, request.IsRequired);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Added course {CourseId} to path {PathId}", request.CourseId, request.LearningPathId);
        return learningPath;
    }

    public async Task<bool> Handle(RemoveCourseFromPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Removing course {CourseId} from path {PathId}", request.CourseId, request.LearningPathId);

        var course = await context.Set<LearningPathCourse>()
            .FirstOrDefaultAsync(c => c.LearningPathId == request.LearningPathId && c.CourseId == request.CourseId, cancellationToken).ConfigureAwait(false);

        if (course == null)
        {
            logger.LogWarning("Course {CourseId} not found in path {PathId}", request.CourseId, request.LearningPathId);
            return false;
        }

        context.Set<LearningPathCourse>().Remove(course);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Removed course {CourseId} from path {PathId}", request.CourseId, request.LearningPathId);
        return true;
    }

    public async Task<LearningPath?> Handle(ReorderPathCoursesCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Reordering courses in path: {PathId}", request.LearningPathId);

        var learningPath = await context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null)
            .FirstOrDefaultAsync(lp => lp.Id == request.LearningPathId, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            logger.LogWarning("Learning path not found: {Id}", request.LearningPathId);
            return null;
        }

        var requestedOrders = request.Courses.ToList();
        var requestedCourseIds = requestedOrders.Select(item => item.CourseId).ToHashSet();
        if (requestedCourseIds.Count != requestedOrders.Count || requestedOrders.Select(item => item.Order).Distinct().Count() != requestedOrders.Count)
        {
            throw new InvalidOperationException("Each course and order must be unique when reordering a learning path");
        }

        var coursesById = learningPath.Courses.ToDictionary(course => course.CourseId);
        if (requestedOrders.Any(item => !coursesById.ContainsKey(item.CourseId)))
        {
            throw new InvalidOperationException("One or more courses do not belong to this learning path");
        }

        var resultingOrders = learningPath.Courses
            .Select(course => requestedCourseIds.Contains(course.CourseId)
                ? requestedOrders.First(item => item.CourseId == course.CourseId).Order
                : course.Order)
            .ToList();
        if (resultingOrders.Distinct().Count() != resultingOrders.Count)
        {
            throw new InvalidOperationException("Course orders must be unique within a learning path");
        }

        await using var transaction = await context.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        for (var index = 0; index < requestedOrders.Count; index++)
        {
            coursesById[requestedOrders[index].CourseId].SetOrder(-(index + 1));
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var courseOrder in requestedOrders)
        {
            coursesById[courseOrder.CourseId].SetOrder(courseOrder.Order);
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Reordered courses in path: {PathId}", request.LearningPathId);
        return learningPath;
    }

    // ===== ENROLLMENT HANDLERS =====

    public async Task<LearningPathEnrollment> Handle(EnrollInPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Enrolling user {UserId} in path {PathId}", request.UserId, request.LearningPathId);

        var learningPath = await context.Set<LearningPath>()
            .Include(lp => lp.Courses)
            .Where(lp => lp.DeletedAt == null && lp.IsPublished)
            .FirstOrDefaultAsync(lp => lp.Id == request.LearningPathId, cancellationToken).ConfigureAwait(false);

        if (learningPath == null)
        {
            throw new InvalidOperationException("Learning path not found or not published");
        }

        // Check if already enrolled
        var existingEnrollment = await context.Set<LearningPathEnrollment>()
            .FirstOrDefaultAsync(e => e.LearningPathId == request.LearningPathId && e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (existingEnrollment != null)
        {
            logger.LogWarning("User {UserId} already enrolled in path {PathId}", request.UserId, request.LearningPathId);
            throw new InvalidOperationException("User is already enrolled in this learning path");
        }

        var enrollment = LearningPathEnrollment.Create(
            learningPathId: request.LearningPathId,
            userId: request.UserId,
            totalCourses: learningPath.Courses.Count
        );

        context.Set<LearningPathEnrollment>().Add(enrollment);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Enrolled user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
        return enrollment;
    }

    public async Task<bool> Handle(UnenrollFromPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Unenrolling user {UserId} from path {PathId}", request.UserId, request.LearningPathId);

        var enrollment = await context.Set<LearningPathEnrollment>()
            .FirstOrDefaultAsync(e => e.LearningPathId == request.LearningPathId && e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (enrollment == null)
        {
            logger.LogWarning("Enrollment not found for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
            return false;
        }

        enrollment.SoftDelete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Unenrolled user {UserId} from path {PathId}", request.UserId, request.LearningPathId);
        return true;
    }

    public async Task<LearningPathEnrollment?> Handle(UpdatePathProgressCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Updating progress for user {UserId} in path {PathId}: {Completed} courses", 
            request.UserId, request.LearningPathId, request.CoursesCompleted);

        var enrollment = await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(e => e.LearningPathId == request.LearningPathId && e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (enrollment == null)
        {
            logger.LogWarning("Enrollment not found for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
            return null;
        }

        enrollment.UpdateProgress(request.CoursesCompleted);
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Updated progress for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
        return enrollment;
    }

    public async Task<LearningPathEnrollment?> Handle(CompletePathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Completing path {PathId} for user {UserId}", request.LearningPathId, request.UserId);

        var enrollment = await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(e => e.LearningPathId == request.LearningPathId && e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (enrollment == null)
        {
            logger.LogWarning("Enrollment not found for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
            return null;
        }

        enrollment.Complete();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Completed path {PathId} for user {UserId}", request.LearningPathId, request.UserId);
        return enrollment;
    }

    public async Task<bool> Handle(AbandonPathCommand request, CancellationToken cancellationToken)
    {
        logger.LogInformation("User {UserId} abandoning path {PathId}", request.UserId, request.LearningPathId);

        var enrollment = await context.Set<LearningPathEnrollment>()
            .Where(e => e.DeletedAt == null)
            .FirstOrDefaultAsync(e => e.LearningPathId == request.LearningPathId && e.UserId == request.UserId, cancellationToken).ConfigureAwait(false);

        if (enrollment == null)
        {
            logger.LogWarning("Enrollment not found for user {UserId} in path {PathId}", request.UserId, request.LearningPathId);
            return false;
        }

        enrollment.Abandon();
        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        logger.LogInformation("User {UserId} abandoned path {PathId}", request.UserId, request.LearningPathId);
        return true;
    }

    // ===== HELPER METHODS =====

    private static string GenerateSlug(string title)
    {
        return title.ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace(",", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("'", "")
            .Replace("\"", "");
    }
}
