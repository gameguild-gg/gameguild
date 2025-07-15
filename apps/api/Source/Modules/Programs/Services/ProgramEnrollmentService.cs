using GameGuild.Database;
using Microsoft.EntityFrameworkCore;


namespace GameGuild.Modules.Programs;

/// <summary>
/// Service implementation for program enrollment management
/// </summary>
public class ProgramEnrollmentService : IProgramEnrollmentService
{
    private readonly ApplicationDbContext _context;

    public ProgramEnrollmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Enroll a user in a program
    /// </summary>
    public async Task<ProgramEnrollment> EnrollUserAsync(Guid userId, Guid programId, EnrollmentSource source = EnrollmentSource.Manual)
    {
        // Check if user is already enrolled
        var existingEnrollment = await _context.ProgramEnrollments
            .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.ProgramId == programId);

        if (existingEnrollment != null)
        {
            // Reactivate if cancelled or expired
            if (existingEnrollment.EnrollmentStatus == EnrollmentStatus.Cancelled || 
                existingEnrollment.EnrollmentStatus == EnrollmentStatus.Expired)
            {
                existingEnrollment.EnrollmentStatus = EnrollmentStatus.Active;
                existingEnrollment.EnrolledAt = DateTime.UtcNow;
                existingEnrollment.EnrollmentSource = source;
                existingEnrollment.Touch();
                await _context.SaveChangesAsync();
            }
            return existingEnrollment;
        }

        // Verify program exists and is available for enrollment
        var program = await _context.Programs
            .FirstOrDefaultAsync(p => p.Id == programId);

        if (program == null)
            throw new ArgumentException("Program not found", nameof(programId));

        if (program.EnrollmentStatus != EnrollmentStatus.Open)
            throw new InvalidOperationException("Program is not available for enrollment");

        // Create new enrollment
        var enrollment = new ProgramEnrollment
        {
            UserId = userId,
            ProgramId = programId,
            EnrollmentSource = source,
            EnrolledAt = DateTime.UtcNow,
            StartDate = DateTime.UtcNow
        };

        _context.ProgramEnrollments.Add(enrollment);
        await _context.SaveChangesAsync();

        return enrollment;
    }

    /// <summary>
    /// Auto-enroll user in all programs included in a product
    /// </summary>
    public async Task<IEnumerable<ProgramEnrollment>> AutoEnrollInProductProgramsAsync(Guid userId, Guid productId)
    {
        // Get all programs in the product
        var productPrograms = await _context.ProductPrograms
            .Include(pp => pp.Program)
            .Where(pp => pp.ProductId == productId)
            .OrderBy(pp => pp.SortOrder)
            .ToListAsync();

        var enrollments = new List<ProgramEnrollment>();

        foreach (var productProgram in productPrograms)
        {
            try
            {
                var enrollment = await EnrollUserAsync(userId, productProgram.ProgramId, EnrollmentSource.ProductPurchase);
                enrollments.Add(enrollment);
            }
            catch (Exception ex)
            {
                // Log error but continue with other programs
                // You might want to add proper logging here
                Console.WriteLine($"Failed to enroll user {userId} in program {productProgram.ProgramId}: {ex.Message}");
            }
        }

        return enrollments;
    }

    /// <summary>
    /// Get user's enrollment in a program
    /// </summary>
    public async Task<ProgramEnrollment?> GetEnrollmentAsync(Guid userId, Guid programId)
    {
        return await _context.ProgramEnrollments
            .Include(pe => pe.Program)
            .Include(pe => pe.User)
            .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.ProgramId == programId);
    }

    /// <summary>
    /// Get all user's enrollments
    /// </summary>
    public async Task<IEnumerable<ProgramEnrollment>> GetUserEnrollmentsAsync(Guid userId, EnrollmentStatus? status = null)
    {
        var query = _context.ProgramEnrollments
            .Include(pe => pe.Program)
            .Where(pe => pe.UserId == userId);

        if (status.HasValue)
        {
            query = query.Where(pe => pe.EnrollmentStatus == status.Value);
        }

        return await query
            .OrderByDescending(pe => pe.EnrolledAt)
            .ToListAsync();
    }

    /// <summary>
    /// Update enrollment progress
    /// </summary>
    public async Task<ProgramEnrollment> UpdateProgressAsync(Guid enrollmentId, decimal progressPercentage)
    {
        var enrollment = await _context.ProgramEnrollments
            .FirstOrDefaultAsync(pe => pe.Id == enrollmentId);

        if (enrollment == null)
            throw new ArgumentException("Enrollment not found", nameof(enrollmentId));

        enrollment.ProgressPercentage = Math.Max(0, Math.Min(100, progressPercentage));
        
        // Update completion status based on progress
        if (enrollment.ProgressPercentage == 100 && enrollment.CompletionStatus != CompletionStatus.Completed)
        {
            enrollment.CompletionStatus = CompletionStatus.Completed;
            enrollment.CompletedAt = DateTime.UtcNow;
        }
        else if (enrollment.ProgressPercentage > 0 && enrollment.CompletionStatus == CompletionStatus.NotStarted)
        {
            enrollment.CompletionStatus = CompletionStatus.InProgress;
        }

        enrollment.Touch();
        await _context.SaveChangesAsync();

        return enrollment;
    }

    /// <summary>
    /// Mark enrollment as completed
    /// </summary>
    public async Task<ProgramEnrollment> CompleteEnrollmentAsync(Guid enrollmentId, decimal? finalGrade = null)
    {
        var enrollment = await _context.ProgramEnrollments
            .FirstOrDefaultAsync(pe => pe.Id == enrollmentId);

        if (enrollment == null)
            throw new ArgumentException("Enrollment not found", nameof(enrollmentId));

        enrollment.MarkAsCompleted(finalGrade);
        await _context.SaveChangesAsync();

        return enrollment;
    }

    /// <summary>
    /// Cancel enrollment
    /// </summary>
    public async Task<bool> CancelEnrollmentAsync(Guid enrollmentId)
    {
        var enrollment = await _context.ProgramEnrollments
            .FirstOrDefaultAsync(pe => pe.Id == enrollmentId);

        if (enrollment == null)
            return false;

        enrollment.EnrollmentStatus = EnrollmentStatus.Cancelled;
        enrollment.Touch();
        await _context.SaveChangesAsync();

        return true;
    }

    /// <summary>
    /// Check if user is enrolled in program
    /// </summary>
    public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid programId)
    {
        return await _context.ProgramEnrollments
            .AnyAsync(pe => pe.UserId == userId && 
                           pe.ProgramId == programId && 
                           pe.EnrollmentStatus == EnrollmentStatus.Active);
    }

    /// <summary>
    /// Get enrollment statistics for a program
    /// </summary>
    public async Task<ProgramEnrollmentStats> GetEnrollmentStatsAsync(Guid programId)
    {
        var enrollments = await _context.ProgramEnrollments
            .Where(pe => pe.ProgramId == programId)
            .ToListAsync();

        var totalEnrollments = enrollments.Count;
        var activeEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Active);
        var completedEnrollments = enrollments.Count(e => e.CompletionStatus == CompletionStatus.Completed);
        var cancelledEnrollments = enrollments.Count(e => e.EnrollmentStatus == EnrollmentStatus.Cancelled);

        return new ProgramEnrollmentStats
        {
            TotalEnrollments = totalEnrollments,
            ActiveEnrollments = activeEnrollments,
            CompletedEnrollments = completedEnrollments,
            CancelledEnrollments = cancelledEnrollments,
            AverageProgressPercentage = totalEnrollments > 0 ? enrollments.Average(e => e.ProgressPercentage) : 0,
            CompletionRate = totalEnrollments > 0 ? (decimal)completedEnrollments / totalEnrollments * 100 : 0,
            AverageFinalGrade = enrollments.Where(e => e.FinalGrade.HasValue).Any() ? 
                enrollments.Where(e => e.FinalGrade.HasValue).Average(e => e.FinalGrade!.Value) : null,
            CertificatesIssued = enrollments.Count(e => e.CertificateIssued)
        };
    }

    /// <summary>
    /// Issue certificate for completed enrollment
    /// </summary>
    public async Task<bool> IssueCertificateAsync(Guid enrollmentId)
    {
        var enrollment = await _context.ProgramEnrollments
            .Include(pe => pe.Program)
            .Include(pe => pe.User)
            .FirstOrDefaultAsync(pe => pe.Id == enrollmentId);

        if (enrollment == null || enrollment.CompletionStatus != CompletionStatus.Completed)
            return false;

        if (enrollment.CertificateIssued)
            return true; // Already issued

        // TODO: Integrate with certificate service to generate actual certificate
        // For now, just mark as issued
        enrollment.CertificateIssued = true;
        enrollment.CertificateIssuedAt = DateTime.UtcNow;
        enrollment.CompletionStatus = CompletionStatus.CompletedWithCertificate;
        enrollment.Touch();

        await _context.SaveChangesAsync();
        return true;
    }
}
