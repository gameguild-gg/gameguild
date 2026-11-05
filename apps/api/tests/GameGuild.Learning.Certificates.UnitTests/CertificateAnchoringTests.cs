using FluentAssertions;
using GameGuild.Identity.Users;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

using LearningProgram = GameGuild.Learning.Courses.Program;
using LearningProgramEnrollment = GameGuild.Learning.Courses.ProgramEnrollment;
using CompletionStatus = GameGuild.Learning.Courses.CompletionStatus;
using EnrollmentStatus = GameGuild.Learning.Courses.EnrollmentStatus;

namespace GameGuild.Learning.Certificates.Tests;

public class CertificateAnchoringTests
{
    [Fact]
    public async Task NoOpCertificateAnchoring_CompletesWithoutSideEffects()
    {
        var certificate = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Recipient", "Course");

        var anchor = () => NoOpCertificateAnchoring.Instance.AnchorIssuedCertificateAsync(certificate);
        var revoke = () => NoOpCertificateAnchoring.Instance.RecordRevocationAsync(certificate, "reason");

        await anchor.Should().NotThrowAsync();
        await revoke.Should().NotThrowAsync();
    }

    [Fact]
    public void AddCertificatesModule_RegistersNoOpAnchoringByDefault()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddCertificatesModule();
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificateAnchoring>().Should().BeOfType<NoOpCertificateAnchoring>();
    }

    [Fact]
    public void AddCertificatesModule_HostAdapterRegistration_TakesPrecedenceOverNoOp()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        services.AddCertificatesModule();
        // The host registers its adapter AFTER the module (mirrors ApiProductComposition ordering).
        services.AddScoped<ICertificateAnchoring>(_ => Mock.Of<ICertificateAnchoring>());
        var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ICertificateAnchoring>().Should().NotBeOfType<NoOpCertificateAnchoring>();
    }

    [Fact]
    public async Task IssueCertificateAsync_AnchorsIssuedCertificate()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var anchoring = new RecordingCertificateAnchoring();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, new NoopLearningAuditEventSink(), anchoring);
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);

        var result = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        result.IsSuccess.Should().BeTrue();
        anchoring.Anchored.Should().ContainSingle(certificate => certificate.Id == result.Value.Id);
        anchoring.Anchored[0].CertificateNumber.Should().Be(result.Value.CertificateNumber);
    }

    [Fact]
    public async Task IssueCertificateAsync_AnchoringFailure_DoesNotBreakIssuance()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var service = new CertificateService(
            context,
            NullLogger<CertificateService>.Instance,
            new NoopLearningAuditEventSink(),
            new ThrowingCertificateAnchoring());
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);

        var result = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        result.IsSuccess.Should().BeTrue();
        (await context.Set<Certificate>().CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task RevokeCertificateAsync_RecordsRevocationAnchor()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var anchoring = new RecordingCertificateAnchoring();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, new NoopLearningAuditEventSink(), anchoring);
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        var result = await service.RevokeCertificateAsync(issued.Value.Id, "integrity issue");

        result.IsSuccess.Should().BeTrue();
        anchoring.Revoked.Should().ContainSingle(revocation =>
            revocation.Certificate.Id == issued.Value.Id && revocation.Reason == "integrity issue");
    }

    [Fact]
    public async Task RevokeCertificateAsync_AnchoringFailure_DoesNotBreakRevocation()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var service = new CertificateService(
            context,
            NullLogger<CertificateService>.Instance,
            new NoopLearningAuditEventSink(),
            new ThrowingCertificateAnchoring());
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        var result = await service.RevokeCertificateAsync(issued.Value.Id, "integrity issue");

        result.IsSuccess.Should().BeTrue();
        (await context.Set<Certificate>().SingleAsync()).Status.Should().Be(CertificateStatus.Revoked);
    }

    [Fact]
    public async Task NoOpCertificateAnchoring_VerifyAnchoring_ReportsNotAnchoredWithoutConfirming()
    {
        var certificate = Certificate.Issue(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Recipient", "Course");

        var verification = await NoOpCertificateAnchoring.Instance.VerifyAnchoringAsync(certificate);

        verification.Status.Should().Be(CertificateAnchorVerificationStatus.NotAnchored);
        verification.IsVerified.Should().BeFalse();
        verification.CertificateId.Should().Be(certificate.Id.ToString());
    }

    [Fact]
    public async Task VerifyCertificateAnchoringAsync_QueriesTheAnchoringPort_AndReturnsItsFailClosedResult()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var anchoring = new RecordingCertificateAnchoring
        {
            VerificationResult = CertificateAnchorVerification.BackendUnreachable(
                "00000000-0000-0000-0000-000000000000", null, "chain rpc unreachable"),
        };
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, new NoopLearningAuditEventSink(), anchoring);
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        var result = await service.VerifyCertificateAnchoringAsync(issued.Value.CertificateNumber);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(CertificateAnchorVerificationStatus.Unreachable);
        result.Value.IsVerified.Should().BeFalse();
        anchoring.Verified.Should().ContainSingle(certificate => certificate.Id == issued.Value.Id);
    }

    [Fact]
    public async Task VerifyCertificateAnchoringAsync_UnknownCertificateNumber_FailsWithNotFound()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, new NoopLearningAuditEventSink());

        var result = await service.VerifyCertificateAnchoringAsync("CERT-20990101-DOESNOT");

        result.IsSuccess.Should().BeFalse();
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task VerifyCertificateAnchoringAsync_ThrowingBackend_FailsClosedInsteadOfThrowing()
    {
        await using var context = CertificateAnchoringDbContext.Create();
        var service = new CertificateService(
            context,
            NullLogger<CertificateService>.Instance,
            new NoopLearningAuditEventSink(),
            new ThrowingCertificateAnchoring());
        var (templateId, enrollmentId, userId, courseId) = SeedTemplateAndIssue(service, context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        var result = await service.VerifyCertificateAnchoringAsync(issued.Value.CertificateNumber);

        // A throwing anchoring backend surfaces as a failed query, never as a verified anchor.
        result.IsSuccess.Should().BeFalse();
    }

    private static (Guid TemplateId, Guid EnrollmentId, Guid UserId, Guid CourseId) SeedTemplateAndIssue(
        CertificateService service,
        CertificateAnchoringDbContext context)
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var template = CertificateTemplate.Create(courseId, "Completion", "<html />");
        // The #1226 issuance eligibility gate requires a real, completed enrollment that
        // belongs to the recipient and the course — seed one instead of random ids
        // (same fix as the EVM fixtures in PR #1302).
        var enrollment = new LearningProgramEnrollment
        {
            Id = Guid.NewGuid(),
            ProgramId = courseId,
            UserId = userId,
            EnrollmentStatus = EnrollmentStatus.Active,
            CompletionStatus = CompletionStatus.Completed,
        };
        context.Set<CertificateTemplate>().Add(template);
        context.Set<LearningProgramEnrollment>().Add(enrollment);
        context.SaveChanges();

        return (template.Id, enrollment.Id, userId, courseId);
    }

    private sealed class RecordingCertificateAnchoring : ICertificateAnchoring
    {
        public List<Certificate> Anchored { get; } = [];

        public List<(Certificate Certificate, string Reason)> Revoked { get; } = [];

        public List<Certificate> Verified { get; } = [];

        public CertificateAnchorVerification VerificationResult { get; set; } =
            CertificateAnchorVerification.NotAnchored("00000000-0000-0000-0000-000000000000");

        public Task AnchorIssuedCertificateAsync(Certificate certificate, CancellationToken cancellationToken = default)
        {
            Anchored.Add(certificate);
            return Task.CompletedTask;
        }

        public Task RecordRevocationAsync(Certificate certificate, string reason, CancellationToken cancellationToken = default)
        {
            Revoked.Add((certificate, reason));
            return Task.CompletedTask;
        }

        public Task<CertificateAnchorVerification> VerifyAnchoringAsync(Certificate certificate, CancellationToken cancellationToken = default)
        {
            Verified.Add(certificate);
            return Task.FromResult(VerificationResult);
        }
    }

    private sealed class ThrowingCertificateAnchoring : ICertificateAnchoring
    {
        public Task AnchorIssuedCertificateAsync(Certificate certificate, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("anchor backend unavailable");

        public Task RecordRevocationAsync(Certificate certificate, string reason, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("anchor backend unavailable");

        public Task<CertificateAnchorVerification> VerifyAnchoringAsync(Certificate certificate, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("anchor backend unavailable");
    }

    private sealed class CertificateAnchoringDbContext(DbContextOptions<CertificateAnchoringDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public static CertificateAnchoringDbContext Create()
        {
            var inMemoryOptions = new DbContextOptionsBuilder<CertificateAnchoringDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CertificateAnchoringDbContext(inMemoryOptions);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CertificatesModelConfiguration().Configure(modelBuilder);
            modelBuilder.Entity<User>(user =>
            {
                user.HasKey(entity => entity.Id);
                user.Ignore(entity => entity.Profile);
                user.Ignore(entity => entity.Metadata);
                user.Ignore(entity => entity.Preferences);
                user.Ignore(entity => entity.Notifications);
                user.Ignore(entity => entity.TenantMemberships);
            });
            modelBuilder.Entity<LearningProgram>(program =>
            {
                program.HasKey(entity => entity.Id);
                program.Ignore(entity => entity.ProgramContents);
                program.Ignore(entity => entity.ProgramUsers);
                program.Ignore(entity => entity.ProgramRatings);
                program.Ignore(entity => entity.ProgramWishlists);
            });
            // The eligibility gate queries Set<ProgramEnrollment>() on every issuance,
            // so the enrollment must be part of this InMemory model. PercentValue is a
            // readonly struct with a private constructor: conventions cannot map it, so
            // mirror the converters CoursesModelConfiguration applies in the real model.
            modelBuilder.Entity<LearningProgramEnrollment>(enrollment =>
            {
                enrollment.HasKey(entity => entity.Id);
                enrollment.Property(entity => entity.ProgressPercentage).HasConversion(PercentConverter);
                enrollment.Property(entity => entity.FinalGrade).HasConversion(NullablePercentConverter);
            });
        }

        private static readonly ValueConverter<PercentValue, int> PercentConverter =
            new(value => value.Units, value => PercentValue.FromUnits(value));

        private static readonly ValueConverter<PercentValue?, int?> NullablePercentConverter =
            new(value => value.HasValue ? value.Value.Units : null,
                value => value == null ? null : PercentValue.FromUnits(value.Value));
    }
}
