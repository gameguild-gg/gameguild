using FluentAssertions;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using LearningProgram = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Certificates.Tests;

/// <summary>
/// Universal audit coverage for certificate mutations (issues #180/#181/#184): issuing and
/// revoking certificates (and template mutations) emit learning audit events, and an audit
/// emission failure rolls the mutation back (fail closed — the operation reports failure and
/// nothing is persisted).
/// </summary>
public class CertificateAuditEmissionTests
{
    [Fact]
    public async Task IssuingAndRevokingCertificatesEmitsAuditEvents()
    {
        await using var context = CreateSqliteContext();
        var sink = new RecordingLearningAuditEventSink();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, sink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);

        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        var revoked = await service.RevokeCertificateAsync(issued.Value.Id, "compliance hold");

        issued.IsSuccess.Should().BeTrue();
        revoked.IsSuccess.Should().BeTrue();
        sink.Events.Select(auditEvent => auditEvent.ActionType).Should().Equal(
            "Learning.Certificates.CertificateIssued",
            "Learning.Certificates.CertificateRevoked");
        sink.Events.Should().OnlyContain(auditEvent => auditEvent.ResourceType == "Certificate");
        sink.Events[0].ActorUserId.Should().Be(userId);
    }

    [Fact]
    public async Task TemplateMutationsEmitAuditEvents()
    {
        await using var context = CreateSqliteContext();
        var sink = new RecordingLearningAuditEventSink();
        var service = new CertificateTemplateService(context, NullLogger<CertificateTemplateService>.Instance, sink);
        var courseId = Guid.NewGuid();

        var template = CertificateTemplate.Create(courseId, "Completion", "<html />");
        (await service.CreateTemplateAsync(template)).IsSuccess.Should().BeTrue();
        template.SetDefault(true);
        (await service.UpdateTemplateAsync(template)).IsSuccess.Should().BeTrue();
        (await service.SetDefaultTemplateAsync(courseId, template.Id)).IsSuccess.Should().BeTrue();
        (await service.DeleteTemplateAsync(template.Id)).IsSuccess.Should().BeTrue();

        sink.Events.Select(auditEvent => auditEvent.ActionType).Should().Equal(
            "Learning.Certificates.TemplateCreated",
            "Learning.Certificates.TemplateUpdated",
            "Learning.Certificates.TemplateDefaultChanged",
            "Learning.Certificates.TemplateDeleted");
    }

    [Fact]
    public async Task AuditEmissionFailureBlocksCertificateIssuance()
    {
        await using var context = CreateSqliteContext();
        var sink = new ThrowingLearningAuditEventSink();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, sink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);

        var result = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);

        result.IsSuccess.Should().BeFalse();
        (await context.Set<Certificate>().AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task AuditEmissionFailureBlocksCertificateRevocation()
    {
        await using var context = CreateSqliteContext();
        var recordingSink = new RecordingLearningAuditEventSink();
        var issuing = new CertificateService(context, NullLogger<CertificateService>.Instance, recordingSink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await issuing.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        issued.IsSuccess.Should().BeTrue();

        var revoking = new CertificateService(context, NullLogger<CertificateService>.Instance, new ThrowingLearningAuditEventSink());
        var result = await revoking.RevokeCertificateAsync(issued.Value.Id, "compliance hold");

        result.IsSuccess.Should().BeFalse();
        var stored = await context.Set<Certificate>().AsNoTracking().SingleAsync(certificate => certificate.Id == issued.Value.Id);
        stored.Status.Should().NotBe(CertificateStatus.Revoked);
    }

    [Fact]
    public async Task RenewingACertificateEmitsItsAuditEvent()
    {
        // Issue #180 certificate lifecycle: renewal extends validity and journals
        // Learning.Certificates.CertificateRenewed.
        await using var context = CreateSqliteContext();
        var sink = new RecordingLearningAuditEventSink();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, sink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        issued.IsSuccess.Should().BeTrue();

        var newExpiry = DateTime.UtcNow.AddYears(2);
        var renewed = await service.RenewCertificateAsync(issued.Value.Id, newExpiry);

        renewed.IsSuccess.Should().BeTrue();
        renewed.Value.Status.Should().Be(CertificateStatus.Active);
        renewed.Value.ExpiresAt.Should().Be(newExpiry);
        sink.Events.Select(auditEvent => auditEvent.ActionType).Should().Equal(
            "Learning.Certificates.CertificateIssued",
            "Learning.Certificates.CertificateRenewed");
    }

    [Fact]
    public async Task RenewingARevokedCertificateFails()
    {
        await using var context = CreateSqliteContext();
        var sink = new RecordingLearningAuditEventSink();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, sink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        (await service.RevokeCertificateAsync(issued.Value.Id, "compliance hold")).IsSuccess.Should().BeTrue();

        var renewed = await service.RenewCertificateAsync(issued.Value.Id, DateTime.UtcNow.AddYears(1));

        renewed.IsSuccess.Should().BeFalse();
        sink.Events.Select(auditEvent => auditEvent.ActionType).Should().NotContain("Learning.Certificates.CertificateRenewed");
    }

    [Fact]
    public async Task AuditEmissionFailureBlocksCertificateRenewal()
    {
        await using var context = CreateSqliteContext();
        var issuing = new CertificateService(context, NullLogger<CertificateService>.Instance, new RecordingLearningAuditEventSink());
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await issuing.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        issued.IsSuccess.Should().BeTrue();

        var renewing = new CertificateService(context, NullLogger<CertificateService>.Instance, new ThrowingLearningAuditEventSink());
        var result = await renewing.RenewCertificateAsync(issued.Value.Id, DateTime.UtcNow.AddYears(1));

        result.IsSuccess.Should().BeFalse();
        var stored = await context.Set<Certificate>().AsNoTracking().SingleAsync(certificate => certificate.Id == issued.Value.Id);
        stored.ExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task VerifyingACertificateEmitsItsAuditEvent()
    {
        // Issue #180 certificate lifecycle: validation attempts (including unknown numbers) are
        // audited with their outcome.
        await using var context = CreateSqliteContext();
        var sink = new RecordingLearningAuditEventSink();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, sink);
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await service.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        issued.IsSuccess.Should().BeTrue();

        var verified = await service.VerifyCertificateAsync(issued.Value.CertificateNumber);
        var unknown = await service.VerifyCertificateAsync("CERT-00000000-UNKNOWN");

        verified.Value.IsValid.Should().BeTrue();
        unknown.Value.IsValid.Should().BeFalse();
        var verificationEvents = sink.Events
            .Where(auditEvent => auditEvent.ActionType == "Learning.Certificates.CertificateVerified")
            .ToList();
        verificationEvents.Should().HaveCount(2);
        verificationEvents[0].Success.Should().BeTrue();
        verificationEvents[1].Success.Should().BeFalse();
        verificationEvents.Should().OnlyContain(auditEvent => auditEvent.ResourceType == "Certificate");
    }

    [Fact]
    public async Task VerificationStillAnswersWhenItsAuditCannotBePersisted()
    {
        // Verification is a read: its audit emission is best effort and never breaks the public
        // verification endpoint.
        await using var context = CreateSqliteContext();
        var issuing = new CertificateService(context, NullLogger<CertificateService>.Instance, new RecordingLearningAuditEventSink());
        var (templateId, enrollmentId, userId, courseId) = await SeedAsync(context);
        var issued = await issuing.IssueCertificateAsync(templateId, enrollmentId, userId, courseId);
        issued.IsSuccess.Should().BeTrue();

        var verifying = new CertificateService(context, NullLogger<CertificateService>.Instance, new ThrowingLearningAuditEventSink());
        var verified = await verifying.VerifyCertificateAsync(issued.Value.CertificateNumber);

        verified.IsSuccess.Should().BeTrue();
        verified.Value.IsValid.Should().BeTrue();
    }

    private static async Task<(Guid TemplateId, Guid EnrollmentId, Guid UserId, Guid CourseId)> SeedAsync(CertificateAuditSqliteContext context)
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var template = CertificateTemplate.Create(courseId, "Default completion", "<html />");
        var user = new User { Id = userId, Email = "recipient@example.com", Username = "recipient", Name = "Recipient" };
        var program = new LearningProgram { Id = courseId, Title = "Audited Course" };
        context.Set<CertificateTemplate>().Add(template);
        context.Set<User>().Add(user);
        context.Set<LearningProgram>().Add(program);
        await context.SaveChangesAsync();
        return (template.Id, Guid.NewGuid(), userId, courseId);
    }

    private static CertificateAuditSqliteContext CreateSqliteContext()
    {
        var connection = new Microsoft.Data.Sqlite.SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<CertificateAuditSqliteContext>()
            .UseSqlite(connection)
            .Options;
        var context = new CertificateAuditSqliteContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed class CertificateAuditSqliteContext(DbContextOptions<CertificateAuditSqliteContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CertificatesModelConfiguration().Configure(modelBuilder);
            // The Program entity carries PercentValue/ScoreValue properties that need
            // the production model configuration's value converters on SQLite.
            new GameGuild.Learning.Courses.CoursesModelConfiguration().Configure(modelBuilder);
            // The CoursesModelConfiguration pulls in Product via Program relationships;
            // the Commerce Product entity has navigations (BundleItems) that need the
            // Commerce module's configuration or must be ignored for the isolated test.
            modelBuilder.Entity<GameGuild.Commerce.Products.Product>(product =>
            {
                product.HasKey(entity => entity.Id);
                product.Ignore(entity => entity.BundleItems);
                product.Ignore(entity => entity.IncludedInBundles);
            });
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
        }
    }
}
