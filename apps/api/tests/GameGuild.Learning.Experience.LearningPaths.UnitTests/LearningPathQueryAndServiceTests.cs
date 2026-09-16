using FluentAssertions;
using GameGuild.CQRS;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.LearningPaths.UnitTests;

public sealed class LearningPathQueryHandlerTests
{
    [Fact]
    public async Task PathQueries_ShouldApplyVisibilityTenantDifficultySearchAndPagingRules()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var global = CreatePath(creatorId, "Global Beginner", "global", null, LearningPathDifficulty.Beginner, published: true, featured: true);
        var tenant = CreatePath(creatorId, "Tenant Advanced", "tenant", tenantId, LearningPathDifficulty.Advanced, published: true);
        var draft = CreatePath(creatorId, "Draft", "draft", tenantId, LearningPathDifficulty.Beginner, published: false);
        var deleted = CreatePath(creatorId, "Deleted", "deleted", tenantId, LearningPathDifficulty.Advanced, published: true);
        deleted.Version = 1;
        deleted.SoftDelete();
        context.AddRange(global, tenant, draft, deleted);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var published = (await handler.Handle(new GetPublishedPathsQuery(tenantId, LearningPathDifficulty.Advanced, 0, 10), CancellationToken.None)).ToList();
        published.Should().ContainSingle().Which.Id.Should().Be(tenant.Id);
        (await handler.Handle(new GetPublishedPathsQuery(), CancellationToken.None)).Should().HaveCount(2);

        (await handler.Handle(new GetPathBySlugQuery("global", tenantId), CancellationToken.None))!.Id.Should().Be(global.Id);
        (await handler.Handle(new GetPathBySlugQuery("tenant"), CancellationToken.None))!.Id.Should().Be(tenant.Id);
        (await handler.Handle(new GetPathByIdQuery(tenant.Id, true), CancellationToken.None))!.Courses.Should().HaveCount(1);
        (await handler.Handle(new GetPathByIdQuery(tenant.Id), CancellationToken.None))!.Id.Should().Be(tenant.Id);

        (await handler.Handle(new GetFeaturedPathsQuery(tenantId, 1), CancellationToken.None)).Should().ContainSingle().Which.Id.Should().Be(global.Id);
        (await handler.Handle(new GetFeaturedPathsQuery(), CancellationToken.None)).Should().Contain(global);
        (await handler.Handle(new GetPathsByCreatorQuery(creatorId, false, 0, 10), CancellationToken.None)).Should().HaveCount(2);
        (await handler.Handle(new GetPathsByCreatorQuery(creatorId, true, 0, 10), CancellationToken.None)).Should().HaveCount(3);
        (await handler.Handle(new GetAllPathsQuery(tenantId, false, 0, 10), CancellationToken.None)).Should().ContainSingle().Which.Id.Should().Be(tenant.Id);
        (await handler.Handle(new GetAllPathsQuery(null, true, 0, 10), CancellationToken.None)).Should().HaveCount(3);

        (await handler.Handle(new SearchPathsQuery("advanced", tenantId, LearningPathDifficulty.Advanced, 0, 10), CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(tenant.Id);
        (await handler.Handle(new SearchPathsQuery("beginner"), CancellationToken.None)).Should().ContainSingle().Which.Id.Should().Be(global.Id);
    }

    [Fact]
    public async Task EnrollmentQueries_ShouldReturnExpectedStatusesProgressAndPaging()
    {
        await using var context = CreateContext();
        var pathId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var inProgress = LearningPathEnrollment.Create(pathId, userId, 4);
        inProgress.UpdateProgress(1);
        var completed = LearningPathEnrollment.Create(pathId, Guid.NewGuid(), 1);
        completed.Complete();
        var abandoned = LearningPathEnrollment.Create(Guid.NewGuid(), userId, 2);
        abandoned.Abandon();
        context.AddRange(inProgress, completed, abandoned);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new GetUserEnrolledPathsQuery(userId, LearningPathEnrollmentStatus.InProgress, 0, 10), CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(inProgress.Id);
        (await handler.Handle(new GetUserEnrolledPathsQuery(userId), CancellationToken.None)).Should().HaveCount(2);
        (await handler.Handle(new GetUserPathEnrollmentQuery(userId, pathId), CancellationToken.None))!.Id.Should().Be(inProgress.Id);
        (await handler.Handle(new CheckPathEnrollmentQuery(userId, pathId), CancellationToken.None)).Should().BeTrue();
        (await handler.Handle(new CheckPathEnrollmentQuery(Guid.NewGuid(), pathId), CancellationToken.None)).Should().BeFalse();
        (await handler.Handle(new GetPathEnrollmentsQuery(pathId, LearningPathEnrollmentStatus.Completed, 0, 10), CancellationToken.None))
            .Should().ContainSingle().Which.Id.Should().Be(completed.Id);
        (await handler.Handle(new GetPathEnrollmentsQuery(pathId), CancellationToken.None)).Should().HaveCount(2);

        var progress = await handler.Handle(new GetUserPathProgressQuery(userId, pathId), CancellationToken.None);
        progress!.Progress.Should().Be(25);
        (await handler.Handle(new GetUserPathProgressQuery(Guid.NewGuid(), pathId), CancellationToken.None)).Should().BeNull();
        (await handler.Handle(new GetUserCompletedPathsQuery(userId, 0, 10), CancellationToken.None)).Should().BeEmpty();
        (await handler.Handle(new GetUserCompletedPathsQuery(completed.UserId), CancellationToken.None)).Should().ContainSingle();
    }

    [Fact]
    public async Task Statistics_ShouldCoverMissingEmptyAndCompletedEnrollmentCalculations()
    {
        await using var context = CreateContext();
        var emptyPath = CreatePath(Guid.NewGuid(), "Empty", "empty", null, LearningPathDifficulty.Beginner, true);
        var populatedPath = CreatePath(Guid.NewGuid(), "Populated", "populated", null, LearningPathDifficulty.Beginner, true);
        var active = LearningPathEnrollment.Create(populatedPath.Id, Guid.NewGuid(), 4);
        active.UpdateProgress(2);
        var completed = LearningPathEnrollment.Create(populatedPath.Id, Guid.NewGuid(), 1);
        completed.Complete();
        context.AddRange(emptyPath, populatedPath, active, completed);
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        (await handler.Handle(new GetPathStatisticsQuery(Guid.NewGuid()), CancellationToken.None)).Should().BeNull();
        var emptyStats = await handler.Handle(new GetPathStatisticsQuery(emptyPath.Id), CancellationToken.None);
        emptyStats!.TotalEnrollments.Should().Be(0);
        emptyStats.CompletionRate.Should().Be(0);
        emptyStats.AverageCompletionTime.Should().Be(TimeSpan.Zero);

        var stats = await handler.Handle(new GetPathStatisticsQuery(populatedPath.Id), CancellationToken.None);
        stats!.TotalEnrollments.Should().Be(2);
        stats.ActiveEnrollments.Should().Be(1);
        stats.CompletedEnrollments.Should().Be(1);
        stats.CompletionRate.Should().Be(0.5);
        stats.AverageProgress.Should().Be(75);
        stats.AverageCompletionTime.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public async Task PopularPaths_ShouldFollowRecentEnrollmentRankingAndTenantVisibility()
    {
        await using var context = CreateContext();
        var tenantId = Guid.NewGuid();
        var first = CreatePath(Guid.NewGuid(), "First", "first", tenantId, LearningPathDifficulty.Beginner, true);
        var second = CreatePath(Guid.NewGuid(), "Second", "second", null, LearningPathDifficulty.Beginner, true);
        var hiddenTenant = CreatePath(Guid.NewGuid(), "Hidden", "hidden", Guid.NewGuid(), LearningPathDifficulty.Beginner, true);
        context.AddRange(first, second, hiddenTenant);
        context.AddRange(
            LearningPathEnrollment.Create(first.Id, Guid.NewGuid(), 1),
            LearningPathEnrollment.Create(first.Id, Guid.NewGuid(), 1),
            LearningPathEnrollment.Create(second.Id, Guid.NewGuid(), 1),
            LearningPathEnrollment.Create(hiddenTenant.Id, Guid.NewGuid(), 1));
        await context.SaveChangesAsync();
        var handler = CreateHandler(context);

        var scoped = (await handler.Handle(new GetPopularPathsQuery(tenantId, 30, 10), CancellationToken.None)).ToList();
        scoped.Select(path => path.Id).Should().Equal(first.Id, second.Id);
        (await handler.Handle(new GetPopularPathsQuery(null, 30, 10), CancellationToken.None)).Should().HaveCount(3);
    }

    private static LearningPath CreatePath(
        Guid creatorId,
        string title,
        string slug,
        Guid? tenantId,
        LearningPathDifficulty difficulty,
        bool published,
        bool featured = false)
    {
        var path = LearningPath.Create(creatorId, title, slug, difficulty, tenantId, $"About {title}");
        path.AddCourse(Guid.NewGuid(), 0);
        if (featured) path.Update(null, null, null, null, null, true);
        if (published) path.Publish();
        return path;
    }

    private static LearningPathQueryHandlers CreateHandler(TestLearningPathDbContext context) =>
        new(context, NullLogger<LearningPathQueryHandlers>.Instance);

    private static TestLearningPathDbContext CreateContext() => new(
        new DbContextOptionsBuilder<TestLearningPathDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

public sealed class LearningPathServiceAndContractTests
{
    [Fact]
    public async Task Service_ShouldForwardEveryOperationToTheMediator()
    {
        var path = LearningPath.Create(Guid.NewGuid(), "Path", "path");
        var enrollment = LearningPathEnrollment.Create(path.Id, Guid.NewGuid(), 1);
        var mediator = new RecordingMediator(request => request switch
        {
            GetPublishedPathsQuery or GetFeaturedPathsQuery or GetPathsByCreatorQuery or SearchPathsQuery or GetPopularPathsQuery => new[] { path },
            GetPathByIdQuery or GetPathBySlugQuery or CreateLearningPathCommand or UpdateLearningPathCommand or PublishLearningPathCommand or
                UnpublishLearningPathCommand or AddCourseToPathCommand or ReorderPathCoursesCommand => path,
            GetUserEnrolledPathsQuery or GetPathEnrollmentsQuery or GetUserCompletedPathsQuery => new[] { enrollment },
            GetUserPathEnrollmentQuery or EnrollInPathCommand or UpdatePathProgressCommand or CompletePathCommand => enrollment,
            GetPathStatisticsQuery => new LearningPathStatisticsDto(path.Id, 1, 1, 0, 0, 10, TimeSpan.Zero),
            CheckPathEnrollmentQuery or DeleteLearningPathCommand or RemoveCourseFromPathCommand or UnenrollFromPathCommand or AbandonPathCommand => true,
            _ => null
        });
        var service = new LearningPathService(mediator);
        var createDto = new CreateLearningPathDto("Path", LearningPathDifficulty.Beginner, "Description", "image", 2);
        var updateDto = new UpdateLearningPathDto("Updated", "Description", "image", 3, LearningPathDifficulty.Advanced, true);
        var addDto = new AddCourseToPathDto(Guid.NewGuid(), 0, true);
        var reorderDto = new ReorderCoursesDto([new CourseOrderDto(addDto.CourseId, 0)]);
        var progressDto = new UpdatePathProgressDto(1);

        await service.GetPathByIdAsync(path.Id, true);
        await service.GetPathBySlugAsync(path.Slug, path.TenantId);
        await service.GetPublishedPathsAsync();
        await service.GetFeaturedPathsAsync();
        await service.GetPathsByCreatorAsync(path.CreatorId);
        await service.SearchPathsAsync("path");
        await service.CreatePathAsync(createDto, path.CreatorId);
        await service.UpdatePathAsync(path.Id, updateDto);
        await service.DeletePathAsync(path.Id);
        await service.PublishPathAsync(path.Id);
        await service.UnpublishPathAsync(path.Id);
        await service.AddCourseToPathAsync(path.Id, addDto);
        await service.RemoveCourseFromPathAsync(path.Id, addDto.CourseId);
        await service.ReorderCoursesAsync(path.Id, reorderDto);
        await service.EnrollAsync(path.Id, enrollment.UserId);
        await service.UnenrollAsync(path.Id, enrollment.UserId);
        await service.UpdateProgressAsync(path.Id, enrollment.UserId, progressDto);
        await service.CompletePathAsync(path.Id, enrollment.UserId);
        await service.AbandonPathAsync(path.Id, enrollment.UserId);
        await service.IsEnrolledAsync(path.Id, enrollment.UserId);
        await service.GetEnrollmentAsync(path.Id, enrollment.UserId);
        await service.GetUserEnrollmentsAsync(enrollment.UserId);
        await service.GetPathEnrollmentsAsync(path.Id);
        await service.GetUserCompletedPathsAsync(enrollment.UserId);
        await service.GetPathStatisticsAsync(path.Id);
        await service.GetPopularPathsAsync();

        mediator.Requests.Should().HaveCount(26);
    }

    [Fact]
    public void DtoMappingsAndRecords_ShouldExposeTheCompleteContract()
    {
        var path = LearningPath.Create(
            Guid.NewGuid(), "Path", "path", LearningPathDifficulty.Intermediate, Guid.NewGuid(), "Description", "image", 8);
        path.AddCourse(Guid.NewGuid(), 2, false);
        path.Update(null, null, null, null, null, true);
        path.Publish();
        var enrollment = LearningPathEnrollment.Create(path.Id, Guid.NewGuid(), 1);
        enrollment.Complete();

        var summary = path.ToDto();
        var detail = path.ToDetailDto();
        var course = path.Courses.Single().ToDto();
        var enrollmentDto = enrollment.ToDto();

        summary.CourseCount.Should().Be(1);
        summary.Description.Should().Be("Description");
        detail.Courses.Should().ContainSingle().Which.Should().Be(course);
        enrollmentDto.Status.Should().Be(LearningPathEnrollmentStatus.Completed);

        new EnrollInPathDto(path.Id).LearningPathId.Should().Be(path.Id);
        new UpdatePathProgressDto(1).CoursesCompleted.Should().Be(1);
        new ReorderCoursesDto([new CourseOrderDto(course.CourseId, 0)]).Courses.Should().ContainSingle();
        new LearningPathStatisticsDto(path.Id, 1, 0, 1, 1, 100, TimeSpan.FromHours(1)).AverageProgress.Should().Be(100);
    }

    [Fact]
    public void ModuleRegistration_ShouldRegisterTheLearningPathService()
    {
        var services = new ServiceCollection();

        services.AddLearningPathsModule();

        services.Should().Contain(descriptor =>
            descriptor.ServiceType == typeof(ILearningPathService) &&
            descriptor.ImplementationType == typeof(LearningPathService) &&
            descriptor.Lifetime == ServiceLifetime.Scoped);
    }

    [Fact]
    public void FrameworkConstructors_ShouldBeAvailableForApiAndEfMaterialization()
    {
        var controller = new LearningPathController(Mock.Of<ILearningPathService>(), Mock.Of<ISender>());
        var efCourse = Activator.CreateInstance(typeof(LearningPathCourse), nonPublic: true);

        controller.Should().NotBeNull();
        efCourse.Should().BeOfType<LearningPathCourse>();
    }
}

internal sealed class RecordingMediator(Func<object, object?> responseFactory) : IMediator
{
    public List<object> Requests { get; } = [];

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult((TResponse)responseFactory(request)!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        Requests.Add(request);
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        return Task.FromResult(responseFactory(request));
    }

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;

    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
