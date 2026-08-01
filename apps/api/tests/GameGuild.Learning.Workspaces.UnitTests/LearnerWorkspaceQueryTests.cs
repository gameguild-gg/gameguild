using FluentAssertions;
using GameGuild.Learning.Assessments;
using GameGuild.Learning.Certificates;
using GameGuild.Learning.Cohorts;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Experience.Social;
using GameGuild.Learning.Workspaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;
using LearningEnrollment = GameGuild.Learning.Enrollments.Enrollment;
using Program = GameGuild.Learning.Courses.Program;

namespace GameGuild.Learning.Workspaces.UnitTests;

public sealed class LearnerWorkspaceQueryTests
{
    [Fact]
    public async Task Dashboard_returns_only_the_actor_courses_with_batched_learning_context()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var course = CreateCourse("game-ai", "Game AI");
        var hiddenCourse = CreateCourse("hidden", "Hidden");
        var enrollment = CreateEnrollment(userId, course.Id, 50m);
        var otherEnrollment = CreateEnrollment(otherUserId, hiddenCourse.Id, 80m);
        var module = CreateContent(course.Id, "Foundations", ProgramContentType.Module, 0);
        var lesson = CreateContent(course.Id, "Pathfinding", ProgramContentType.Lesson, 1, module.Id, 45);
        var progress = new ContentProgress
        {
            UserId = userId,
            ContentId = lesson.Id,
            ProgramEnrollmentId = enrollment.Id,
            CompletionStatus = ContentCompletionStatus.InProgress,
            ProgressPercentage = 40m,
            TimeSpentSeconds = 900,
        };
        var cohort = Cohort.Create(
            course.Id,
            "Evening",
            DateTime.UtcNow.AddDays(-2),
            DateTime.UtcNow.AddMonths(2),
            30);
        var learningEnrollment = LearningEnrollment.Create(course.Id, userId, cohort.Id);
        var meeting = CohortScheduleItem.Create(
            cohort.Id,
            lesson.Id,
            null,
            CohortScheduleItemType.LiveSession,
            "Pathfinding workshop",
            startsAt: DateTime.UtcNow.AddDays(1),
            endsAt: DateTime.UtcNow.AddDays(1).AddHours(2),
            status: CohortScheduleItemStatus.Published);
        var group = AssessmentGroup.Create(course.Id, "Quizzes", 20m);
        var assessment = Assessment.Create(course.Id, "Pathfinding quiz", AssessmentType.Quiz, 10, 7, assessmentGroupId: group.Id);
        assessment.SetDeliverySchedule(
            DateTime.UtcNow.AddDays(-1),
            DateTime.UtcNow.AddDays(3),
            DateTime.UtcNow.AddDays(2),
            false,
            null);
        var submission = AssessmentSubmission.Start(assessment.Id, enrollment.Id, userId, 1);
        submission.Submit();
        submission.Grade(8, 7, 10);
        var discussion = CourseDiscussion.Create(course.Id, userId, "Welcome", "Start here");
        discussion.Pin();
        var certificate = Certificate.Issue(
            Guid.NewGuid(),
            enrollment.Id,
            userId,
            course.Id,
            "Ada Learner",
            course.Title);

        context.AddRange(
            course,
            hiddenCourse,
            enrollment,
            otherEnrollment,
            module,
            lesson,
            progress,
            cohort,
            learningEnrollment,
            meeting,
            group,
            assessment,
            submission,
            discussion,
            certificate);
        await context.SaveChangesAsync();

        var result = await new GetLearnerDashboardQueryHandler(context)
            .Handle(new GetLearnerDashboardQuery(userId), CancellationToken.None);

        result.Courses.Should().ContainSingle();
        result.Courses[0].CourseId.Should().Be(course.Id);
        result.Courses[0].CurrentContentId.Should().Be(lesson.Id);
        result.Courses[0].CurrentContentTitle.Should().Be(lesson.Title);
        result.Courses[0].CurrentContentType.Should().Be(nameof(ProgramContentType.Lesson));
        result.Upcoming.Should().ContainSingle(entry => entry.ScheduleItemId == meeting.Id);
        result.Deadlines.Should().ContainSingle(item => item.AssessmentId == assessment.Id);
        result.Grades.Should().ContainSingle(item => item.Percentage == 80m);
        result.Certificates.Should().ContainSingle(item => item.CertificateId == certificate.Id);
        result.Announcements.Should().ContainSingle(item => item.DiscussionId == discussion.Id);
    }

    [Fact]
    public async Task Workspace_returns_null_when_the_actor_is_not_enrolled()
    {
        await using var context = CreateContext();
        var course = CreateCourse("secure-course", "Secure course");
        context.Add(course);
        await context.SaveChangesAsync();

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(new GetLearnerCourseWorkspaceQuery(Guid.NewGuid(), course.Id), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Workspace_returns_the_complete_authorized_course_context()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var course = CreateCourse("workspace", "Workspace");
        var enrollment = CreateEnrollment(userId, course.Id, 25m);
        var lesson = CreateContent(course.Id, "First lesson", ProgramContentType.Lesson, 0, estimatedMinutes: 30);
        var progress = new ContentProgress
        {
            UserId = userId,
            ContentId = lesson.Id,
            ProgramEnrollmentId = enrollment.Id,
            CompletionStatus = ContentCompletionStatus.Completed,
            ProgressPercentage = 100m,
            CompletedAt = DateTime.UtcNow,
        };
        var group = AssessmentGroup.Create(course.Id, "Assignments", 100m);
        var assessment = Assessment.Create(course.Id, "Practice", AssessmentType.Assignment, 20, 12, assessmentGroupId: group.Id);
        assessment.Update(null, null, null, null, null, null, null, null, null, contentId: lesson.Id);
        var discussion = CourseDiscussion.Create(course.Id, userId, "Question", "How does this work?", lesson.Id);
        var certificate = Certificate.Issue(
            Guid.NewGuid(),
            enrollment.Id,
            userId,
            course.Id,
            "Grace Learner",
            course.Title);

        context.AddRange(course, enrollment, lesson, progress, group, assessment, discussion, certificate);
        await context.SaveChangesAsync();

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(new GetLearnerCourseWorkspaceQuery(userId, course.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Course.CourseId.Should().Be(course.Id);
        result.Content.Should().ContainSingle(item => item.ContentId == lesson.Id);
        result.Progress.Should().ContainSingle(item => item.ContentId == lesson.Id);
        result.AssessmentGroups.Should().ContainSingle(item => item.GroupId == group.Id);
        result.Assessments.Should().ContainSingle(item => item.AssessmentId == assessment.Id);
        result.Discussions.Should().ContainSingle(item => item.DiscussionId == discussion.Id);
        result.Certificates.Should().ContainSingle(item => item.CertificateId == certificate.Id);
    }

    [Fact]
    public async Task Search_only_returns_results_from_enrolled_courses_and_enforces_the_limit()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var enrolled = CreateCourse("game-ai", "Advanced Game AI");
        var privateCourse = CreateCourse("private-ai", "Private Game AI");
        var enrollment = CreateEnrollment(userId, enrolled.Id, 0m);
        var lesson = CreateContent(enrolled.Id, "AI Pathfinding", ProgramContentType.Lesson, 0);
        var privateLesson = CreateContent(privateCourse.Id, "AI Secrets", ProgramContentType.Lesson, 0);
        context.AddRange(enrolled, privateCourse, enrollment, lesson, privateLesson);
        await context.SaveChangesAsync();

        var handler = new SearchLearnerWorkspaceQueryHandler(context);
        var result = await handler.Handle(
            new SearchLearnerWorkspaceQuery(userId, "ai", 1),
            CancellationToken.None);

        result.Should().ContainSingle();
        result[0].CourseId.Should().Be(enrolled.Id);
        result.Should().NotContain(item => item.CourseId == privateCourse.Id);
    }

    [Fact]
    public async Task Search_returns_empty_for_queries_shorter_than_two_characters()
    {
        await using var context = CreateContext();

        var result = await new SearchLearnerWorkspaceQueryHandler(context)
            .Handle(new SearchLearnerWorkspaceQuery(Guid.NewGuid(), "a"), CancellationToken.None);

        result.Should().BeEmpty();
    }

    private static Program CreateCourse(string slug, string title)
    {
        return new Program
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Title = title,
            Description = $"{title} description",
            Status = ContentStatus.Published,
            EnrollmentStatus = GameGuild.Learning.Courses.EnrollmentStatus.Open,
            EstimatedHours = 12,
        };
    }

    private static ProgramEnrollment CreateEnrollment(Guid userId, Guid courseId, decimal progress)
    {
        return new ProgramEnrollment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramId = courseId,
            EnrollmentStatus = GameGuild.Learning.Courses.EnrollmentStatus.Active,
            CompletionStatus = CompletionStatus.InProgress,
            ProgressPercentage = progress,
        };
    }

    private static ProgramContent CreateContent(
        Guid courseId,
        string title,
        ProgramContentType type,
        int order,
        Guid? parentId = null,
        int? estimatedMinutes = null)
    {
        return new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = courseId,
            ParentId = parentId,
            Title = title,
            Type = type,
            SortOrder = order,
            EstimatedMinutes = estimatedMinutes,
            IsRequired = true,
        };
    }

    private static TestApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new TestApplicationDbContext(options);
    }

    private sealed class TestApplicationDbContext(DbContextOptions<TestApplicationDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Program>().Ignore(item => item.ProgramContents);
            modelBuilder.Entity<Program>().Ignore(item => item.ProgramUsers);
            modelBuilder.Entity<Program>().Ignore(item => item.ProgramRatings);
            modelBuilder.Entity<Program>().Ignore(item => item.ProgramWishlists);
            modelBuilder.Entity<ProgramEnrollment>().Ignore(item => item.Program);
            modelBuilder.Entity<ProgramEnrollment>().Ignore(item => item.User);
            modelBuilder.Entity<ProgramContent>().Ignore(item => item.Program);
            modelBuilder.Entity<ProgramContent>().Ignore(item => item.Parent);
            modelBuilder.Entity<ProgramContent>().Ignore(item => item.Children);
            modelBuilder.Entity<ProgramContent>().Ignore(item => item.ContentInteractions);
            modelBuilder.Entity<ContentProgress>().Ignore(item => item.User);
            modelBuilder.Entity<ContentProgress>().Ignore(item => item.Content);
            modelBuilder.Entity<ContentProgress>().Ignore(item => item.ProgramEnrollment);
            modelBuilder.Entity<Assessment>().Ignore(item => item.AssessmentGroup);
            modelBuilder.Entity<Assessment>().Ignore(item => item.InteractiveVideoCues);
            modelBuilder.Entity<LearningEnrollment>();
            modelBuilder.Entity<Cohort>();
            modelBuilder.Entity<CohortScheduleItem>();
            modelBuilder.Entity<AssessmentGroup>();
            modelBuilder.Entity<AssessmentSubmission>();
            modelBuilder.Entity<Certificate>();
            modelBuilder.Entity<CourseDiscussion>();
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Database.BeginTransactionAsync(cancellationToken);
        }
    }
}
