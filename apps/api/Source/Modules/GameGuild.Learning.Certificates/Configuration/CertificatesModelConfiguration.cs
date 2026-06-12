using Microsoft.EntityFrameworkCore;

namespace GameGuild.Learning.Certificates;

/// <summary>
///     EF Core model configuration for certificate templates and issued certificates.
/// </summary>
public sealed class CertificatesModelConfiguration : IModelConfiguration
{
    public void Configure(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CertificateTemplate>(entity =>
        {
            entity.ToTable("learning_certificate_templates");
            entity.HasKey(template => template.Id);
            entity.Property(template => template.Name).HasMaxLength(250).IsRequired();
            entity.Property(template => template.Description).HasMaxLength(2000);
            entity.Property(template => template.TemplateHtml).IsRequired();
            entity.HasIndex(template => template.CourseId);
            entity.HasIndex(template => new { template.CourseId, template.IsDefault });
            entity.HasIndex(template => new { template.TenantId, template.IsActive });
        });

        modelBuilder.Entity<Certificate>(entity =>
        {
            entity.ToTable("learning_certificates");
            entity.HasKey(certificate => certificate.Id);
            entity.Property(certificate => certificate.CertificateNumber).HasMaxLength(80).IsRequired();
            entity.Property(certificate => certificate.RecipientName).HasMaxLength(250).IsRequired();
            entity.Property(certificate => certificate.CourseName).HasMaxLength(500).IsRequired();
            entity.Property(certificate => certificate.RevocationReason).HasMaxLength(1000);
            entity.Property(certificate => certificate.VerificationUrl).HasMaxLength(1000);
            entity.Property(certificate => certificate.Status).HasConversion<string>().HasMaxLength(40);
            entity.HasIndex(certificate => certificate.CertificateNumber).IsUnique();
            entity.HasIndex(certificate => certificate.TemplateId);
            entity.HasIndex(certificate => certificate.EnrollmentId);
            entity.HasIndex(certificate => certificate.UserId);
            entity.HasIndex(certificate => certificate.CourseId);
            entity.HasIndex(certificate => certificate.Status);
        });
    }
}
