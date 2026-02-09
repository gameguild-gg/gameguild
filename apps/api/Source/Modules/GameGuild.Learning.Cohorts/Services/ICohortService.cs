
namespace GameGuild.Learning.Cohorts;

/// <summary>
/// Service interface for cohort management
/// </summary>
public interface ICohortService
{
    /// <summary>
    /// Creates a new cohort for a course
    /// </summary>
    Task<Result<Cohort>> CreateCohortAsync(CreateCohortRequest request);

    /// <summary>
    /// Gets a cohort by ID
    /// </summary>
    Task<Cohort?> GetCohortByIdAsync(Guid id);

    /// <summary>
    /// Gets all cohorts for a course
    /// </summary>
    Task<IEnumerable<Cohort>> GetCoursCohortsAsync(Guid courseId, Guid? tenantId = null);

    /// <summary>
    /// Gets active cohorts for a course
    /// </summary>
    Task<IEnumerable<Cohort>> GetActiveCohortsAsync(Guid courseId, Guid? tenantId = null);

    /// <summary>
    /// Updates a cohort
    /// </summary>
    Task<Result<Cohort>> UpdateCohortAsync(Guid id, UpdateCohortRequest request);

    /// <summary>
    /// Opens a cohort for enrollment
    /// </summary>
    Task<Result<Cohort>> OpenCohortAsync(Guid id);

    /// <summary>
    /// Closes a cohort for enrollment
    /// </summary>
    Task<Result<Cohort>> CloseCohortAsync(Guid id);

    /// <summary>
    /// Marks a cohort as completed
    /// </summary>
    Task<Result<Cohort>> CompleteCohortAsync(Guid id);

    /// <summary>
    /// Cancels a cohort
    /// </summary>
    Task<Result<Cohort>> CancelCohortAsync(Guid id);

    /// <summary>
    /// Deletes a cohort
    /// </summary>
    Task<Result> DeleteCohortAsync(Guid id);

    /// <summary>
    /// Gets enrollable cohorts (open with capacity)
    /// </summary>
    Task<IEnumerable<Cohort>> GetEnrollableCohortsAsync(Guid courseId, Guid? tenantId = null);
}

/// <summary>
/// Request to create a new cohort
/// </summary>
public sealed record CreateCohortRequest(
    Guid CourseId,
    string Name,
    DateTime StartDate,
    DateTime EndDate,
    int MaxCapacity,
    string? Description = null,
    Guid? TenantId = null,
    Guid? InstructorId = null,
    string? MeetingSchedule = null);

/// <summary>
/// Request to update a cohort
/// </summary>
public sealed record UpdateCohortRequest(
    string? Name = null,
    string? Description = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? MaxCapacity = null,
    Guid? InstructorId = null,
    string? MeetingSchedule = null);
