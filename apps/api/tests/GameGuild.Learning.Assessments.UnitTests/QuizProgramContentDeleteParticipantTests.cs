using FluentAssertions;
using GameGuild.Learning.Assessments.QuizAdapter;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class QuizProgramContentDeleteParticipantTests
{
    [Fact]
    public async Task PrepareDeleteAsync_SoftDeletesTheAssessmentOwnedByTheQuiz()
    {
        await using var context = CreateContext();
        var courseId = Guid.NewGuid();
        var contentId = Guid.NewGuid();
        var assessment = Assessment.Create(
            courseId,
            "Quiz",
            AssessmentType.Quiz,
            ScoreValue.FromPoints("100"),
            contentId: contentId);
        assessment.Version = 1;
        context.Set<Assessment>().Add(assessment);
        await context.SaveChangesAsync();
        var participant = new QuizProgramContentDeleteParticipant(context);
        var content = new ProgramContent
        {
            Id = contentId,
            ProgramId = courseId,
            Title = "Quiz",
            Type = ProgramContentType.Questionnaire,
        };

        await participant.PrepareDeleteAsync(content);
        await context.SaveChangesAsync();

        var persisted = await context.Set<Assessment>()
            .IgnoreQueryFilters()
            .SingleAsync(value => value.Id == assessment.Id);
        persisted.DeletedAt.Should().NotBeNull();
    }

    private static TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"QuizDelete_{Guid.NewGuid()}")
            .Options;
        return new TestContext(options);
    }

    private sealed class TestContext(DbContextOptions<TestContext> options) :
        DbContext(options),
        IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) =>
            new AssessmentsModelConfiguration().Configure(modelBuilder);

        public Task<IDbContextTransaction> BeginTransactionAsync(
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
