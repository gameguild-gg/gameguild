using GameGuild.Models;

namespace GameGuild.Learning.Certificates;

/// <summary>
/// Service interface for certificate template management
/// </summary>
public interface ICertificateTemplateService
{
    /// <summary>
    /// Creates a new certificate template
    /// </summary>
    Task<Result<CertificateTemplate>> CreateTemplateAsync(CertificateTemplate template, Guid? tenantId = null);

    /// <summary>
    /// Gets a certificate template by ID
    /// </summary>
    Task<CertificateTemplate?> GetTemplateByIdAsync(Guid id);

    /// <summary>
    /// Gets certificate templates for a course
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetTemplatesByCourseAsync(Guid courseId);

    /// <summary>
    /// Updates an existing certificate template
    /// </summary>
    Task<Result<CertificateTemplate>> UpdateTemplateAsync(CertificateTemplate template);

    /// <summary>
    /// Deletes a certificate template
    /// </summary>
    Task<Result> DeleteTemplateAsync(Guid id);

    /// <summary>
    /// Gets active certificate templates
    /// </summary>
    Task<IEnumerable<CertificateTemplate>> GetActiveTemplatesAsync(Guid? tenantId = null);

    /// <summary>
    /// Sets a template as the default for a course
    /// </summary>
    Task<Result<CertificateTemplate>> SetDefaultTemplateAsync(Guid courseId, Guid templateId);
}
