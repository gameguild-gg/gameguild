using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GameGuild.Learning.Certificates.Tests;

public sealed class DtoAndModelConfigurationTests
{
    [Fact]
    public void CertificateTemplateDtos_MapAllTemplateFields()
    {
        var courseId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var template = CertificateTemplate.Create(courseId, "Completion", "<html />", tenantId);
        template.SetProperties(new Dictionary<string, object?>
        {
            [nameof(CertificateTemplate.Description)] = "Default completion template",
            [nameof(CertificateTemplate.TemplateStyles)] = "body { color: #111; }",
            [nameof(CertificateTemplate.IsDefault)] = true,
            [nameof(CertificateTemplate.IsActive)] = false
        });

        var listDto = CertificateTemplateDto.FromEntity(template);
        var detailDto = CertificateTemplateDetailDto.FromEntity(template);
        var createRequest = new CreateCertificateTemplateRequest(courseId, "Completion", "<html />");

        listDto.Id.Should().Be(template.Id);
        listDto.CourseId.Should().Be(courseId);
        listDto.TenantId.Should().Be(tenantId);
        listDto.Name.Should().Be("Completion");
        listDto.Description.Should().Be("Default completion template");
        listDto.IsDefault.Should().BeTrue();
        listDto.IsActive.Should().BeFalse();
        listDto.CreatedAt.Should().Be(template.CreatedAt);
        listDto.UpdatedAt.Should().Be(template.UpdatedAt);

        detailDto.Id.Should().Be(template.Id);
        detailDto.CourseId.Should().Be(courseId);
        detailDto.TenantId.Should().Be(tenantId);
        detailDto.Name.Should().Be("Completion");
        detailDto.Description.Should().Be("Default completion template");
        detailDto.TemplateHtml.Should().Be("<html />");
        detailDto.TemplateStyles.Should().Be("body { color: #111; }");
        detailDto.IsDefault.Should().BeTrue();
        detailDto.IsActive.Should().BeFalse();
        detailDto.CreatedAt.Should().Be(template.CreatedAt);
        detailDto.UpdatedAt.Should().Be(template.UpdatedAt);

        createRequest.CourseId.Should().Be(courseId);
        createRequest.Name.Should().Be("Completion");
        createRequest.TemplateHtml.Should().Be("<html />");
    }

    [Fact]
    public void CertificatesModelConfiguration_AppliesTemplateAndCertificateMappings()
    {
        using var context = CreateContext();
        var templateEntity = context.Model.FindEntityType(typeof(CertificateTemplate));
        var certificateEntity = context.Model.FindEntityType(typeof(Certificate));

        templateEntity.Should().NotBeNull();
        certificateEntity.Should().NotBeNull();
        var template = templateEntity!;
        var certificate = certificateEntity!;

        template.GetTableName().Should().Be("learning_certificate_templates");
        template.FindPrimaryKey()!.Properties.Single().Name.Should().Be(nameof(CertificateTemplate.Id));
        template.FindProperty(nameof(CertificateTemplate.Name))!.GetMaxLength().Should().Be(250);
        template.FindProperty(nameof(CertificateTemplate.Name))!.IsNullable.Should().BeFalse();
        template.FindProperty(nameof(CertificateTemplate.Description))!.GetMaxLength().Should().Be(2000);
        template.FindProperty(nameof(CertificateTemplate.TemplateHtml))!.IsNullable.Should().BeFalse();
        template.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(CertificateTemplate.CourseId));
        var courseDefaultIndex = new[] { nameof(CertificateTemplate.CourseId), nameof(CertificateTemplate.IsDefault) };
        var tenantActiveIndex = new[] { nameof(CertificateTemplate.TenantId), nameof(CertificateTemplate.IsActive) };
        template.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name).SequenceEqual(courseDefaultIndex));
        template.GetIndexes().Should().Contain(index => index.Properties.Select(property => property.Name).SequenceEqual(tenantActiveIndex));

        certificate.GetTableName().Should().Be("learning_certificates");
        certificate.FindPrimaryKey()!.Properties.Single().Name.Should().Be(nameof(Certificate.Id));
        certificate.FindProperty(nameof(Certificate.CertificateNumber))!.GetMaxLength().Should().Be(80);
        certificate.FindProperty(nameof(Certificate.CertificateNumber))!.IsNullable.Should().BeFalse();
        certificate.FindProperty(nameof(Certificate.RecipientName))!.GetMaxLength().Should().Be(250);
        certificate.FindProperty(nameof(Certificate.RecipientName))!.IsNullable.Should().BeFalse();
        certificate.FindProperty(nameof(Certificate.CourseName))!.GetMaxLength().Should().Be(500);
        certificate.FindProperty(nameof(Certificate.CourseName))!.IsNullable.Should().BeFalse();
        certificate.FindProperty(nameof(Certificate.RevocationReason))!.GetMaxLength().Should().Be(1000);
        certificate.FindProperty(nameof(Certificate.VerificationUrl))!.GetMaxLength().Should().Be(1000);
        certificate.FindProperty(nameof(Certificate.Status))!.GetMaxLength().Should().Be(40);
        certificate.GetIndexes().Should().Contain(index => index.IsUnique && index.Properties.Single().Name == nameof(Certificate.CertificateNumber));
        certificate.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Certificate.TemplateId));
        certificate.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Certificate.EnrollmentId));
        certificate.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Certificate.UserId));
        certificate.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Certificate.CourseId));
        certificate.GetIndexes().Should().Contain(index => index.Properties.Single().Name == nameof(Certificate.Status));
    }

    private static CertificateConfigurationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<CertificateConfigurationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new CertificateConfigurationDbContext(options);
    }

    private sealed class CertificateConfigurationDbContext(DbContextOptions<CertificateConfigurationDbContext> options)
        : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CertificatesModelConfiguration().Configure(modelBuilder);
        }
    }
}
