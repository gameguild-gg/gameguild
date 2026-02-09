using FluentAssertions;
using Xunit;

namespace GameGuild.Learning.Certificates.Tests;

/// <summary>
/// Unit tests for Certificate entity domain logic.
/// </summary>
public class CertificateEntityTests
{
    [Fact]
    public void Issue_ShouldSetDefaultValues()
    {
        var templateId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var cert = Certificate.Issue(templateId, enrollmentId, userId, courseId, "John Doe", "Intro to C#");

        cert.Id.Should().NotBeEmpty();
        cert.TemplateId.Should().Be(templateId);
        cert.EnrollmentId.Should().Be(enrollmentId);
        cert.UserId.Should().Be(userId);
        cert.CourseId.Should().Be(courseId);
        cert.RecipientName.Should().Be("John Doe");
        cert.CourseName.Should().Be("Intro to C#");
        cert.Status.Should().Be(CertificateStatus.Active);
        cert.CertificateNumber.Should().StartWith("CERT-");
        cert.IssuedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        cert.ExpiresAt.Should().BeNull();
        cert.RevokedAt.Should().BeNull();
    }

    [Fact]
    public void Issue_WithExpiration_ShouldSetExpiresAt()
    {
        var expiry = DateTime.UtcNow.AddYears(1);
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Jane Doe", "Advanced C#", expiry);

        cert.ExpiresAt.Should().Be(expiry);
    }

    [Fact]
    public void IsValid_WhenActiveAndNotExpired_ShouldReturnTrue()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course", DateTime.UtcNow.AddYears(1));

        cert.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenActiveAndNoExpiry_ShouldReturnTrue()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course");

        cert.IsValid().Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenRevoked_ShouldReturnFalse()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course");

        cert.Revoke("Fraudulent");
        cert.IsValid().Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenExpired_ShouldReturnFalse()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course", DateTime.UtcNow.AddDays(-1));

        cert.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Revoke_ShouldSetStatusAndReason()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course");

        cert.Revoke("Policy violation");

        cert.Status.Should().Be(CertificateStatus.Revoked);
        cert.RevokedAt.Should().NotBeNull();
        cert.RevocationReason.Should().Be("Policy violation");
    }

    [Fact]
    public void CertificateNumber_ShouldBeUnique()
    {
        var cert1 = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "User1", "Course1");
        var cert2 = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "User2", "Course2");

        cert1.CertificateNumber.Should().NotBe(cert2.CertificateNumber);
    }

    [Fact]
    public void CertificateNumber_ShouldMatchFormat()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Test User", "Course");

        // Format: CERT-yyyyMMdd-XXXXXXXX
        cert.CertificateNumber.Should().MatchRegex(@"^CERT-\d{8}-[A-Z0-9]{8}$");
    }
}

/// <summary>
/// Unit tests for CertificateTemplate entity domain logic.
/// </summary>
public class CertificateTemplateEntityTests
{
    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var courseId = Guid.NewGuid();
        var template = CertificateTemplate.Create(courseId, "Default Template", "<h1>Certificate</h1>");

        template.Id.Should().NotBeEmpty();
        template.CourseId.Should().Be(courseId);
        template.Name.Should().Be("Default Template");
        template.TemplateHtml.Should().Be("<h1>Certificate</h1>");
        template.IsDefault.Should().BeFalse();
        template.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithTenantId_ShouldSetTenantId()
    {
        var tenantId = Guid.NewGuid();
        var template = CertificateTemplate.Create(Guid.NewGuid(), "Template", "<h1>Cert</h1>", tenantId);
        template.TenantId.Should().Be(tenantId);
    }
}
