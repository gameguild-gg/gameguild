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

/// <summary>
/// Tests for CertificateDto record and mapping.
/// </summary>
public class CertificateDtoTests
{
    [Fact]
    public void FromEntity_ShouldMapAllProperties()
    {
        var templateId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var cert = Certificate.Issue(templateId, enrollmentId, userId, courseId,
            "Alice Smith", "Game Dev 101", DateTime.UtcNow.AddYears(2));

        var dto = CertificateDto.FromEntity(cert);

        dto.Id.Should().Be(cert.Id);
        dto.TemplateId.Should().Be(templateId);
        dto.EnrollmentId.Should().Be(enrollmentId);
        dto.UserId.Should().Be(userId);
        dto.CourseId.Should().Be(courseId);
        dto.CertificateNumber.Should().Be(cert.CertificateNumber);
        dto.RecipientName.Should().Be("Alice Smith");
        dto.CourseName.Should().Be("Game Dev 101");
        dto.IssuedAt.Should().Be(cert.IssuedAt);
        dto.ExpiresAt.Should().Be(cert.ExpiresAt);
        dto.Status.Should().Be(CertificateStatus.Active);
    }

    [Fact]
    public void FromEntity_RevokedCertificate_ShouldMapRevokedStatus()
    {
        var cert = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            "Bob Jones", "Security 201");
        cert.Revoke("Policy");

        var dto = CertificateDto.FromEntity(cert);

        dto.Status.Should().Be(CertificateStatus.Revoked);
    }

    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var id = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var dto = new CertificateDto(id, templateId, enrollmentId, userId, courseId, "CERT-123", "Name", "Course",
            DateTime.UtcNow, DateTime.UtcNow.AddDays(30), CertificateStatus.Active);

        dto.Id.Should().Be(id);
        dto.TemplateId.Should().Be(templateId);
        dto.EnrollmentId.Should().Be(enrollmentId);
        dto.UserId.Should().Be(userId);
        dto.CourseId.Should().Be(courseId);
        dto.CertificateNumber.Should().Be("CERT-123");
        dto.RecipientName.Should().Be("Name");
        dto.CourseName.Should().Be("Course");
    }
}

/// <summary>
/// Tests for CertificateVerificationResult record.
/// </summary>
public class CertificateVerificationResultTests
{
    [Fact]
    public void Constructor_ShouldSetAllProperties()
    {
        var result = new CertificateVerificationResult(
            true, "CERT-001", "Alice", "Course 1",
            DateTime.UtcNow, DateTime.UtcNow.AddYears(1),
            CertificateStatus.Active, "Valid certificate");

        result.IsValid.Should().BeTrue();
        result.CertificateNumber.Should().Be("CERT-001");
        result.RecipientName.Should().Be("Alice");
        result.CourseName.Should().Be("Course 1");
        result.Status.Should().Be(CertificateStatus.Active);
        result.Message.Should().Be("Valid certificate");
    }

    [Fact]
    public void Constructor_WithNullOptionalFields_ShouldWork()
    {
        var result = new CertificateVerificationResult(
            false, "CERT-002", null, null,
            DateTime.UtcNow, null,
            CertificateStatus.Revoked, null);

        result.IsValid.Should().BeFalse();
        result.RecipientName.Should().BeNull();
        result.ExpiresAt.Should().BeNull();
        result.Message.Should().BeNull();
    }
}

/// <summary>
/// Tests for request records.
/// </summary>
public class RequestRecordTests
{
    [Fact]
    public void IssueCertificateRequest_ShouldSetAllProperties()
    {
        var templateId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();

        var request = new IssueCertificateRequest(templateId, enrollmentId, userId, courseId);

        request.TemplateId.Should().Be(templateId);
        request.EnrollmentId.Should().Be(enrollmentId);
        request.UserId.Should().Be(userId);
        request.CourseId.Should().Be(courseId);
    }

    [Fact]
    public void RevokeCertificateRequest_ShouldSetReason()
    {
        var request = new RevokeCertificateRequest("Fraudulent activity");
        request.Reason.Should().Be("Fraudulent activity");
    }
}
