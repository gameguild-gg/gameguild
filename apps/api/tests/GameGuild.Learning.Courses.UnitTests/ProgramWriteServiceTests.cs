using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramWriteServiceTests
{
    [Fact]
    public async Task UpdateProgramAsync_ShouldClearNullableEnrollmentControls()
    {
        await using var context = CreateContext();
        var program = new Program
        {
            Id = Guid.NewGuid(),
            Title = "Enrollment controls",
            Slug = "enrollment-controls",
            MaxEnrollments = 25,
            EnrollmentDeadline = DateTime.UtcNow.AddDays(10),
        };
        context.Set<Program>().Add(program);
        await context.SaveChangesAsync();

        var service = new ProgramWriteService(context);
        var updated = await service.UpdateProgramAsync(program.Id, new UpdateProgramDto
        {
            ClearMaxEnrollments = true,
            ClearEnrollmentDeadline = true,
        });

        updated.Should().NotBeNull();
        updated!.MaxEnrollments.Should().BeNull();
        updated.EnrollmentDeadline.Should().BeNull();
    }

    [Fact]
    public async Task UpdateProgramAsync_ShouldSetNullableEnrollmentControls()
    {
        await using var context = CreateContext();
        var program = new Program
        {
            Id = Guid.NewGuid(),
            Title = "Enrollment controls",
            Slug = "enrollment-controls",
        };
        context.Set<Program>().Add(program);
        await context.SaveChangesAsync();
        var deadline = DateTime.UtcNow.AddDays(10);

        var service = new ProgramWriteService(context);
        var updated = await service.UpdateProgramAsync(program.Id, new UpdateProgramDto
        {
            MaxEnrollments = 40,
            EnrollmentDeadline = deadline,
        });

        updated.Should().NotBeNull();
        updated!.MaxEnrollments.Should().Be(40);
        updated.EnrollmentDeadline.Should().Be(deadline);
    }

    private static LearningCoursesTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningCoursesTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new LearningCoursesTestContext(options);
    }

    private sealed class LearningCoursesTestContext(DbContextOptions<LearningCoursesTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<Program> Programs => Set<Program>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>(entity =>
            {
                entity.Ignore(program => program.ProgramContents);
                entity.Ignore(program => program.ProgramUsers);
                entity.Ignore(program => program.ProgramRatings);
                entity.Ignore(program => program.ProgramWishlists);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not needed for these service tests.");
        }
    }
}
