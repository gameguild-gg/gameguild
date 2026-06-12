using FluentAssertions;
using GameGuild.Identity.Users;
using GameGuild.Learning.Abstractions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

using LearningProgram = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Certificates.Tests;

public class CertificatesModuleAndServiceTests
{
    [Fact]
    public void AddCertificatesModule_RegistersServices()
    {
        var services = new ServiceCollection();
        services.AddScoped<IApplicationDbContext>(_ => Mock.Of<IApplicationDbContext>());
        services.AddScoped(typeof(ILogger<>), typeof(NullLogger<>));

        services.AddCertificatesModule();

        var provider = services.BuildServiceProvider();
        provider.GetService<ICertificateService>().Should().NotBeNull();
        provider.GetService<ICertificateIssuanceService>().Should().NotBeNull();
        provider.GetService<ICertificateTemplateService>().Should().NotBeNull();
    }

    [Fact]
    public void CertificateService_CanBeInstantiated()
    {
        var service = new CertificateService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<CertificateService>.Instance);

        service.Should().NotBeNull();
        service.Should().BeAssignableTo<ICertificateIssuanceService>();
    }

    [Fact]
    public async Task GenerateCertificateNumberAsync_ShouldReturnConventionalNumber()
    {
        var service = new CertificateService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<CertificateService>.Instance);

        var number = await service.GenerateCertificateNumberAsync();

        number.Should().StartWith($"CERT-{SystemClock.UtcNow:yyyyMMdd}-");
        number.Length.Should().Be("CERT-yyyyMMdd-xxxxxxxx".Length);
    }

    [Fact]
    public async Task IssueCertificateForEnrollmentAsync_ShouldCreateCertificateAndReportExistingCertificate()
    {
        await using var context = CertificateServiceDbContext.Create();
        var service = new CertificateService(context, NullLogger<CertificateService>.Instance);
        var enrollmentId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var programId = Guid.NewGuid();
        var template = CertificateTemplate.Create(programId, "Default completion", "<html />");
        context.Set<CertificateTemplate>().Add(template);
        await context.SaveChangesAsync();

        var result = await service.IssueCertificateForEnrollmentAsync(enrollmentId, userId, programId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        var hasCertificate = await service.HasCertificateAsync(enrollmentId);
        hasCertificate.Should().BeTrue();
        var certificate = await context.Set<Certificate>().SingleAsync(c => c.Id == result.Value);
        certificate.TemplateId.Should().Be(template.Id);
        certificate.EnrollmentId.Should().Be(enrollmentId);
        certificate.UserId.Should().Be(userId);
        certificate.CourseId.Should().Be(programId);
    }

    [Fact]
    public void CertificateTemplateService_CanBeInstantiated()
    {
        var service = new CertificateTemplateService(
            Mock.Of<IApplicationDbContext>(),
            NullLogger<CertificateTemplateService>.Instance);

        service.Should().NotBeNull();
    }

    private sealed class CertificateServiceDbContext(DbContextOptions<CertificateServiceDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public static CertificateServiceDbContext Create()
        {
            var options = new DbContextOptionsBuilder<CertificateServiceDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            return new CertificateServiceDbContext(options);
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
        }
    }
}
