
namespace GameGuild.Learning.Abstractions;

/// <summary>
/// Abstraction for certificate issuance operations.
/// This interface follows the Dependency Inversion Principle (DIP):
/// - The Learning.Courses module depends on this abstraction
/// - The Learning.Certificates module implements this abstraction
/// - This breaks the circular dependency between modules
/// </summary>
public interface ICertificateIssuanceService
{
    /// <summary>
    /// Issues a certificate for a completed enrollment
    /// </summary>
    /// <param name="enrollmentId">The enrollment ID</param>
    /// <param name="userId">The user receiving the certificate</param>
    /// <param name="programId">The program/course ID</param>
    /// <param name="tenantId">Optional tenant ID for multi-tenant scenarios</param>
    /// <returns>Result containing the certificate ID if successful, or error details</returns>
    Task<Result<Guid>> IssueCertificateForEnrollmentAsync(
        Guid enrollmentId,
        Guid userId,
        Guid programId,
        Guid? tenantId = null);

    /// <summary>
    /// Checks if an enrollment has a certificate issued
    /// </summary>
    Task<bool> HasCertificateAsync(Guid enrollmentId);
}
