using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Cohorts;

/// <summary>
/// Service for cohort management
/// </summary>
public class CohortService : ICohortService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CohortService> _logger;

    public CohortService(IApplicationDbContext context, ILogger<CohortService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<Cohort>> CreateCohortAsync(CreateCohortRequest request)
    {
        try
        {
            var cohort = Cohort.Create(
                request.CourseId,
                request.Name,
                request.StartDate,
                request.EndDate,
                request.MaxCapacity,
                request.TenantId,
                request.InstructorId);

            if (request.Description != null)
            {
                cohort.SetDescription(request.Description);
            }

            if (request.MeetingSchedule != null)
            {
                cohort.SetMeetingSchedule(request.MeetingSchedule);
            }

            _context.Set<Cohort>().Add(cohort);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Cohort created: {CohortId} for course {CourseId}", cohort.Id, request.CourseId);

            return Result.Success(cohort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating cohort for course {CourseId}", request.CourseId);
            return Result.Failure<Cohort>(Error.Failure("CreateCohort", "Failed to create cohort"));
        }
    }

    public async Task<Cohort?> GetCohortByIdAsync(Guid id)
    {
        return await _context.Set<Cohort>()
            .FirstOrDefaultAsync(c => c.Id == id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Cohort>> GetCoursCohortsAsync(Guid courseId, Guid? tenantId = null)
    {
        var query = _context.Set<Cohort>()
            .Where(c => c.CourseId == courseId);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == null || c.TenantId == tenantId);
        }

        return await query
            .OrderByDescending(c => c.StartDate)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<Cohort>> GetActiveCohortsAsync(Guid courseId, Guid? tenantId = null)
    {
        var query = _context.Set<Cohort>()
            .Where(c => c.CourseId == courseId)
            .Where(c => c.Status == CohortStatus.Active);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == null || c.TenantId == tenantId);
        }

        return await query
            .OrderBy(c => c.StartDate)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<Cohort>> GetEnrollableCohortsAsync(Guid courseId, Guid? tenantId = null)
    {
        var query = _context.Set<Cohort>()
            .Where(c => c.CourseId == courseId)
            .Where(c => c.Status == CohortStatus.Active)
            .Where(c => c.IsOpen)
            .Where(c => c.CurrentEnrollmentCount < c.MaxCapacity);

        if (tenantId.HasValue)
        {
            query = query.Where(c => c.TenantId == null || c.TenantId == tenantId);
        }

        return await query
            .OrderBy(c => c.StartDate)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<Result<Cohort>> UpdateCohortAsync(Guid id, UpdateCohortRequest request)
    {
        try
        {
            var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
            if (cohort == null)
            {
                return Result.Failure<Cohort>(Error.NotFound("Cohort", "Cohort not found"));
            }

            cohort.Update(
                request.Name,
                request.Description,
                request.StartDate,
                request.EndDate,
                request.MaxCapacity,
                request.InstructorId,
                request.MeetingSchedule);

            _context.Set<Cohort>().Update(cohort);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Cohort updated: {CohortId}", id);

            return Result.Success(cohort);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating cohort {CohortId}", id);
            return Result.Failure<Cohort>(Error.Failure("UpdateCohort", "Failed to update cohort"));
        }
    }

    public async Task<Result<Cohort>> OpenCohortAsync(Guid id)
    {
        var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return Result.Failure<Cohort>(Error.NotFound("Cohort", "Cohort not found"));
        }

        cohort.Open();
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cohort opened: {CohortId}", id);
        return Result.Success(cohort);
    }

    public async Task<Result<Cohort>> CloseCohortAsync(Guid id)
    {
        var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return Result.Failure<Cohort>(Error.NotFound("Cohort", "Cohort not found"));
        }

        cohort.Close();
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cohort closed: {CohortId}", id);
        return Result.Success(cohort);
    }

    public async Task<Result<Cohort>> CompleteCohortAsync(Guid id)
    {
        var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return Result.Failure<Cohort>(Error.NotFound("Cohort", "Cohort not found"));
        }

        cohort.Complete();
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cohort completed: {CohortId}", id);
        return Result.Success(cohort);
    }

    public async Task<Result<Cohort>> CancelCohortAsync(Guid id)
    {
        var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return Result.Failure<Cohort>(Error.NotFound("Cohort", "Cohort not found"));
        }

        cohort.Cancel();
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cohort cancelled: {CohortId}", id);
        return Result.Success(cohort);
    }

    public async Task<Result> DeleteCohortAsync(Guid id)
    {
        var cohort = await GetCohortByIdAsync(id).ConfigureAwait(false);
        if (cohort == null)
        {
            return Result.Failure(Error.NotFound("Cohort", "Cohort not found"));
        }

        if (cohort.CurrentEnrollmentCount > 0)
        {
            return Result.Failure(Error.Validation("Cohort", "Cannot delete cohort with enrolled students"));
        }

        _context.Set<Cohort>().Remove(cohort);
        await _context.SaveChangesAsync().ConfigureAwait(false);

        _logger.LogInformation("Cohort deleted: {CohortId}", id);
        return Result.Success();
    }
}
