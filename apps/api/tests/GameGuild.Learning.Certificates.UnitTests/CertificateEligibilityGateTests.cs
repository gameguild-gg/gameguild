using FluentAssertions;
using GameGuild.Identity.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

using LearningProgram = GameGuild.Learning.Courses.Program;
using LearningProgramEnrollment = GameGuild.Learning.Courses.ProgramEnrollment;
using LearningCompletionStatus = GameGuild.Learning.Courses.CompletionStatus;

namespace GameGuild.Learning.Certificates.Tests;

/// <summary>
/// Eligibility gate for certificate issuance (issue #1226 N1): <c>IssueCertificateAsync</c>
/// verifies the enrollment it certifies exists, belongs to the requested recipient and
/// course, and is completed — so the endpoint path (which previously called issuance
/// directly, skipping <c>CheckEligibilityAsync</c>) can no longer mint certificates for
/// nonexistent, in-progress or foreign enrollments.
/// </summary>
public class CertificateEligibilityGateTests
{
    [Fact]
    public async Task Issuance_Fails_WhenEnrollmentDoesNotExist()
    {
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);

        var result = await service.IssueCertificateAsync(seed.TemplateId, Guid.NewGuid(), seed.UserId, seed.CourseId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.NotFound);
        CertificatesPersisted(context).Should().Be(0);
    }

    [Fact]
    public async Task Issuance_Fails_WhenEnrollmentIsNotCompleted()
    {
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);
        var inProgress = SeedEnrollment(context, seed.UserId, seed.CourseId, LearningCompletionStatus.InProgress);

        var result = await service.IssueCertificateAsync(seed.TemplateId, inProgress, seed.UserId, seed.CourseId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        CertificatesPersisted(context).Should().Be(0);
    }

    [Fact]
    public async Task Issuance_Fails_WhenEnrollmentBelongsToAnotherUser()
    {
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);
        var someoneElses = SeedEnrollment(context, Guid.NewGuid(), seed.CourseId, LearningCompletionStatus.Completed);

        var result = await service.IssueCertificateAsync(seed.TemplateId, someoneElses, seed.UserId, seed.CourseId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Forbidden);
        CertificatesPersisted(context).Should().Be(0);
    }

    [Fact]
    public async Task Issuance_Fails_WhenEnrollmentBelongsToAnotherCourse()
    {
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);
        var otherCourse = SeedEnrollment(context, seed.UserId, Guid.NewGuid(), LearningCompletionStatus.Completed);

        var result = await service.IssueCertificateAsync(seed.TemplateId, otherCourse, seed.UserId, seed.CourseId);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Conflict);
        CertificatesPersisted(context).Should().Be(0);
    }

    [Fact]
    public async Task Issuance_Succeeds_ForCompletedOwnedEnrollment()
    {
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);

        var result = await service.IssueCertificateAsync(seed.TemplateId, seed.EnrollmentId, seed.UserId, seed.CourseId);

        result.IsSuccess.Should().BeTrue();
        result.Value.EnrollmentId.Should().Be(seed.EnrollmentId);
        result.Value.UserId.Should().Be(seed.UserId);
        result.Value.CourseId.Should().Be(seed.CourseId);
    }

    [Fact]
    public async Task Issuance_Succeeds_ForCompletedWithCertificateStatus()
    {
        // CompletedWithCertificate is the other terminal completion status the eligibility
        // check accepts; issuance must not reject it.
        await using var context = CreateContext();
        var (service, seed) = CreateService(context);
        var completedWithCertificate = SeedEnrollment(
            context, seed.UserId, seed.CourseId, LearningCompletionStatus.CompletedWithCertificate);

        var result = await service.IssueCertificateAsync(seed.TemplateId, completedWithCertificate, seed.UserId, seed.CourseId);

        result.IsSuccess.Should().BeTrue();
    }

    private static int CertificatesPersisted(EligibilityGateDbContext context) =>
        context.Set<Certificate>().AsNoTracking().Count();

    private static (CertificateService Service, Seed Seed) CreateService(EligibilityGateDbContext context)
    {
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var template = CertificateTemplate.Create(courseId, "Completion", "<html />");
        var user = new User { Id = userId, Email = "learner@example.com", Username = "learner", Name = "Learner" };
        var program = new LearningProgram { Id = courseId, Title = "Gated Course" };
        var enrollmentId = SeedEnrollment(context, userId, courseId, LearningCompletionStatus.Completed);
        context.Set<CertificateTemplate>().Add(template);
        context.Set<User>().Add(user);
        context.Set<LearningProgram>().Add(program);
        context.SaveChanges();

        var service = new CertificateService(context, NullLogger<CertificateService>.Instance, new NoopLearningAuditEventSink());
        return (service, new Seed(template.Id, enrollmentId, userId, courseId));
    }

    private static Guid SeedEnrollment(
        EligibilityGateDbContext context,
        Guid userId,
        Guid courseId,
        LearningCompletionStatus completionStatus)
    {
        var enrollment = new LearningProgramEnrollment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramId = courseId,
            CompletionStatus = completionStatus
        };
        context.Set<LearningProgramEnrollment>().Add(enrollment);
        context.SaveChanges();
        return enrollment.Id;
    }

    private static EligibilityGateDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<EligibilityGateDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new EligibilityGateDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private sealed record Seed(Guid TemplateId, Guid EnrollmentId, Guid UserId, Guid CourseId);

    private sealed class EligibilityGateDbContext(DbContextOptions<EligibilityGateDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new CertificatesModelConfiguration().Configure(modelBuilder);
            // ProgramEnrollment (the eligibility gate's aggregate) carries PercentValue
            // properties that need the production model configuration's converters; the
            // Program relationships pull in the Commerce Product, whose navigations must
            // be ignored for the isolated test.
            new GameGuild.Learning.Courses.CoursesModelConfiguration().Configure(modelBuilder);
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
