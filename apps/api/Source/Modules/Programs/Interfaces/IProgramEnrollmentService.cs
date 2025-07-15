using GameGuild.Modules.Programs.Models;

namespace GameGuild.Modules.Programs.Interfaces;

/// <summary>
/// Interface for program enrollment services
/// </summary>
public interface IProgramEnrollmentService
{
    /// <summary>
    /// Enroll a user in a program
    /// </summary>
    Task<ProgramEnrollment> EnrollUserAsync(Guid userId, Guid programId, EnrollmentSource source = EnrollmentSource.Manual);

    /// <summary>
    /// Auto-enroll user in all programs included in a product
    /// </summary>
    Task<IEnumerable<ProgramEnrollment>> AutoEnrollInProductProgramsAsync(Guid userId, Guid productId);

    /// <summary>
    /// Get user's enrollment in a program
    /// </summary>
    Task<ProgramEnrollment?> GetEnrollmentAsync(Guid userId, Guid programId);

    /// <summary>
    /// Get all user's enrollments
    /// </summary>
    Task<IEnumerable<ProgramEnrollment>> GetUserEnrollmentsAsync(Guid userId, EnrollmentStatus? status = null);

    /// <summary>
    /// Update enrollment progress
    /// </summary>
    Task<ProgramEnrollment> UpdateProgressAsync(Guid enrollmentId, decimal progressPercentage);

    /// <summary>
    /// Mark enrollment as completed
    /// </summary>
    Task<ProgramEnrollment> CompleteEnrollmentAsync(Guid enrollmentId, decimal? finalGrade = null);

    /// <summary>
    /// Cancel enrollment
    /// </summary>
    Task<bool> CancelEnrollmentAsync(Guid enrollmentId);

    /// <summary>
    /// Check if user is enrolled in program
    /// </summary>
    Task<bool> IsUserEnrolledAsync(Guid userId, Guid programId);

    /// <summary>
    /// Get enrollment statistics for a program
    /// </summary>
    Task<ProgramEnrollmentStats> GetEnrollmentStatsAsync(Guid programId);

    /// <summary>
    /// Issue certificate for completed enrollment
    /// </summary>
    Task<bool> IssueCertificateAsync(Guid enrollmentId);
}

/// <summary>
/// Enrollment statistics for a program
/// </summary>
public class ProgramEnrollmentStats
{
    public int TotalEnrollments { get; set; }
    public int ActiveEnrollments { get; set; }
    public int CompletedEnrollments { get; set; }
    public int CancelledEnrollments { get; set; }
    public decimal AverageProgressPercentage { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal? AverageFinalGrade { get; set; }
    public int CertificatesIssued { get; set; }
}
