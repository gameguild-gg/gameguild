using System.Reflection;
using FluentAssertions;
using GameGuild.CQRS;
using GameGuild.Learning.Experience.LearningPaths;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Experience.LearningPaths.UnitTests;

public class LearningPathContractCoverageTests
{
    [Fact]
    public void Dtos_ShouldExposeAllValues()
    {
        var id = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);
        var updatedAt = createdAt.AddHours(1);
        var averageCompletion = TimeSpan.FromDays(12);
        var course = new LearningPathCourseDto(courseId, 2, false);
        var order = new CourseOrderDto(courseId, 3);

        var summary = new LearningPathDto(id, tenantId, creatorId, "Path", "path", "Description", "image.png", 40, LearningPathDifficulty.Advanced, true, false, 12, 4, 1, createdAt, updatedAt);
        var detail = new LearningPathDetailDto(id, tenantId, creatorId, "Path", "path", "Description", "image.png", 40, LearningPathDifficulty.Advanced, true, false, 12, 4, new[] { course }, createdAt, updatedAt);
        var create = new CreateLearningPathDto("Path", LearningPathDifficulty.Intermediate, "Description", "image.png", 40);
        var update = new UpdateLearningPathDto("Updated", "Updated description", "new.png", 45, LearningPathDifficulty.Expert, true);
        var addCourse = new AddCourseToPathDto(courseId, 1, false);
        var reorder = new ReorderCoursesDto(new[] { order });
        var enrollment = new LearningPathEnrollmentDto(id, id, userId, 50, 5, 10, createdAt, updatedAt, LearningPathEnrollmentStatus.Completed, createdAt, updatedAt);
        var enroll = new EnrollInPathDto(id);
        var progress = new UpdatePathProgressDto(6);
        var statistics = new LearningPathStatisticsDto(id, 10, 4, 6, 60, 75, averageCompletion);

        summary.CourseCount.Should().Be(1);
        detail.Courses.Should().ContainSingle().Which.Should().Be(course);
        create.EstimatedHours.Should().Be(40);
        update.IsFeatured.Should().BeTrue();
        addCourse.IsRequired.Should().BeFalse();
        reorder.Courses.Should().ContainSingle().Which.Should().Be(order);
        enrollment.Status.Should().Be(LearningPathEnrollmentStatus.Completed);
        enroll.LearningPathId.Should().Be(id);
        progress.CoursesCompleted.Should().Be(6);
        statistics.AverageCompletionTime.Should().Be(averageCompletion);
    }

    [Fact]
    public void EntityExtensions_ShouldMapSummaryDetailCourseAndEnrollmentDtos()
    {
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var firstCourseId = Guid.NewGuid();
        var secondCourseId = Guid.NewGuid();
        var path = LearningPath.Create(creatorId, "Path", "path", LearningPathDifficulty.Intermediate, tenantId);
        path.AddCourse(secondCourseId, 2, false);
        path.AddCourse(firstCourseId, 1, true);
        path.Publish();
        var enrollment = LearningPathEnrollment.Create(path.Id, Guid.NewGuid(), 2);
        enrollment.UpdateProgress(1);

        var summary = path.ToDto();
        var detail = path.ToDetailDto();
        var course = path.Courses.Last().ToDto();
        var enrollmentDto = enrollment.ToDto();

        summary.Id.Should().Be(path.Id);
        summary.CourseCount.Should().Be(2);
        detail.Courses.Select(c => c.CourseId).Should().Equal(firstCourseId, secondCourseId);
        course.IsRequired.Should().BeTrue();
        enrollmentDto.Progress.Should().Be(50);
    }

    [Fact]
    public void RemainingCommandsAndQueries_ShouldExposeAllValues()
    {
        var pathId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var creatorId = Guid.NewGuid();
        var courseOrder = new CourseOrderDto(courseId, 1);

        new RemoveCourseFromPathCommand(pathId, courseId).CourseId.Should().Be(courseId);
        new UnenrollFromPathCommand(pathId, userId).UserId.Should().Be(userId);
        new CompletePathCommand(pathId, userId).LearningPathId.Should().Be(pathId);
        new AbandonPathCommand(pathId, userId).UserId.Should().Be(userId);
        new GetPublishedPathsQuery(tenantId, LearningPathDifficulty.Beginner, 1, 2).TenantId.Should().Be(tenantId);
        new GetFeaturedPathsQuery(tenantId, 3).Take.Should().Be(3);
        new GetPathsByCreatorQuery(creatorId, true, 4, 5).IncludeUnpublished.Should().BeTrue();
        new GetAllPathsQuery(tenantId, false, 6, 7).IncludeUnpublished.Should().BeFalse();
        new SearchPathsQuery("term", tenantId, LearningPathDifficulty.Expert, 8, 9).SearchTerm.Should().Be("term");
        new GetUserEnrolledPathsQuery(userId, LearningPathEnrollmentStatus.Completed, 10, 11).Status.Should().Be(LearningPathEnrollmentStatus.Completed);
        new GetUserPathEnrollmentQuery(userId, pathId).LearningPathId.Should().Be(pathId);
        new CheckPathEnrollmentQuery(userId, pathId).UserId.Should().Be(userId);
        new GetPathEnrollmentsQuery(pathId, LearningPathEnrollmentStatus.InProgress, 12, 13).Take.Should().Be(13);
        new GetUserPathProgressQuery(userId, pathId).UserId.Should().Be(userId);
        new GetPopularPathsQuery(tenantId, 14, 15).DaysBack.Should().Be(14);
        new GetUserCompletedPathsQuery(userId, 16, 17).Skip.Should().Be(16);
        new ReorderPathCoursesCommand(pathId, new[] { courseOrder }).Courses.Should().ContainSingle();
    }

    [Fact]
    public void ConstructorsAndPrivateHelpers_ShouldBeCovered()
    {
        var context = new Mock<IApplicationDbContext>().Object;

        new LearningPathController(new Mock<ILearningPathService>().Object).Should().NotBeNull();
        new LearningPathService(new Mock<IMediator>().Object).Should().BeAssignableTo<ILearningPathService>();
        new LearningPathCommandHandlers(context, NullLogger<LearningPathCommandHandlers>.Instance).Should().NotBeNull();
        new LearningPathQueryHandlers(context, NullLogger<LearningPathQueryHandlers>.Instance).Should().NotBeNull();
        Activator.CreateInstance(typeof(LearningPathCourse), nonPublic: true).Should().NotBeNull();

        var method = typeof(LearningPathCommandHandlers)
            .GetMethod("GenerateSlug", BindingFlags.NonPublic | BindingFlags.Static);
        var slug = method!.Invoke(null, new object[] { "C# Path: Hello, World! Isn't Fun?" });

        slug.Should().Be("c#-path:-hello-world-isnt-fun");
    }
}
