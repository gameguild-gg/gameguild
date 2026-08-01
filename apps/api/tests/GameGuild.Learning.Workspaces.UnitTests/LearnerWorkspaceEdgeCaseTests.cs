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

public sealed class LearnerWorkspaceEdgeCaseTests
{
    [Fact]
    public async Task Dashboard_returns_an_empty_read_model_without_current_enrollments()
    {
        await using var context = CreateContext();

        var result = await new GetLearnerDashboardQueryHandler(context)
            .Handle(new GetLearnerDashboardQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().Be(new LearnerDashboardDto([], [], [], [], [], []));
    }

    [Fact]
    public async Task Dashboard_rejects_an_empty_actor_identifier()
    {
        await using var context = CreateContext();

        var action = () => new GetLearnerDashboardQueryHandler(context)
            .Handle(new GetLearnerDashboardQuery(Guid.Empty), CancellationToken.None);

        await action.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Dashboard_maps_fallback_schedule_dates_and_an_ungraded_course()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var course = CreateCourse("fallback", "Fallback course");
        course.Slug = null;
        course.Description = null;
        var enrollment = CreateEnrollment(userId, course.Id);
        var lesson = CreateContent(course.Id, "Lesson");
        var cohort = Cohort.Create(
            course.Id,
            "Morning",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(1),
            20);
        var learningEnrollment = LearningEnrollment.Create(course.Id, userId, cohort.Id);
        var available = CohortScheduleItem.Create(
            cohort.Id,
            lesson.Id,
            null,
            CohortScheduleItemType.ContentRelease,
            null,
            availableFrom: DateTime.UtcNow.AddDays(1),
            status: CohortScheduleItemStatus.Published);
        var due = CohortScheduleItem.Create(
            cohort.Id,
            null,
            Guid.NewGuid(),
            CohortScheduleItemType.AssessmentWindow,
            "Due item",
            dueAt: DateTime.UtcNow.AddDays(2),
            status: CohortScheduleItemStatus.Published);
        var undated = CohortScheduleItem.Create(
            cohort.Id,
            lesson.Id,
            null,
            CohortScheduleItemType.ContentRelease,
            "Undated",
            status: CohortScheduleItemStatus.Published);
        var ungraded = Assessment.Create(course.Id, "Ungraded yet", AssessmentType.Quiz, 10, 7);

        context.AddRange(
            course,
            enrollment,
            lesson,
            cohort,
            learningEnrollment,
            available,
            due,
            undated,
            ungraded);
        await context.SaveChangesAsync();

        var result = await new GetLearnerDashboardQueryHandler(context)
            .Handle(new GetLearnerDashboardQuery(userId), CancellationToken.None);

        result.Courses.Should().ContainSingle(item =>
            item.Slug == course.Id.ToString() && item.Description == string.Empty);
        result.Upcoming.Should().HaveCount(2);
        result.Upcoming.Should().Contain(item =>
            item.ScheduleItemId == available.Id &&
            item.Title == string.Empty &&
            item.CourseSlug == course.Id.ToString());
        result.Upcoming.Should().Contain(item => item.ScheduleItemId == due.Id);
        result.Grades.Should().ContainSingle(item =>
            item.GradedAssessments == 0 &&
            item.EarnedPoints == null &&
            item.PossiblePoints == null &&
            item.Percentage == null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Workspace_returns_null_when_either_identifier_is_empty(bool emptyUser)
    {
        await using var context = CreateContext();
        var query = emptyUser
            ? new GetLearnerCourseWorkspaceQuery(Guid.Empty, Guid.NewGuid())
            : new GetLearnerCourseWorkspaceQuery(Guid.NewGuid(), Guid.Empty);

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(query, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Workspace_returns_null_when_an_enrollment_references_a_missing_course()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        context.Add(CreateEnrollment(userId, courseId));
        await context.SaveChangesAsync();

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(new GetLearnerCourseWorkspaceQuery(userId, courseId), CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Workspace_returns_empty_assessment_context_when_course_has_no_assessments()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var course = CreateCourse("empty-assessments", "No assessments");
        context.AddRange(course, CreateEnrollment(userId, course.Id));
        await context.SaveChangesAsync();

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(new GetLearnerCourseWorkspaceQuery(userId, course.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Assessments.Should().BeEmpty();
        result.Submissions.Should().BeEmpty();
    }

    [Fact]
    public async Task Workspace_maps_cohort_calendar_content_and_submission_details()
    {
        await using var context = CreateContext();
        var userId = Guid.NewGuid();
        var course = CreateCourse("cohort-workspace", "Cohort workspace");
        var enrollment = CreateEnrollment(userId, course.Id);
        var lesson = CreateContent(course.Id, "First lesson");
        lesson.Description = "Lesson description";
        lesson.LessonFormat = null;
        var group = AssessmentGroup.Create(course.Id, "Assignments", 100m);
        var assessment = Assessment.Create(
            course.Id,
            "Practice",
            AssessmentType.Assignment,
            20,
            12,
            assessmentGroupId: group.Id);
        assessment.Update(null, null, null, null, null, null, null, null, null, contentId: lesson.Id);
        var submission = AssessmentSubmission.Start(assessment.Id, enrollment.Id, userId, 1);
        submission.Submit();
        var cohort = Cohort.Create(
            course.Id,
            "Evening",
            DateTime.UtcNow,
            DateTime.UtcNow.AddMonths(2),
            25,
            instructorId: Guid.NewGuid());
        cohort.SetDescription("Evening cohort");
        cohort.SetMeetingSchedule("Weekly");
        var learningEnrollment = LearningEnrollment.Create(course.Id, userId, cohort.Id);
        var schedule = CohortScheduleItem.Create(
            cohort.Id,
            lesson.Id,
            null,
            CohortScheduleItemType.LiveSession,
            "Live lesson",
            startsAt: DateTime.UtcNow.AddDays(1),
            endsAt: DateTime.UtcNow.AddDays(1).AddHours(1),
            status: CohortScheduleItemStatus.Published);

        context.AddRange(
            course,
            enrollment,
            lesson,
            group,
            assessment,
            submission,
            cohort,
            learningEnrollment,
            schedule);
        await context.SaveChangesAsync();

        var result = await new GetLearnerCourseWorkspaceQueryHandler(context)
            .Handle(new GetLearnerCourseWorkspaceQuery(userId, course.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Content.Should().ContainSingle(item =>
            item.ContentId == lesson.Id &&
            item.Description == "Lesson description" &&
            item.LessonFormat == null);
        result.Cohort.Should().NotBeNull().And.Match<LearnerCohortDto>(item =>
            item.CohortId == cohort.Id && item.MeetingSchedule == "Weekly");
        result.Calendar.Should().ContainSingle(item => item.ScheduleItemId == schedule.Id);
        result.Submissions.Should().ContainSingle(item => item.SubmissionId == submission.Id);
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

    private static ProgramEnrollment CreateEnrollment(Guid userId, Guid courseId)
    {
        return new ProgramEnrollment
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramId = courseId,
            EnrollmentStatus = GameGuild.Learning.Courses.EnrollmentStatus.Active,
            CompletionStatus = CompletionStatus.InProgress,
        };
    }

    private static ProgramContent CreateContent(Guid courseId, string title)
    {
        return new ProgramContent
        {
            Id = Guid.NewGuid(),
            ProgramId = courseId,
            Title = title,
            Type = ProgramContentType.Lesson,
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
