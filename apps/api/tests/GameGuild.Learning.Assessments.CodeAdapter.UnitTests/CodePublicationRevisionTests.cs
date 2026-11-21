using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.Learning.Assessments.CodeAdapter;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Authoring;
using GameGuild.Learning.Assessments.Grading.Capabilities;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Persistence;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Courses;
using GameGuild.Learning.Grading.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GameGuild.Learning.Assessments.CodeAdapter.Tests;

public sealed class CodePublicationRevisionTests
{
    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public async Task PublishedProblemAndSubmittedFiles_RemainBoundToThePersistedRevisionAfterEditing(string family)
    {
        await using var context = new PublicationContext(new DbContextOptionsBuilder<PublicationContext>()
            .UseInMemoryDatabase($"CodingPublication_{Guid.NewGuid()}").Options);
        var services = new ServiceCollection();
        services.AddAssessmentsModule();
        services.AddCodeGradingAdapter();
        using var provider = services.BuildServiceProvider();
        var service = new AssessmentAuthoringService(context,
            provider.GetRequiredService<IAssessmentTypeAdapterResolver>(),
            provider.GetRequiredService<IAssessmentExecutionPolicyResolver>(),
            provider.GetRequiredService<IReviewStageHandlerResolver>(),
            new AIReviewProviderRegistry([]), provider.GetServices<IReviewStageHandler>(), [],
            new AcademicOutboxWriter(context, []), NullLogger<AssessmentAuthoringService>.Instance);
        var content = new ProgramContent
        {
            Id = Guid.NewGuid(), ProgramId = Guid.NewGuid(), TenantId = Guid.NewGuid(), Title = "Exercise",
            Slug = "exercise", Type = ProgramContentType.Code,
        };
        context.Add(content);
        await context.SaveChangesAsync();
        var actor = Guid.NewGuid();
        var first = await SaveAndPublishAsync(context, service, content.Id, actor, CodePublicationMetadataTests.Definition(family));
        var revision = await context.Set<AssessmentDefinitionRevision>().SingleAsync(value => value.Id == first);
        var frozenBytes = revision.ExecutionSnapshotCanonicalJson;
        var frozenHash = revision.ExecutionSnapshotHash;
        var frozen = JsonSerializer.Deserialize<AssessmentExecutionSnapshotV1>(frozenBytes, GradingJson.Options)
            ?? throw new InvalidOperationException("The persisted execution snapshot is required.");
        var adapter = provider.GetRequiredService<CodeAssessmentTypeAdapter>();
        var response = new AssessmentResponseEnvelopeV1(1, "coding-assignment", "code-files", JsonSerializer.SerializeToElement(new
        {
            files = new Dictionary<string, object> { ["/home/user/main.cpp"] = new { content = "// LEARNER-ANSWER", encoding = "text" } },
        }));
        var normalized = adapter.DecodeResponse(response, [frozen.ItemProjections["code"]]);
        var submission = AssessmentSubmission.StartIndividual(content.TenantId, revision.AssessmentId, first, Guid.NewGuid(), Guid.NewGuid(), 1);
        submission.SetPayload(new SubmitAssessmentRequest(CodePayload: normalized.GetRawText()), SubmissionModality.Code);
        submission.Submit();
        context.Add(submission);
        await context.SaveChangesAsync();

        var edited = JsonNode.Parse(CodePublicationMetadataTests.Definition(family).GetRawText())!.AsObject();
        edited["Problem"]!["Statement"] = "A different published problem.";
        var files = edited["Data"]!["Files"]!.AsObject();
        files["/home/user/next.cpp"] = files["/home/user/main.cpp"]!.DeepClone();
        files.Remove("/home/user/main.cpp");
        edited["ValidationSolutions"]![0]!["Files"]![0]!["Path"] = "/home/user/next.cpp";
        var next = await SaveAndPublishAsync(context, service, content.Id, actor, JsonSerializer.SerializeToElement(edited));
        next.Should().NotBe(first);
        context.ChangeTracker.Clear();

        var retainedSubmission = await context.Set<AssessmentSubmission>().SingleAsync();
        retainedSubmission.DefinitionRevisionId.Should().Be(first);
        retainedSubmission.CodePayload.Should().Be(normalized.GetRawText()).And.Contain("LEARNER-ANSWER");
        var retained = await context.Set<AssessmentDefinitionRevision>().SingleAsync(value => value.Id == first);
        retained.ExecutionSnapshotCanonicalJson.Should().Be(frozenBytes);
        retained.ExecutionSnapshotHash.Should().Be(frozenHash);
        var oldDelivery = adapter.GenerateDelivery(frozen.ItemProjections["code"]);
        oldDelivery.GetRawText().Should().NotContain("different published").And.NotContain("SYNTHETIC-");
        oldDelivery.GetProperty("definition").GetProperty("Problem").GetProperty("Statement").GetString()
            .Should().Be(CodePublicationMetadataTests.Definition(family).GetProperty("Problem").GetProperty("Statement").GetString());
        var currentRevision = await context.Set<AssessmentDefinitionRevision>().SingleAsync(value => value.Id == next);
        var current = JsonSerializer.Deserialize<AssessmentExecutionSnapshotV1>(currentRevision.ExecutionSnapshotCanonicalJson, GradingJson.Options)
            ?? throw new InvalidOperationException("The new persisted execution snapshot is required.");
        adapter.GenerateDelivery(current.ItemProjections["code"]).GetRawText().Should().Contain("different published");
        Action decodeWithWrongRevision = () => adapter.DecodeResponse(response, [current.ItemProjections["code"]]);
        decodeWithWrongRevision.Should().Throw<JsonException>();
        adapter.DecodeResponse(response, [frozen.ItemProjections["code"]]).GetRawText().Should().Be(normalized.GetRawText());
        (await context.Set<AssessmentDefinitionRevision>().CountAsync()).Should().Be(2);
    }

    private static async Task<Guid> SaveAndPublishAsync(PublicationContext context, AssessmentAuthoringService service, Guid contentId, Guid actorId, JsonElement document)
    {
        var content = await context.Set<ProgramContent>().SingleAsync(value => value.Id == contentId);
        var assessment = await context.Set<Assessment>().SingleOrDefaultAsync();
        var saved = await service.SaveDraftAsync(content.ProgramId, contentId, actorId,
            new SaveAssessmentDraftRequest(content.Version, assessment?.Version, "Exercise", "exercise", null, document,
                Visibility.Public, true, null, EstimatedMinutesSource.Auto, ReviewMethods.AutomatedReview, ScoreValue.FromUnits(5000)));
        saved.IsSuccess.Should().BeTrue(saved.Error.ToString());
        assessment = await context.Set<Assessment>().SingleAsync();
        var prepared = await service.PrepareAsync(assessment.Id, actorId, new PrepareAssessmentRevisionRequest(assessment.Version));
        prepared.IsSuccess.Should().BeTrue(prepared.Error.ToString());
        var published = await service.PublishAsync(assessment.Id, actorId, new PublishAssessmentRevisionRequest(prepared.Value.RevisionId, assessment.Version));
        published.IsSuccess.Should().BeTrue(published.Error.ToString());
        return published.Value.RevisionId;
    }

    private sealed class PublicationContext(DbContextOptions<PublicationContext> options) : DbContext(options), IApplicationDbContext
    {
        public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Database.BeginTransactionAsync(cancellationToken);
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<EntityBase<Guid>>().Where(entry => entry.State is EntityState.Added or EntityState.Modified))
            {
                entry.Entity.Version++;
            }
            return base.SaveChangesAsync(cancellationToken);
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            new AssessmentsModelConfiguration().Configure(modelBuilder);
            new GradingPersistenceModelConfiguration().Configure(modelBuilder);
            modelBuilder.Entity<ProgramContent>(entity =>
            {
                entity.Ignore(value => value.Program);
                entity.Ignore(value => value.Parent);
                entity.Ignore(value => value.Children);
                entity.Ignore(value => value.ContentInteractions);
            });
        }
    }
}
