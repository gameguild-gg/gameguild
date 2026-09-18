using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Persistence;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentGradebookProjectionTests
{
    [Fact]
    public async Task Projection_AggregatesPointsThenWeightsGroupsWithoutRenormalizingOrRoundingTwice()
    {
        await using var context = ProjectionTestContext.Create();
        var service = new AssessmentGradebookProjectionService(
            context,
            new AcademicOutboxWriter(context, []));
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var firstGroupId = Guid.NewGuid();
        var secondGroupId = Guid.NewGuid();

        context.Set<AssessmentGradebookEntry>().AddRange(
            Entry(courseId, enrollmentId, firstGroupId, scoreUnits: 1, maxScoreUnits: 3, weightUnits: 5003),
            Entry(courseId, enrollmentId, secondGroupId, scoreUnits: 1, maxScoreUnits: 2, weightUnits: 3000));
        await context.SaveChangesAsync();

        var projection = await service.GetCourseProjectionAsync(courseId, enrollmentId, learnerView: false);

        projection.LearnerVisible.Should().BeTrue();
        projection.CoursePercentUnits.Should().Be(3168);
        projection.Groups.Should().HaveCount(2);
        var first = projection.Groups.Single(value => value.AssessmentGroupId == firstGroupId);
        first.GroupRatio.Units.Should().Be(3333);
        first.ContributionPercent.Units.Should().Be(1668,
            "the weighted contribution is rounded once from the exact 1/3 ratio");
        var second = projection.Groups.Single(value => value.AssessmentGroupId == secondGroupId);
        second.GroupRatio.Units.Should().Be(5000);
        second.ContributionPercent.Units.Should().Be(1500);
    }

    [Fact]
    public async Task Projection_UsesPointTotalsForDifferentMaximumScoresAndKeepsZeroWeightAsPractice()
    {
        await using var context = ProjectionTestContext.Create();
        var service = new AssessmentGradebookProjectionService(
            context,
            new AcademicOutboxWriter(context, []));
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var gradedGroupId = Guid.NewGuid();
        var practiceGroupId = Guid.NewGuid();

        context.Set<AssessmentGradebookEntry>().AddRange(
            Entry(courseId, enrollmentId, gradedGroupId, scoreUnits: 100, maxScoreUnits: 100, weightUnits: 4000),
            Entry(courseId, enrollmentId, gradedGroupId, scoreUnits: 0, maxScoreUnits: 300, weightUnits: 4000),
            Entry(courseId, enrollmentId, practiceGroupId, scoreUnits: 100, maxScoreUnits: 100, weightUnits: 0));
        await context.SaveChangesAsync();

        var projection = await service.GetCourseProjectionAsync(courseId, enrollmentId, learnerView: false);

        projection.CoursePercentUnits.Should().Be(1000);
        var graded = projection.Groups.Single(value => value.AssessmentGroupId == gradedGroupId);
        graded.GroupRatio.Units.Should().Be(2500);
        graded.ContributionPercent.Units.Should().Be(1000);
        var practice = projection.Groups.Single(value => value.AssessmentGroupId == practiceGroupId);
        practice.GroupRatio.Units.Should().Be(10000);
        practice.ContributionPercent.Units.Should().Be(0);
    }

    [Fact]
    public async Task ReprojectGroupWeight_UpdatesExistingPlacementOnceAndAuditsBeforeAndAfter()
    {
        await using var context = ProjectionTestContext.Create();
        var service = new AssessmentGradebookProjectionService(
            context,
            new AcademicOutboxWriter(context, []));
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var group = AssessmentGroup.Create(courseId, "Exams", PercentValue.FromUnits(5_000));
        group.TenantId = tenantId;
        var assessment = Assessment.Create(
            courseId,
            "Exam",
            AssessmentType.Quiz,
            ScoreValue.FromUnits(1_000),
            assessmentGroupId: group.Id);
        assessment.TenantId = tenantId;
        var entry = AssessmentGradebookEntry.Create(
            tenantId,
            courseId,
            enrollmentId,
            assessment.Id,
            group.Id,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ScoreValue.FromUnits(750),
            ScoreValue.FromUnits(1_000),
            group.WeightPercent);
        context.AddRange(group, assessment, entry);
        await context.SaveChangesAsync();

        var previousWeight = group.WeightPercent;
        group.Update(null, null, PercentValue.FromUnits(2_500), null);
        await service.ReprojectGroupWeightAsync(group.Id, actorId, previousWeight);
        await context.SaveChangesAsync();

        var persisted = await context.Set<AssessmentGradebookEntry>().SingleAsync();
        persisted.Id.Should().Be(entry.Id);
        persisted.GradeRoundId.Should().Be(entry.GradeRoundId);
        persisted.CapturedWeightPercent.Should().Be(PercentValue.FromUnits(2_500));
        (await context.Set<AssessmentGradebookEntry>().CountAsync()).Should().Be(1);
        var audit = await context.Set<AcademicOutboxMessage>().SingleAsync();
        audit.EventType.Should().Be("gradebook-placement-reprojected");
        audit.PayloadCanonicalJson.Should().Contain(actorId.ToString());
        audit.PayloadCanonicalJson.Should().Contain("5000");
        audit.PayloadCanonicalJson.Should().Contain("2500");
    }

    [Fact]
    public async Task ReprojectAssessmentPlacement_MovesThenRemovesEntryWithoutCreatingANewResult()
    {
        await using var context = ProjectionTestContext.Create();
        var service = new AssessmentGradebookProjectionService(
            context,
            new AcademicOutboxWriter(context, []));
        var tenantId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var courseId = Guid.NewGuid();
        var enrollmentId = Guid.NewGuid();
        var originalGroup = AssessmentGroup.Create(courseId, "Original", PercentValue.FromUnits(4_000));
        var nextGroup = AssessmentGroup.Create(courseId, "Next", PercentValue.FromUnits(6_000));
        originalGroup.TenantId = tenantId;
        nextGroup.TenantId = tenantId;
        var assessment = Assessment.Create(
            courseId,
            "Project",
            AssessmentType.Project,
            ScoreValue.FromUnits(2_000),
            assessmentGroupId: originalGroup.Id);
        assessment.TenantId = tenantId;
        var submissionId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        var entry = AssessmentGradebookEntry.Create(
            tenantId,
            courseId,
            enrollmentId,
            assessment.Id,
            originalGroup.Id,
            submissionId,
            roundId,
            ScoreValue.FromUnits(1_500),
            ScoreValue.FromUnits(2_000),
            originalGroup.WeightPercent);
        context.AddRange(originalGroup, nextGroup, assessment, entry);
        await context.SaveChangesAsync();

        assessment.AssignToGroup(nextGroup.Id);
        await service.ReprojectAssessmentPlacementAsync(
            assessment.Id,
            actorId,
            originalGroup.Id,
            originalGroup.WeightPercent);
        await context.SaveChangesAsync();

        var moved = await context.Set<AssessmentGradebookEntry>().SingleAsync();
        moved.Id.Should().Be(entry.Id);
        moved.AssessmentGroupId.Should().Be(nextGroup.Id);
        moved.CapturedWeightPercent.Should().Be(nextGroup.WeightPercent);
        moved.SubmissionId.Should().Be(submissionId);
        moved.GradeRoundId.Should().Be(roundId);

        assessment.AssignToGroup(null);
        await service.ReprojectAssessmentPlacementAsync(
            assessment.Id,
            actorId,
            nextGroup.Id,
            nextGroup.WeightPercent);
        await context.SaveChangesAsync();

        (await context.Set<AssessmentGradebookEntry>().CountAsync()).Should().Be(0);
        (await context.Set<AcademicOutboxMessage>().CountAsync(value =>
            value.EventType == "gradebook-placement-reprojected")).Should().Be(2);
        (await context.Set<GradeRound>().CountAsync()).Should().Be(0);
        (await context.Set<ReviewEvidence>().CountAsync()).Should().Be(0);
    }

    private static AssessmentGradebookEntry Entry(
        Guid courseId,
        Guid enrollmentId,
        Guid groupId,
        int scoreUnits,
        int maxScoreUnits,
        int weightUnits) =>
        AssessmentGradebookEntry.Create(
            tenantId: Guid.NewGuid(),
            courseId,
            enrollmentId,
            assessmentId: Guid.NewGuid(),
            groupId,
            submissionId: Guid.NewGuid(),
            roundId: Guid.NewGuid(),
            ScoreValue.FromUnits(scoreUnits),
            ScoreValue.FromUnits(maxScoreUnits),
            PercentValue.FromUnits(weightUnits));

    private sealed class ProjectionTestContext(DbContextOptions<ProjectionTestContext> options)
        : DbContext(options), IApplicationDbContext
    {
        public static ProjectionTestContext Create() => new(
            new DbContextOptionsBuilder<ProjectionTestContext>()
                .UseInMemoryDatabase($"AssessmentGradebookProjection_{Guid.NewGuid():N}")
                .Options);

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            Database.BeginTransactionAsync(cancellationToken);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
            new GradingPersistenceModelConfiguration().Configure(modelBuilder);
        }
    }
}
