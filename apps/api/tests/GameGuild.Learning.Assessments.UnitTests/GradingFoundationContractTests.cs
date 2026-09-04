using System.Text.Json;
using FluentAssertions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using Xunit;

namespace GameGuild.Learning.Assessments.Tests;

public sealed class GradingFoundationContractTests
{
    [Fact]
    public void AcademicValues_AreSerializedAsCanonicalJsonStrings()
    {
        JsonSerializer.Serialize(ScoreValue.Parse("00000012.3400")).Should().Be("\"00000012.3400\"");
        JsonSerializer.Serialize(PercentValue.Parse("075.5000")).Should().Be("\"075.5000\"");

        Action numericScore = () => JsonSerializer.Deserialize<ScoreValue>("12.34");
        Action numericPercent = () => JsonSerializer.Deserialize<PercentValue>("75.5");

        numericScore.Should().Throw<JsonException>();
        numericPercent.Should().Throw<JsonException>();
    }

    [Fact]
    public void WireEnums_UseTheTypeScriptDiscriminators()
    {
        JsonSerializer.Serialize(ReviewMethod.AutomatedReview, GradingJson.Options)
            .Should().Be("\"AutomatedReview\"");
        JsonSerializer.Serialize(ReviewExecutionContext.OfficialSubmission, GradingJson.Options)
            .Should().Be("\"official-submission\"");
        JsonSerializer.Serialize(GradeItemState.Pending, GradingJson.Options)
            .Should().Be("\"pending\"");
    }

    [Fact]
    public void ScoreRatio_UsesExactMidpointRoundingOnce()
    {
        ScoreValue.ByRatio(ScoreValue.Parse("00000001.0000"), 1, 6)
            .Should().Be(ScoreValue.Parse("00000000.1667"));
    }

    [Fact]
    public void ReviewMethods_AcceptOnlyCanonicalWorkflowsAcrossAllFiveBits()
    {
        var valid = new HashSet<int> { 0, 1, 2, 4, 8, 9, 10, 12, 16, 24 };

        for (var mask = 0; mask <= 31; mask++)
        {
            Action action = () => ((ReviewMethods)mask).EnsureValid(allowDraft: true);
            if (valid.Contains(mask)) action.Should().NotThrow();
            else action.Should().Throw<ArgumentOutOfRangeException>();
        }
    }

    [Theory]
    [InlineData(9, ReviewMethod.PeerReview)]
    [InlineData(10, ReviewMethod.AIReview)]
    [InlineData(12, ReviewMethod.AutomatedReview)]
    [InlineData(24, ReviewMethod.SelfReview)]
    public void InstructorReview_IsAlwaysTheFinalStage(int mask, ReviewMethod first)
    {
        ((ReviewMethods)mask).ToSequence().Should().Equal(first, ReviewMethod.InstructorReview);
    }

    [Fact]
    public void CanonicalJson_MatchesTheTypeScriptByteContract()
    {
        using var document = JsonDocument.Parse("""
            {"z":1,"a":{"y":2,"b":3}}
            """);

        CanonicalJson.Serialize(document.RootElement)
            .Should().Be("{\"a\":{\"b\":3,\"y\":2},\"z\":1}");
        CanonicalJson.Sha256(document.RootElement)
            .Should().Be("10d6b907e50339871355376854e16e87112120f63b9ce9bca2913907cd2a124d");
    }

    [Fact]
    public void ExecutionSnapshotValidator_BindsAuthoringManifestStagesAndProjections()
    {
        var snapshot = CreateSnapshot();

        var valid = () => GradingContractValidator.Validate(snapshot);
        valid.Should().NotThrow();

        var mismatched = snapshot with
        {
            AuthoringSource = snapshot.AuthoringSource with
            {
                Grading = new ContentGradingDefinitionV2(
                    2,
                    new Dictionary<string, GradingItemAuthoringV2> { ["q2"] = new() }),
            },
        };
        var invalid = () => GradingContractValidator.Validate(mismatched);
        invalid.Should().Throw<JsonException>().WithMessage("*same item IDs*");
    }

    [Fact]
    public void ExecutionBindings_UseThePersistedSnapshotHashAndManifestGeneratorVersion()
    {
        var snapshot = CreateSnapshot();
        using var learnerPayload = JsonDocument.Parse("{}");
        using var responsePayload = JsonDocument.Parse("{\"answers\":{}}");
        const string snapshotHash = "0000000000000000000000000000000000000000000000000000000000000000";
        var delivery = new AssessmentExecutionDeliveryV1(
            1,
            Guid.NewGuid(),
            snapshotHash,
            ["q1"],
            new Dictionary<string, AssessmentExecutionDeliveryItemV1>
            {
                ["q1"] = new(
                    "quiz-delivery-generator",
                    "1",
                    learnerPayload.RootElement.Clone()),
            });
        var response = new AssessmentResponseEnvelopeV1(
            1,
            "quiz",
            "quiz-answer/v1",
            responsePayload.RootElement.Clone());

        var valid = () => GradingContractValidator.ValidateBindings(snapshotHash, snapshot, delivery, response);
        valid.Should().NotThrow();

        var wrongHash = () => GradingContractValidator.ValidateBindings(
            "1111111111111111111111111111111111111111111111111111111111111111",
            snapshot,
            delivery,
            response);
        wrongHash.Should().Throw<JsonException>().WithMessage("*grading execution snapshot hash*");

        var wrongGenerator = delivery with
        {
            Items = new Dictionary<string, AssessmentExecutionDeliveryItemV1>
            {
                ["q1"] = delivery.Items["q1"] with { DeliveryGeneratorVersion = "2" },
            },
        };
        var invalid = () => GradingContractValidator.ValidateBindings(snapshotHash, snapshot, wrongGenerator, response);
        invalid.Should().Throw<JsonException>().WithMessage("*generator must match*");
    }

    [Fact]
    public void ExecutionPolicy_RequiresTheCanonicalUtcWireRepresentation()
    {
        var snapshot = CreateSnapshot();
        var policy = snapshot.AuthoringSource.Policy;
        var canonical = policy with
        {
            Availability = policy.Availability with { AvailableFrom = "2026-09-04T12:30:00.000Z" },
        };
        var offset = policy with
        {
            Availability = policy.Availability with { AvailableFrom = "2026-09-04T12:30:00.000+00:00" },
        };

        var valid = () => GradingContractValidator.Validate(canonical);
        var invalid = () => GradingContractValidator.Validate(offset);

        valid.Should().NotThrow();
        invalid.Should().Throw<JsonException>().WithMessage("*canonical UTC instant*");
    }

    private static AssessmentExecutionSnapshotV1 CreateSnapshot() =>
        JsonSerializer.Deserialize<AssessmentExecutionSnapshotV1>("""
            {
              "schemaVersion":1,
              "authoringSource":{
                "schemaVersion":1,
                "contentType":"quiz",
                "content":{},
                "grading":{"schemaVersion":2,"items":{"q1":{}}},
                "policy":{
                  "schemaVersion":1,
                  "maxAttempts":1,
                  "availability":{"allowLateSubmissions":false},
                  "completion":{"mode":"on-release"},
                  "resultRelease":{"mode":"manual"},
                  "presentation":{"mode":"continuous"},
                  "review":{"schemaVersion":1,"methods":12,"instructor":{"requireOverrideReason":true}}
                }
              },
              "manifest":{
                "schemaVersion":1,
                "items":[{
                  "itemId":"q1",
                  "itemType":"TRUE_FALSE",
                  "projectorKey":"quiz-item-projector",
                  "projectorVersion":"1",
                  "deliveryGeneratorKey":"quiz-delivery-generator",
                  "deliveryGeneratorVersion":"1",
                  "answerDecoderKey":"quiz-answer-decoder",
                  "answerDecoderVersion":"1"
                }],
                "stages":[
                  {"method":"AutomatedReview","handlerKey":"quiz-automated-review","handlerVersion":"1","algorithmKey":"quiz-deterministic","algorithmVersion":"1"},
                  {"method":"InstructorReview","handlerKey":"instructor-review","handlerVersion":"1"}
                ],
                "policies":[]
              },
              "itemProjections":{
                "q1":{"schemaVersion":1,"itemId":"q1","itemType":"TRUE_FALSE","maxScore":"00000001.0000","source":{"contentType":"quiz","itemId":"q1"}}
              }
            }
            """, GradingJson.Options)!;
}
