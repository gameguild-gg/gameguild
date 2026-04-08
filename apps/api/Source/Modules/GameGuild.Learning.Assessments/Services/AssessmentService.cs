using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// Service implementation for assessment management and submission processing
/// </summary>
public class AssessmentService : IAssessmentService
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AssessmentService> _logger;

    public AssessmentService(IApplicationDbContext context, ILogger<AssessmentService> logger)
    {
        _context = context;
        _logger = logger;
    }

    // ===== ASSESSMENT MANAGEMENT =====

    public async Task<Result<Assessment>> CreateAssessmentAsync(CreateAssessmentRequest request)
    {
        try
        {
            var assessment = Assessment.Create(
                request.CourseId,
                request.Title,
                request.Type,
                request.MaxScore,
                request.PassingScore,
                request.IsRequired);

            // Set optional properties using internal setters
            assessment.SetDescription(request.Description);
            assessment.SetTimeLimit(request.TimeLimitMinutes);
            assessment.SetMaxAttempts(request.MaxAttempts);
            assessment.SetAvailability(request.AvailableFrom, request.AvailableUntil);

            _context.Set<Assessment>().Add(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment created: {AssessmentId} for course {CourseId}", assessment.Id, request.CourseId);

            return Result.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assessment for course {CourseId}", request.CourseId);
            return Result.Failure<Assessment>(Error.Failure("CreateAssessment", "Failed to create assessment"));
        }
    }

    public async Task<Assessment?> GetAssessmentByIdAsync(Guid id)
    {
        return await _context.Set<Assessment>()
            .FirstOrDefaultAsync(a => a.Id == id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<Assessment>> GetCourseAssessmentsAsync(Guid courseId)
    {
        return await _context.Set<Assessment>()
            .Where(a => a.CourseId == courseId)
            .OrderBy(a => a.Order)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<Result<Assessment>> UpdateAssessmentAsync(Guid id, UpdateAssessmentRequest request)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(id).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<Assessment>(Error.NotFound("Assessment", "Assessment not found"));
            }

            assessment.Update(
                request.Title,
                request.Description,
                request.MaxScore,
                request.PassingScore,
                request.TimeLimitMinutes,
                request.MaxAttempts,
                request.IsRequired,
                request.AvailableFrom,
                request.AvailableUntil,
                request.ContentId,
                request.ClearContentId);

            _context.Set<Assessment>().Update(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment updated: {AssessmentId}", id);

            return Result.Success(assessment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assessment {AssessmentId}", id);
            return Result.Failure<Assessment>(Error.Failure("UpdateAssessment", "Failed to update assessment"));
        }
    }

    public async Task<Result> DeleteAssessmentAsync(Guid id)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(id).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure(Error.NotFound("Assessment", "Assessment not found"));
            }

            assessment.SoftDelete();
            _context.Set<Assessment>().Update(assessment);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Assessment deleted: {AssessmentId}", id);

            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assessment {AssessmentId}", id);
            return Result.Failure(Error.Failure("DeleteAssessment", "Failed to delete assessment"));
        }
    }

    // ===== SUBMISSION MANAGEMENT =====

    public async Task<Result<AssessmentSubmission>> StartSubmissionAsync(Guid assessmentId, Guid enrollmentId, Guid userId)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Assessment", "Assessment not found"));
            }

            if (!assessment.IsAvailable())
            {
                return Result.Failure<AssessmentSubmission>(Error.Validation("Assessment", "Assessment is not currently available"));
            }

            // Check attempt limit
            var attemptCount = await GetAttemptCountAsync(assessmentId, enrollmentId).ConfigureAwait(false);
            if (assessment.MaxAttempts.HasValue && attemptCount >= assessment.MaxAttempts.Value)
            {
                return Result.Failure<AssessmentSubmission>(Error.Validation("Assessment", "Maximum attempts reached"));
            }

            var submission = AssessmentSubmission.Start(assessmentId, enrollmentId, userId, attemptCount + 1);

            _context.Set<AssessmentSubmission>().Add(submission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Submission started: {SubmissionId} for assessment {AssessmentId}", submission.Id, assessmentId);

            return Result.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting submission for assessment {AssessmentId}", assessmentId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("StartSubmission", "Failed to start submission"));
        }
    }

    public async Task<Result<AssessmentSubmission>> SubmitAsync(Guid submissionId)
    {
        try
        {
            var submission = await GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
            if (submission == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Submission", "Submission not found"));
            }

            if (submission.Status != SubmissionStatus.InProgress)
            {
                return Result.Failure<AssessmentSubmission>(Error.Validation("Submission", "Submission is not in progress"));
            }

            submission.Submit();
            _context.Set<AssessmentSubmission>().Update(submission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Submission submitted: {SubmissionId}", submissionId);

            return Result.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting {SubmissionId}", submissionId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("Submit", "Failed to submit"));
        }
    }

    public async Task<Result<AssessmentSubmission>> GradeSubmissionAsync(Guid submissionId, GradeSubmissionRequest request)
    {
        try
        {
            var submission = await GetSubmissionByIdAsync(submissionId).ConfigureAwait(false);
            if (submission == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Submission", "Submission not found"));
            }

            var assessment = await GetAssessmentByIdAsync(submission.AssessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<AssessmentSubmission>(Error.NotFound("Assessment", "Assessment not found"));
            }

            submission.Grade(request.Score, assessment.PassingScore, request.GradedBy, request.Feedback);
            _context.Set<AssessmentSubmission>().Update(submission);
            await _context.SaveChangesAsync().ConfigureAwait(false);

            _logger.LogInformation("Submission graded: {SubmissionId} with score {Score}", submissionId, request.Score);

            return Result.Success(submission);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading submission {SubmissionId}", submissionId);
            return Result.Failure<AssessmentSubmission>(Error.Failure("GradeSubmission", "Failed to grade submission"));
        }
    }

    public async Task<AssessmentSubmission?> GetSubmissionByIdAsync(Guid id)
    {
        return await _context.Set<AssessmentSubmission>()
            .FirstOrDefaultAsync(s => s.Id == id).ConfigureAwait(false);
    }

    public async Task<IEnumerable<AssessmentSubmission>> GetAssessmentSubmissionsAsync(Guid assessmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.AssessmentId == assessmentId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<IEnumerable<AssessmentSubmission>> GetUserSubmissionsAsync(Guid enrollmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .Where(s => s.EnrollmentId == enrollmentId)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync().ConfigureAwait(false);
    }

    public async Task<int> GetAttemptCountAsync(Guid assessmentId, Guid enrollmentId)
    {
        return await _context.Set<AssessmentSubmission>()
            .CountAsync(s => s.AssessmentId == assessmentId && s.EnrollmentId == enrollmentId).ConfigureAwait(false);
    }

    public async Task<Result<bool>> CanAttemptAsync(Guid assessmentId, Guid enrollmentId)
    {
        try
        {
            var assessment = await GetAssessmentByIdAsync(assessmentId).ConfigureAwait(false);
            if (assessment == null)
            {
                return Result.Failure<bool>(Error.NotFound("Assessment", "Assessment not found"));
            }

            if (!assessment.IsAvailable())
            {
                return Result.Success(false);
            }

            if (assessment.MaxAttempts.HasValue)
            {
                var attemptCount = await GetAttemptCountAsync(assessmentId, enrollmentId).ConfigureAwait(false);
                return Result.Success(attemptCount < assessment.MaxAttempts.Value);
            }

            return Result.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking attempt eligibility for assessment {AssessmentId}", assessmentId);
            return Result.Failure<bool>(Error.Failure("CanAttempt", "Failed to check attempt eligibility"));
        }
    }
}
