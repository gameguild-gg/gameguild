using FluentAssertions;
using GameGuild.Learning.Courses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class AssessmentDefinitionContractTests
{
    private const string QuizDefinition = """
    {
      "order": [["1", "quiz"]],
      "blocks": {
        "1": {
          "type": "SINGLE_CHOICE",
          "stem": "2 + 2?",
          "points": 2,
          "options": [
            { "id": "a", "text": "4" },
            { "id": "b", "text": "5" }
          ],
          "correctOptionId": "a",
          "settings": { "allowRetry": true, "showFeedback": true, "showCorrectAnswer": true }
        }
      }
    }
    """;

    [Fact]
    public void SetDefinition_WhenPayloadIsInvalidJson_Throws()
    {
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 10, 6);

        var action = () => assessment.SetDefinition("{not-json", 1);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Assessment definition must be valid JSON.*");
    }

    [Fact]
    public void LearnerDefinition_RedactsQuizAnswerKeys()
    {
        var learnerDefinition = AssessmentDefinitionContract.LearnerDefinition(
            QuizDefinition,
            Guid.Parse("0c7aaf53-2d09-4fb9-9174-5b58f7c8d66a"));

        var quiz = learnerDefinition.GetProperty("blocks").GetProperty("1");

        quiz.TryGetProperty("correctOptionId", out _).Should().BeFalse();
        quiz.GetProperty("options").EnumerateArray().Select(option => option.GetProperty("id").GetString())
            .Should().BeEquivalentTo(["a", "b"]);
    }

    [Fact]
    public void TryGradeDeterministicQuiz_WhenAnswerMatchesServerDefinition_ReturnsFullScore()
    {
        const string submittedPayload = """
        {
          "answers": {
            "1": {
              "selectedOptionIds": ["a"],
              "score": 0,
              "isCorrect": false
            }
          },
          "score": 0
        }
        """;

        var graded = AssessmentDefinitionContract.TryGradeDeterministicQuiz(
            QuizDefinition,
            submittedPayload,
            maxScore: 10,
            out var score,
            out _);

        graded.Should().BeTrue();
        score.Should().Be(10);
    }

    [Fact]
    public void TryGradeDeterministicQuiz_WhenPayloadClaimsCorrectnessButAnswerIsWrong_UsesServerDefinition()
    {
        const string submittedPayload = """
        {
          "answers": {
            "1": {
              "selectedOptionIds": ["b"],
              "correctOptionId": "b",
              "score": 100,
              "isCorrect": true
            }
          },
          "score": 100
        }
        """;

        var graded = AssessmentDefinitionContract.TryGradeDeterministicQuiz(
            QuizDefinition,
            submittedPayload,
            maxScore: 10,
            out var score,
            out _);

        graded.Should().BeTrue();
        score.Should().Be(0);
    }

    [Fact]
    public async Task SubmitAsync_WhenQuizHasStructuredAnswer_AutoGradesFromServerDefinition()
    {
        await using var db = CreateContext();
        var assessment = Assessment.Create(Guid.NewGuid(), "Quiz", AssessmentType.Quiz, 10, 6);
        assessment.SetDeliveryContract(SubmissionModality.StructuredAnswer, AssessmentPresentationMode.Continuous);
        assessment.SetDefinition(QuizDefinition, 1);
        var submission = AssessmentSubmission.Start(assessment.Id, Guid.NewGuid(), Guid.NewGuid(), 1);
        db.AddRange(assessment, submission);
        await db.SaveChangesAsync();
        var service = new AssessmentService(db, Mock.Of<IProgramContentService>(), NullLogger<AssessmentService>.Instance);

        var result = await service.SubmitAsync(
            submission.Id,
            new SubmitAssessmentRequest(StructuredAnswerPayload: """{"answers":{"1":{"selectedOptionIds":["a"]}}}"""));

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(SubmissionStatus.Graded);
        result.Value.Score.Should().Be(10);
        result.Value.Passed.Should().BeTrue();
    }

    private static TestAssessmentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestAssessmentDbContext>()
            .UseInMemoryDatabase($"AssessmentDefinition_{Guid.NewGuid()}")
            .Options;
        return new TestAssessmentDbContext(options);
    }

    private sealed class TestAssessmentDbContext(DbContextOptions<TestAssessmentDbContext> options)
        : DbContext(options), IApplicationDbContext
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
        }

        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException("Transactions are not required for assessment definition tests.");
        }
    }
}
