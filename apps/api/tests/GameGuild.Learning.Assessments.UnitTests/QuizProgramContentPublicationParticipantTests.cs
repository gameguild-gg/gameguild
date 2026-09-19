using System.Text.Json;
using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Authoring;
using GameGuild.Learning.Assessments.QuizAdapter;
using GameGuild.Identity.Context.Actors;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Moq;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class QuizProgramContentPublicationParticipantTests
{
    [Fact]
    public async Task PreparePublishAsync_CreatesStudentDiscoverableQuizAssessment()
    {
        await using var context = CreateContext();
        var content = QuizContent();
        var participant = CreateParticipant(context);

        await participant.PreparePublishAsync(
            content,
            Payload(content, QuizDocument()),
            Guid.NewGuid());
        await context.SaveChangesAsync();

        var assessment = await context.Set<Assessment>().SingleAsync();
        assessment.ContentId.Should().Be(content.Id);
        assessment.CourseId.Should().Be(content.ProgramId);
        assessment.Type.Should().Be(AssessmentType.Quiz);
        assessment.Title.Should().Be("Published quiz");
        assessment.Slug.Should().Be("published-quiz");
        assessment.Description.Should().Be("Visible to students");
        assessment.MaxScore.Should().Be(ScoreValue.FromUnits(200));
        assessment.SubmissionModalities.Should().Be(SubmissionModality.StructuredAnswer);
        assessment.PresentationMode.Should().Be(AssessmentPresentationMode.Continuous);
        assessment.ReviewMethods.Should().Be(
            ReviewMethods.AutomatedReview | ReviewMethods.InstructorReview);
        assessment.ReviewConfigurationCanonicalJson.Should().Be(
            "{\"instructor\":{\"requireOverrideReason\":false},\"schemaVersion\":1}");
        assessment.TenantId.Should().Be(content.TenantId);
    }

    [Fact]
    public async Task PreparePublishAsync_WithoutGrading_RemovesStaleQuizAssessment()
    {
        await using var context = CreateContext();
        var content = QuizContent();
        var assessment = Assessment.Create(
            content.ProgramId,
            "Old quiz",
            AssessmentType.Quiz,
            ScoreValue.FromUnits(200),
            contentId: content.Id);
        assessment.Version = 1;
        context.Set<Assessment>().Add(assessment);
        await context.SaveChangesAsync();
        var participant = CreateParticipant(context);
        using var document = JsonDocument.Parse("{\"schemaVersion\":1,\"order\":[],\"blocks\":{}}");

        await participant.PreparePublishAsync(
            content,
            Payload(content, document.RootElement.Clone()),
            Guid.NewGuid());
        await context.SaveChangesAsync();

        var persisted = await context.Set<Assessment>()
            .IgnoreQueryFilters()
            .SingleAsync(value => value.Id == assessment.Id);
        persisted.DeletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PreparePublishAsync_UpdatesTheExistingQuizIdentityAndScore()
    {
        await using var context = CreateContext();
        var content = QuizContent();
        var assessment = Assessment.Create(
            content.ProgramId,
            "Old title",
            AssessmentType.Quiz,
            ScoreValue.FromUnits(100),
            contentId: content.Id,
            slug: "old-slug");
        context.Set<Assessment>().Add(assessment);
        await context.SaveChangesAsync();
        var participant = CreateParticipant(context);

        await participant.PreparePublishAsync(
            content,
            Payload(content, QuizDocument()),
            Guid.NewGuid());
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var persisted = await context.Set<Assessment>().SingleAsync();
        persisted.Id.Should().Be(assessment.Id);
        persisted.Title.Should().Be("Published quiz");
        persisted.Slug.Should().Be("published-quiz");
        persisted.MaxScore.Should().Be(ScoreValue.FromUnits(200));
        persisted.TenantId.Should().Be(content.TenantId);
        persisted.ReviewConfigurationCanonicalJson.Should().Be(
            "{\"instructor\":{\"requireOverrideReason\":false},\"schemaVersion\":1}");
    }

    [Fact]
    public async Task FinalizePublishAsync_PreparesAndPublishesTheExecutableRevision()
    {
        await using var context = CreateContext();
        var content = QuizContent();
        var assessment = Assessment.Create(
            content.ProgramId,
            content.Title,
            AssessmentType.Quiz,
            ScoreValue.FromUnits(200),
            contentId: content.Id,
            slug: content.Slug);
        assessment.Version = 3;
        context.Set<Assessment>().Add(assessment);
        await context.SaveChangesAsync();
        var revisionId = Guid.NewGuid();
        var authoring = new Mock<IAssessmentAuthoringService>();
        authoring
            .Setup(service => service.PrepareAsync(
                assessment.Id,
                It.IsAny<Guid>(),
                It.Is<PrepareAssessmentRevisionRequest>(request => request.ExpectedAssessmentVersion == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PreparedAssessmentRevisionResult(
                revisionId,
                1,
                new string('a', 64),
                new string('b', 64))));
        authoring
            .Setup(service => service.PublishAsync(
                assessment.Id,
                It.IsAny<Guid>(),
                It.Is<PublishAssessmentRevisionRequest>(request =>
                    request.RevisionId == revisionId && request.ExpectedAssessmentVersion == 3),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new PreparedAssessmentRevisionResult(
                revisionId,
                1,
                new string('a', 64),
                new string('b', 64))));
        var participant = new QuizProgramContentPublicationParticipant(
            context,
            CreateAdapter(),
            authoring.Object,
            Mock.Of<IActorContextAccessor>());
        var actorId = Guid.NewGuid();

        await participant.FinalizePublishAsync(
            content,
            Payload(content, QuizDocument()),
            actorId);

        authoring.VerifyAll();
    }

    [Fact]
    public async Task PreparePublishAsync_GlobalContent_UsesTheAuthenticatedActorTenant()
    {
        await using var context = CreateContext();
        var content = QuizContent();
        content.TenantId = null;
        var actorId = Guid.NewGuid();
        var tenantId = Guid.NewGuid();
        var actorAccessor = new Mock<IActorContextAccessor>();
        actorAccessor.SetupGet(value => value.ActorContext).Returns(
            ActorContext.Anonymous with
            {
                ActorKind = ActorKind.User,
                SubjectId = actorId.ToString(),
                TenantId = tenantId,
                IsAuthenticated = true,
            });
        var participant = CreateParticipant(context, actorAccessor.Object);

        await participant.PreparePublishAsync(
            content,
            Payload(content, QuizDocument()),
            actorId);
        await context.SaveChangesAsync();

        var assessment = await context.Set<Assessment>().SingleAsync();
        assessment.TenantId.Should().Be(tenantId);
    }

    private static ProgramContent QuizContent() => new()
    {
        Id = Guid.NewGuid(),
        TenantId = Guid.NewGuid(),
        ProgramId = Guid.NewGuid(),
        Title = "Published quiz",
        Slug = "published-quiz",
        Type = ProgramContentType.Questionnaire,
    };

    private static AuthoringContentPayload Payload(ProgramContent content, JsonElement document) => new(
        content.Title,
        content.Slug,
        "Visible to students",
        ProgramContentType.Questionnaire,
        null,
        document,
        null,
        null,
        true,
        null,
        EstimatedMinutesSource.Auto,
        Visibility.Public);

    private static JsonElement QuizDocument()
    {
        using var document = JsonDocument.Parse("""
            {
              "schemaVersion": 1,
              "order": [["q1", "quiz"]],
              "blocks": {
                "q1": {
                  "type": "TRUE_FALSE",
                  "stem": "Question",
                  "points": 200,
                  "correctAnswer": true,
                  "settings": { "allowRetry": false }
                }
              },
              "grading": { "schemaVersion": 2, "items": { "q1": {} } }
            }
            """);
        return document.RootElement.Clone();
    }

    private static QuizAssessmentTypeAdapter CreateAdapter() => new(
        new QuizAuthoringAdapter(new QuizItemProjector()),
        new QuizDeliveryGenerator(),
        new QuizAnswerDecoder(),
        new QuizDeterministicReviewAlgorithm());

    private static QuizProgramContentPublicationParticipant CreateParticipant(
        IApplicationDbContext context,
        IActorContextAccessor? actorContextAccessor = null) => new(
        context,
        CreateAdapter(),
        Mock.Of<IAssessmentAuthoringService>(),
        actorContextAccessor ?? Mock.Of<IActorContextAccessor>());

    private static TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>()
            .UseInMemoryDatabase($"QuizPublish_{Guid.NewGuid()}")
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
