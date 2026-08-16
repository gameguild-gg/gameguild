using FluentAssertions;
using GameGuild.Identity.Authorization;
using GameGuild.Identity.Context.Actors;
using GameGuild.Identity.Users;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Enrollments;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

/// <summary>
/// SpeedGrader navigation bundle: instructor-only queue with one item per student/group attempt
/// (InProgress-only entries excluded), canonical Min(Id) group rows, counts, and sort.
/// </summary>
public class GradingQueueTests
{
    [Fact]
    public async Task Individual_Assessment_ListsAttemptsPerStudent_ExcludesInProgressOnly_SortsByNameThenAttempt()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db);
        var (aliceId, aliceRow) = await SeedRowAsync(db, assessment.Id, "Alice", 1, SubmissionStatus.Graded, score: 90);
        var (bobId, bobRow) = await SeedRowAsync(db, assessment.Id, "Bob", 1, SubmissionStatus.Submitted);

        var result = await CreateController(db, instructorId: Guid.NewGuid()).GetGradingQueue(assessment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var queue = ok.Value.Should().BeOfType<GradingQueueDto>().Subject;
        queue.Items.Should().HaveCount(2, "the InProgress-only student is absent");
        queue.Items.Select(i => (i.DisplayName, i.AttemptNumber))
            .Should().ContainInOrder(("Alice", 1), ("Bob", 1));
        var alice = queue.Items[0];
        alice.SubmissionId.Should().Be(aliceRow.Id);
        alice.CanonicalSubmissionId.Should().Be(aliceRow.Id);
        alice.UserId.Should().Be(aliceId);
        alice.Status.Should().Be(SubmissionStatus.Graded);
        alice.Score.Should().Be(90);
        alice.IsLate.Should().BeFalse();
        alice.SubmittedAt.Should().Be(aliceRow.SubmittedAt);
        alice.IsGroup.Should().BeFalse();
        var bob = queue.Items[1];
        bob.SubmissionId.Should().Be(bobRow.Id);
        bob.UserId.Should().Be(bobId);
        bob.Status.Should().Be(SubmissionStatus.Submitted);
        bob.Score.Should().BeNull();
        bob.IsGroup.Should().BeFalse();
    }

    [Fact]
    public async Task Group_Assessment_CollapsesGroupAttemptIntoOneItem_WithCanonicalMinIdAndMemberNames()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db);
        var group = await SeedGroupAsync(db, assessment, "Alice", "Bob", "Carol");
        var rows = new List<AssessmentSubmission>();
        foreach (var (userId, _) in group.Members)
        {
            rows.Add(await SeedGroupRowAsync(
                db, assessment.Id, userId, 1, SubmissionStatus.Submitted, group.GroupId));
        }

        var result = await CreateController(db, instructorId: Guid.NewGuid()).GetGradingQueue(assessment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var queue = ok.Value.Should().BeOfType<GradingQueueDto>().Subject;
        queue.Items.Should().ContainSingle("three member rows of one group attempt collapse into one item");
        var item = queue.Items[0];
        var canonicalId = rows.Min(r => r.Id);
        item.CanonicalSubmissionId.Should().Be(canonicalId);
        item.SubmissionId.Should().Be(canonicalId);
        item.GroupId.Should().Be(group.GroupId);
        item.GroupName.Should().Be(group.GroupName);
        item.MemberNames.Should().BeEquivalentTo(["Alice", "Bob", "Carol"]);
        item.AttemptNumber.Should().Be(1);
        item.Status.Should().Be(SubmissionStatus.Submitted);
        item.IsGroup.Should().BeTrue();
        item.UserId.Should().BeNull();
        queue.Assessment.GroupSetId.Should().Be(group.SetId);
    }

    [Fact]
    public async Task Group_TwoAttempts_ProducesTwoItemsSortedByAttempt()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db);
        var group = await SeedGroupAsync(db, assessment, "Alice", "Bob");
        foreach (var (userId, _) in group.Members)
        {
            await SeedGroupRowAsync(db, assessment.Id, userId, 1, SubmissionStatus.Submitted, group.GroupId);
            await SeedGroupRowAsync(db, assessment.Id, userId, 2, SubmissionStatus.Graded, group.GroupId, score: 88);
        }

        var result = await CreateController(db, instructorId: Guid.NewGuid()).GetGradingQueue(assessment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var queue = ok.Value.Should().BeOfType<GradingQueueDto>().Subject;
        queue.Items.Should().HaveCount(2);
        queue.Items.Select(i => i.AttemptNumber).Should().ContainInOrder(1, 2);
        queue.Items[0].Status.Should().Be(SubmissionStatus.Submitted);
        queue.Items[1].Status.Should().Be(SubmissionStatus.Graded);
        queue.Items[1].Score.Should().Be(88);
    }

    [Fact]
    public async Task NonInstructor_GetsForbidden()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db);
        var student = Guid.NewGuid();
        await SeedRowAsync(db, assessment.Id, "Alice", 1, SubmissionStatus.Submitted);

        var result = await CreateController(db, instructorId: Guid.NewGuid(), actorId: student).GetGradingQueue(assessment.Id);

        result.Result.Should().BeOfType<ForbidResult>();
    }

    [Fact]
    public async Task Counts_TotalAndNeedsGrading_AcrossMixedStatuses()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db);
        await SeedRowAsync(db, assessment.Id, "Alice", 1, SubmissionStatus.Graded, score: 90);
        await SeedRowAsync(db, assessment.Id, "Bob", 1, SubmissionStatus.Submitted);
        await SeedRowAsync(db, assessment.Id, "Carol", 1, SubmissionStatus.Late, isLate: true);
        await SeedRowAsync(db, assessment.Id, "Dave", 1, SubmissionStatus.InProgress);

        var result = await CreateController(db, instructorId: Guid.NewGuid()).GetGradingQueue(assessment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var queue = ok.Value.Should().BeOfType<GradingQueueDto>().Subject;
        queue.Total.Should().Be(3);
        queue.NeedsGrading.Should().Be(2, "Submitted + Late need grading; Graded and InProgress-only do not");
        queue.Items.Single(i => i.DisplayName == "Carol").Status.Should().Be(SubmissionStatus.Late);
        queue.Items.Single(i => i.DisplayName == "Carol").IsLate.Should().BeTrue();
    }

    [Fact]
    public async Task Assessment_Summary_IncludesRubricPayload_GradingMethodsAndPeerReviewPolicy()
    {
        await using var db = CreateContext();
        var assessment = await SeedAssessmentAsync(db,
            gradingMethods: AssessmentGradingMethod.PeerReview | AssessmentGradingMethod.InstructorGraded);
        assessment.SetPeerReviewPolicy(3);
        await SeedRubricAsync(db, assessment);
        await db.SaveChangesAsync();

        var result = await CreateController(db, instructorId: Guid.NewGuid()).GetGradingQueue(assessment.Id);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var queue = ok.Value.Should().BeOfType<GradingQueueDto>().Subject;
        var summary = queue.Assessment;
        summary.Id.Should().Be(assessment.Id);
        summary.Title.Should().Be(assessment.Title);
        summary.Type.Should().Be(AssessmentType.Assignment);
        summary.MaxScore.Should().Be(100);
        summary.GradingMethods.Should().Contain("PeerReview").And.Contain("InstructorGraded");
        summary.PeerReviewsRequiredCount.Should().Be(3);
        summary.HasRubric.Should().BeTrue();
        summary.Rubric.Should().NotBeNull();
        summary.Rubric!.Title.Should().Be("Essay rubric");
        summary.Rubric.Criteria.Should().HaveCount(2);
    }

    // ===== CONTROLLER WIRING (real grading-queue + rubric services over the seeded db) =====

    private readonly Mock<IAssessmentService> _assessments = new();
    private readonly Mock<IActorContextAccessor> _actor = new();
    private readonly Mock<IProgramCrudService> _programs = new();
    private readonly Mock<IEnrollmentService> _enrollments = new();
    private readonly Mock<IPermissionQueryService> _permissions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AssessmentsController>> _log = new();

    private AssessmentsController CreateController(
        TestGradingQueueDbContext db, Guid instructorId, Guid? actorId = null)
    {
        var userId = actorId ?? instructorId;
        _actor.Setup(a => a.ActorContext).Returns(new ActorContext
        {
            ActorKind = ActorKind.User,
            SubjectId = userId.ToString(),
            TenantId = Guid.NewGuid(),
            IsAuthenticated = true,
            Roles = new HashSet<string>(),
            Permissions = new HashSet<string>()
        });
        _assessments.Setup(s => s.GetAssessmentByIdAsync(It.IsAny<Guid>()))
            .Returns(async (Guid id) => await db.Set<Assessment>()
                .FirstOrDefaultAsync(a => a.Id == id && a.DeletedAt == null));
        _programs.Setup(p => p.GetProgramByIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync((Guid id) => new Program { Id = id, CreatorId = instructorId, TenantId = null });
        return new AssessmentsController(
            _assessments.Object,
            _actor.Object,
            _programs.Object,
            _enrollments.Object,
            _permissions.Object,
            new GradingQueueService(
                db,
                new RubricService(db, NullLogger<RubricService>.Instance),
                NullLogger<GradingQueueService>.Instance),
            _log.Object);
    }

    // ===== FIXTURE =====

    private static async Task<Assessment> SeedAssessmentAsync(
        TestGradingQueueDbContext db,
        AssessmentGradingMethod gradingMethods = AssessmentGradingMethod.InstructorGraded)
    {
        var assessment = Assessment.Create(
            Guid.NewGuid(), "Essay", AssessmentType.Assignment, 100,
            gradingMethods: gradingMethods);
        db.Add(assessment);
        await db.SaveChangesAsync();
        return assessment;
    }

    private sealed record QueueGroupFixture(
        Guid SetId,
        Guid GroupId,
        string GroupName,
        (Guid UserId, string Name)[] Members);

    private static async Task<QueueGroupFixture> SeedGroupAsync(
        TestGradingQueueDbContext db, Assessment assessment, params string[] memberNames)
    {
        var set = CourseGroupSet.Create(assessment.CourseId, "Project Groups");
        var group = CourseGroup.Create(set.Id, "Team A", Math.Max(2, memberNames.Length));
        assessment.AssignToGroupSet(set.Id);
        db.AddRange(set, group);
        var members = memberNames.Select(name =>
        {
            var user = Guid.NewGuid();
            db.AddRange(
                new User { Id = user, Name = name, Email = $"{name.ToLowerInvariant()}@example.com" },
                CourseGroupMember.Create(group.Id, user));
            return (user, name);
        }).ToArray();
        await db.SaveChangesAsync();
        return new QueueGroupFixture(set.Id, group.Id, group.Name, members);
    }

    private static async Task<(Guid UserId, AssessmentSubmission Row)> SeedRowAsync(
        TestGradingQueueDbContext db,
        Guid assessmentId,
        string displayName,
        int attempt,
        SubmissionStatus status,
        int? score = null,
        bool isLate = false)
    {
        var userId = Guid.NewGuid();
        db.Add(new User { Id = userId, Name = displayName, Email = $"{displayName.ToLowerInvariant()}@example.com" });
        var row = await BuildRowAsync(db, assessmentId, userId, attempt, status, null, score, isLate);
        return (userId, row);
    }

    private static async Task<AssessmentSubmission> SeedGroupRowAsync(
        TestGradingQueueDbContext db,
        Guid assessmentId,
        Guid userId,
        int attempt,
        SubmissionStatus status,
        Guid groupId,
        int? score = null,
        bool isLate = false) =>
        await BuildRowAsync(db, assessmentId, userId, attempt, status, groupId, score, isLate);

    private static async Task<AssessmentSubmission> BuildRowAsync(
        TestGradingQueueDbContext db,
        Guid assessmentId,
        Guid userId,
        int attempt,
        SubmissionStatus status,
        Guid? groupId,
        int? score,
        bool isLate)
    {
        var row = AssessmentSubmission.Start(assessmentId, Guid.NewGuid(), userId, attempt);
        if (status != SubmissionStatus.InProgress)
        {
            row.SetPayload(new SubmitAssessmentRequest(TextPayload: "work"), SubmissionModality.Text);
            row.Submit(isLate);
            if (status == SubmissionStatus.Graded)
            {
                row.Grade(score ?? 0, 60, 100, Guid.NewGuid(), "graded");
            }
        }

        if (groupId.HasValue)
        {
            row.StampCourseGroup(groupId.Value);
        }

        db.Add(row);
        await db.SaveChangesAsync();
        return row;
    }

    private static async Task SeedRubricAsync(TestGradingQueueDbContext db, Assessment assessment)
    {
        var rubric = AssessmentRubric.Create("Essay rubric");
        db.Add(rubric);
        db.AddRange(
            RubricCriterion.Create(rubric.Id, "Thesis", 60, 1),
            RubricCriterion.Create(rubric.Id, "Mechanics", 40, 2));
        assessment.AssignRubric(rubric.Id);
        db.Update(assessment);
    }

    private static TestGradingQueueDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestGradingQueueDbContext>()
            .UseInMemoryDatabase($"GradingQueue_{Guid.NewGuid()}")
            .Options;
        return new TestGradingQueueDbContext(options);
    }

    private sealed class TestGradingQueueDbContext(DbContextOptions<TestGradingQueueDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
            // ponytail: minimal cross-module mapping for display names; full mapping lives in ApplicationDbContext.
            modelBuilder.Entity<User>(b =>
            {
                b.HasKey(u => u.Id);
                b.Ignore(u => u.Profile);
                b.Ignore(u => u.Metadata);
                b.Ignore(u => u.Preferences);
                b.Ignore(u => u.Notifications);
                b.Ignore(u => u.TenantMemberships);
            });
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not required for grading queue tests.");
        }
    }
}
