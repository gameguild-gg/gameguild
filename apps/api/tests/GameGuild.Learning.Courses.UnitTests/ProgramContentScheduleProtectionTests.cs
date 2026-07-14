using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Courses.UnitTests;

public sealed class ProgramContentScheduleProtectionTests
{
    [Fact]
    public async Task DeleteContentAsync_WhenDescendantIsActivelyScheduled_RejectsEntireDeletion()
    {
        await using var context = CreateContext();
        var programId = Guid.NewGuid();
        var module = PersistedContent(programId, "Module");
        var lesson = PersistedContent(programId, "Lesson", module.Id);
        context.AddRange(module, lesson);
        await context.SaveChangesAsync();
        var guard = new Mock<IProgramContentScheduleGuard>();
        guard.Setup(candidate => candidate.HasActiveScheduleReference(lesson.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var service = new ProgramContentService(context, guard.Object);

        var act = () => service.DeleteContentAsync(module.Id);

        await act.Should().ThrowAsync<RequestValidationException>()
            .WithMessage("*active class schedule*");
        (await context.Set<ProgramContent>().ToArrayAsync())
            .Should().OnlyContain(content => content.DeletedAt == null);
    }

    private static ProgramContentProtectionTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ProgramContentProtectionTestContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ProgramContentProtectionTestContext(options);
    }

    private static ProgramContent PersistedContent(Guid programId, string title, Guid? parentId = null) =>
        new()
        {
            ProgramId = programId,
            ParentId = parentId,
            Title = title,
            Version = 1
        };

    private sealed class ProgramContentProtectionTestContext(DbContextOptions<ProgramContentProtectionTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
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

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
