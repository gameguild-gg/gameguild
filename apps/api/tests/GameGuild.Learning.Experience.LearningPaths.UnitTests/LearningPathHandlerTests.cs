using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.LearningPaths.UnitTests;

public sealed class LearningPathDomainBehaviorTests
{
    [Fact]
    public void CreateAndUpdate_ShouldPreserveAndApplyOptionalMetadata()
    {
        var path = LearningPath.Create(
            Guid.NewGuid(),
            "Original",
            "original",
            LearningPathDifficulty.Beginner,
            Guid.NewGuid(),
            "Description",
            "https://example.test/original.png",
            12);

        path.Description.Should().Be("Description");
        path.ImageUrl.Should().Be("https://example.test/original.png");
        path.EstimatedHours.Should().Be(12);

        path.Update(
            "Updated",
            "Updated description",
            "https://example.test/updated.png",
            24,
            LearningPathDifficulty.Advanced,
            true);

        path.Title.Should().Be("Updated");
        path.Description.Should().Be("Updated description");
        path.ImageUrl.Should().Be("https://example.test/updated.png");
        path.EstimatedHours.Should().Be(24);
        path.Difficulty.Should().Be(LearningPathDifficulty.Advanced);
        path.IsFeatured.Should().BeTrue();

        path.Update(null, null, null, null, null, null);
        path.Title.Should().Be("Updated");
    }

    [Fact]
    public void CourseAndEnrollmentStateChanges_ShouldMutateTheirState()
    {
        var course = new LearningPathCourse(Guid.NewGuid(), Guid.NewGuid(), 1, true);
        course.SetOrder(3);
        course.Order.Should().Be(3);

        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 3);
        enrollment.Abandon();
        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Abandoned);
        enrollment.CompletedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void UpdateProgress_OutsideCourseRange_ShouldThrow(int coursesCompleted)
    {
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 3);

        var act = () => enrollment.UpdateProgress(coursesCompleted);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}

public sealed class LearningPathCommandHandlerTests
{
    [Fact]
    public async Task Create_ShouldPersistMetadataAndScopeSlugUniquenessToTenant()
    {
        await using var context = CreateContext();
        var tenant = Guid.NewGuid();
        var otherTenant = Guid.NewGuid();
        context.Add(LearningPath.Create(Guid.NewGuid(), "Existing", "new-path", tenantId: otherTenant));
        context.Add(LearningPath.Create(Guid.NewGuid(), "Existing", "new-path", tenantId: tenant));
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new CreateLearningPathCommand(
            Guid.NewGuid(),
            "New Path",
            LearningPathDifficulty.Expert,
            tenant,
            "Useful path",
            "https://example.test/path.png",
            20), CancellationToken.None);

        result.Slug.Should().StartWith("new-path-");
        result.Description.Should().Be("Useful path");
        result.ImageUrl.Should().Be("https://example.test/path.png");
        result.EstimatedHours.Should().Be(20);
        result.Difficulty.Should().Be(LearningPathDifficulty.Expert);
        context.Set<LearningPath>().Should().Contain(result);
    }

    [Fact]
    public async Task Create_WhenSlugExistsOnlyInAnotherTenant_ShouldKeepBaseSlug()
    {
        await using var context = CreateContext();
        context.Add(LearningPath.Create(Guid.NewGuid(), "Existing", "tenant-path", tenantId: Guid.NewGuid()));
        await context.SaveChangesAsync();

        var result = await CreateHandler(context).Handle(new CreateLearningPathCommand(
            Guid.NewGuid(), "Tenant Path", TenantId: Guid.NewGuid()), CancellationToken.None);

        result.Slug.Should().Be("tenant-path");
    }

    [Fact]
    public async Task UpdateAndDelete_ShouldHandleMissingAndPersistedPaths()
    {
        await using var context = CreateContext();
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        path.Version = 1;
        context.Add(path);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new UpdateLearningPathCommand(
            path.Id,
            "Updated",
            "Description",
            "image",
            5,
            LearningPathDifficulty.Intermediate,
            true), CancellationToken.None))!.Title.Should().Be("Updated");
        (await handler.Handle(new UpdateLearningPathCommand(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new DeleteLearningPathCommand(Guid.NewGuid()), CancellationToken.None)).Should().BeFalse();
        (await handler.Handle(new DeleteLearningPathCommand(path.Id), CancellationToken.None)).Should().BeTrue();
        path.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PublishAndUnpublish_ShouldCoverValidationAndLifecycle()
    {
        await using var context = CreateContext();
        var emptyPath = LearningPath.Create(Guid.NewGuid(), "Empty", "empty");
        var populatedPath = LearningPath.Create(Guid.NewGuid(), "Populated", "populated");
        populatedPath.AddCourse(Guid.NewGuid(), 0);
        context.AddRange(emptyPath, populatedPath);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new PublishLearningPathCommand(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
        await FluentActions.Invoking(() => handler.Handle(new PublishLearningPathCommand(emptyPath.Id), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        (await handler.Handle(new PublishLearningPathCommand(populatedPath.Id), CancellationToken.None))!.IsPublished.Should().BeTrue();
        (await handler.Handle(new UnpublishLearningPathCommand(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new UnpublishLearningPathCommand(populatedPath.Id), CancellationToken.None))!.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task AddAndRemoveCourse_ShouldCoverMissingDuplicateAndSuccess()
    {
        await using var context = CreateContext();
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var existingCourseId = Guid.NewGuid();
        path.AddCourse(existingCourseId, 0);
        context.Add(path);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new AddCourseToPathCommand(Guid.NewGuid(), Guid.NewGuid(), 0), CancellationToken.None)).Should().BeNull();
        await FluentActions.Invoking(() => handler.Handle(new AddCourseToPathCommand(path.Id, existingCourseId, 0), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        var addedCourseId = Guid.NewGuid();
        (await handler.Handle(new AddCourseToPathCommand(path.Id, addedCourseId, 1, false), CancellationToken.None))!
            .Courses.Should().Contain(course => course.CourseId == addedCourseId && !course.IsRequired);
        (await handler.Handle(new RemoveCourseFromPathCommand(path.Id, Guid.NewGuid()), CancellationToken.None)).Should().BeFalse();
        (await handler.Handle(new RemoveCourseFromPathCommand(path.Id, addedCourseId), CancellationToken.None)).Should().BeTrue();
    }

    [Fact]
    public async Task Reorder_ShouldPersistTheRequestedOrderInATransaction()
    {
        await using var context = CreateContext();
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        path.AddCourse(first, 0);
        path.AddCourse(second, 1);
        context.Add(path);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var result = await handler.Handle(new ReorderPathCoursesCommand(path.Id,
            [new CourseOrderDto(first, 1), new CourseOrderDto(second, 0)]), CancellationToken.None);

        result!.Courses.Single(course => course.CourseId == first).Order.Should().Be(1);
        result.Courses.Single(course => course.CourseId == second).Order.Should().Be(0);
        context.LastTransaction!.Verify(transaction => transaction.CommitAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task Reorder_ShouldRejectMissingDuplicateUnknownAndCollidingEntries()
    {
        await using var context = CreateContext();
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        path.AddCourse(first, 0);
        path.AddCourse(second, 1);
        context.Add(path);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new ReorderPathCoursesCommand(Guid.NewGuid(), [new CourseOrderDto(first, 0)]), CancellationToken.None)).Should().BeNull();
        await AssertInvalidReorder(handler, path.Id, [new CourseOrderDto(first, 0), new CourseOrderDto(first, 1)]);
        await AssertInvalidReorder(handler, path.Id, [new CourseOrderDto(first, 0), new CourseOrderDto(second, 0)]);
        await AssertInvalidReorder(handler, path.Id, [new CourseOrderDto(Guid.NewGuid(), 0)]);
        await AssertInvalidReorder(handler, path.Id, [new CourseOrderDto(first, 1)]);
    }

    [Fact]
    public async Task EnrollmentCommands_ShouldCoverSuccessMissingAndInvalidStates()
    {
        await using var context = CreateContext();
        var unpublished = LearningPath.Create(Guid.NewGuid(), "Draft", "draft");
        var published = LearningPath.Create(Guid.NewGuid(), "Published", "published");
        published.AddCourse(Guid.NewGuid(), 0);
        published.Publish();
        context.AddRange(unpublished, published);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);
        var userId = Guid.NewGuid();

        await FluentActions.Invoking(() => handler.Handle(new EnrollInPathCommand(unpublished.Id, userId), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        var enrollment = await handler.Handle(new EnrollInPathCommand(published.Id, userId), CancellationToken.None);
        enrollment.TotalCourses.Should().Be(1);
        await FluentActions.Invoking(() => handler.Handle(new EnrollInPathCommand(published.Id, userId), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();

        (await handler.Handle(new UpdatePathProgressCommand(published.Id, Guid.NewGuid(), 0), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new UpdatePathProgressCommand(published.Id, userId, 1), CancellationToken.None))!.Status
            .Should().Be(LearningPathEnrollmentStatus.Completed);
        (await handler.Handle(new CompletePathCommand(published.Id, Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new CompletePathCommand(published.Id, userId), CancellationToken.None))!.Progress.Should().Be(100);
        (await handler.Handle(new AbandonPathCommand(published.Id, Guid.NewGuid()), CancellationToken.None)).Should().BeFalse();
        (await handler.Handle(new AbandonPathCommand(published.Id, userId), CancellationToken.None)).Should().BeTrue();
        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Abandoned);
    }

    [Fact]
    public async Task Unenroll_ShouldSoftDeletePersistedEnrollmentOrReportMissing()
    {
        await using var context = CreateContext();
        var enrollment = LearningPathEnrollment.Create(Guid.NewGuid(), Guid.NewGuid(), 1);
        enrollment.Version = 1;
        context.Add(enrollment);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new UnenrollFromPathCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None)).Should().BeFalse();
        (await handler.Handle(new UnenrollFromPathCommand(enrollment.LearningPathId, enrollment.UserId), CancellationToken.None)).Should().BeTrue();
        enrollment.DeletedAt.Should().NotBeNull();
    }

    private static async Task AssertInvalidReorder(
        LearningPathCommandHandlers handler,
        Guid pathId,
        IEnumerable<CourseOrderDto> orders)
    {
        await FluentActions.Invoking(() => handler.Handle(new ReorderPathCoursesCommand(pathId, orders), CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    private static LearningPathCommandHandlers CreateHandler(TestLearningPathDbContext context) =>
        new(context, NullLogger<LearningPathCommandHandlers>.Instance);

    private static TestLearningPathDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TestLearningPathDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

internal sealed class TestLearningPathDbContext(DbContextOptions<TestLearningPathDbContext> options)
    : DbContext(options), IApplicationDbContext
{
    public Mock<IDbContextTransaction>? LastTransaction { get; private set; }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        LastTransaction = new Mock<IDbContextTransaction>();
        return Task.FromResult(LastTransaction.Object);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new LearningPathsModelConfiguration().Configure(modelBuilder);
    }
}
