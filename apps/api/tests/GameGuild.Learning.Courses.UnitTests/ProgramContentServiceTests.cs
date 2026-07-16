using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using Moq;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramContentServiceTests
{
    [Fact]
    public async Task DeleteContentAsync_ShouldSoftDeleteNestedDescendants()
    {
        await using var context = CreateContext();
        var programId = Guid.NewGuid();
        var module = PersistedContent(programId, "Module");
        var submodule = PersistedContent(programId, "Submodule", module.Id);
        var lesson = PersistedContent(programId, "Lesson", submodule.Id);

        context.Set<ProgramContent>().AddRange(module, submodule, lesson);
        await context.SaveChangesAsync();

        var service = new ProgramContentService(
            context,
            Mock.Of<IProgramContentScheduleGuard>(),
            Mock.Of<IProgramContentLifecycleGuard>());

        var deleted = await service.DeleteContentAsync(module.Id);

        deleted.Should().BeTrue();
        var contents = await context.Set<ProgramContent>().IgnoreQueryFilters().ToListAsync();
        contents.Should().OnlyContain(content => content.DeletedAt != null);
    }

    private static LearningCoursesTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LearningCoursesTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new LearningCoursesTestContext(options);
    }

    private static ProgramContent PersistedContent(Guid programId, string title, Guid? parentId = null)
    {
        return new ProgramContent
        {
            ProgramId = programId,
            ParentId = parentId,
            Title = title,
            Version = 1,
        };
    }

    private sealed class LearningCoursesTestContext(DbContextOptions<LearningCoursesTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public DbSet<ProgramContent> ProgramContents => Set<ProgramContent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.Ignore(content => content.Program);
                entity.Ignore(content => content.ContentInteractions);
                entity.HasMany(content => content.Children)
                    .WithOne(content => content.Parent)
                    .HasForeignKey(content => content.ParentId);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not needed for these service tests.");
        }
    }
}
