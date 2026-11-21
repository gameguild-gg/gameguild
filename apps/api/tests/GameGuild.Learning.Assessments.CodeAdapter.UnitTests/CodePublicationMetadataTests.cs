using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using GameGuild.Learning.Assessments.CodeAdapter;
using GameGuild.Learning.Assessments.Grading.Abstractions;
using GameGuild.Learning.Assessments.Grading.Contracts;
using GameGuild.Learning.Assessments.Grading.Runtime;
using GameGuild.Learning.Courses;
using Xunit;

namespace GameGuild.Learning.Assessments.CodeAdapter.Tests;

public sealed class CodePublicationMetadataTests
{
    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public void SharedWireContract_RoundTripsProblemReferenceAndStableIdentities(string family)
    {
        var source = Definition(family);
        var model = Read(source);
        new CodingAssignmentContentValidator().Validate(model).IsValid.Should().BeTrue();
        var roundtrip = JsonSerializer.SerializeToElement(model, CodeAssessmentContracts.ContentJson);
        CanonicalJson.Serialize(roundtrip).Should().Be(CanonicalJson.Serialize(source));
    }

    [Fact]
    public void RetainedDefinition_HasTheSameCanonicalBytesWithoutFabricatedFields()
    {
        var source = Definition("legacy");
        var roundtrip = JsonSerializer.SerializeToElement(Read(source), CodeAssessmentContracts.ContentJson);
        CanonicalJson.Serialize(roundtrip).Should().Be(CanonicalJson.Serialize(source));
        roundtrip.TryGetProperty("Problem", out _).Should().BeFalse();
        roundtrip.TryGetProperty("ValidationSolutions", out _).Should().BeFalse();
        roundtrip.GetProperty("Data").GetProperty("Files").GetProperty("main.cpp").TryGetProperty("Id", out _).Should().BeFalse();
        roundtrip.GetProperty("Tests").GetProperty("Public")[0].TryGetProperty("Id", out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public void FrozenDelivery_IncludesProblemButNoNestedPrivateCanary(string family)
    {
        var adapter = Adapter();
        var item = adapter.ProjectAuthoring(Definition(family)).Items.Single().PrivateProjection;
        item.GetRawText().Should().Contain("SYNTHETIC-TEACHER-REFERENCE");
        var delivery = adapter.GenerateDelivery(item).GetProperty("definition");
        delivery.GetProperty("Problem").GetProperty("Statement").GetString().Should().Be(Read(Definition(family)).Problem!.Statement);
        delivery.TryGetProperty("ValidationSolutions", out _).Should().BeFalse();
        delivery.GetRawText().Should().NotContain("SYNTHETIC-");
        delivery.GetProperty("Data").GetProperty("Files").GetProperty("/home/user/main.cpp").GetProperty("Id").GetString().Should().Be("main-file");
    }

    [Theory]
    [InlineData("blank-problem")]
    [InlineData("duplicate-example")]
    [InlineData("bad-role")]
    [InlineData("duplicate-solution")]
    [InlineData("empty-replacement")]
    [InlineData("unknown-file")]
    [InlineData("wrong-path")]
    [InlineData("duplicate-test")]
    [InlineData("duplicate-file")]
    [InlineData("mixed-family")]
    public void Publication_RejectsMalformedMetadataWithoutEchoingProtectedContent(string mutation)
    {
        var source = JsonNode.Parse(Definition("standard-io").GetRawText())!.AsObject();
        var solutions = source["ValidationSolutions"]!.AsArray();
        var replacement = solutions[0]!["Files"]!.AsArray();
        if (mutation == "blank-problem") source["Problem"]!["Statement"] = " ";
        if (mutation == "duplicate-example") source["Problem"]!["Examples"]!.AsArray().Add(source["Problem"]!["Examples"]![0]!.DeepClone());
        if (mutation == "bad-role") solutions[0]!["Role"] = "grader";
        if (mutation == "duplicate-solution") solutions.Add(solutions[0]!.DeepClone());
        if (mutation == "empty-replacement") replacement.Clear();
        if (mutation == "unknown-file") replacement[0]!["FileId"] = "not-authored";
        if (mutation == "wrong-path") replacement[0]!["Path"] = "/home/user/other.cpp";
        if (mutation == "duplicate-test") source["Tests"]!["Private"]![0]!["Id"] = "echo-public";
        if (mutation == "duplicate-file") source["Data"]!["Files"]!["/home/user/hidden.h"]!["Id"] = "main-file";
        if (mutation == "mixed-family") source["Tests"]!["Public"]!.AsArray().Add(
            JsonNode.Parse(Definition("function").GetRawText())!["Tests"]!["Public"]![0]!.DeepClone());
        Action publish = () => Adapter().ProjectAuthoring(JsonSerializer.SerializeToElement(source));
        publish.Should().Throw<JsonException>().Which.Message.Should().NotContain("SYNTHETIC-TEACHER-REFERENCE");
    }

    [Fact]
    public void DomainValidator_BoundsTheWholeProblemEvenWithoutReferenceSolutions()
    {
        var definition = Read(Definition("standard-io")) with
        {
            ValidationSolutions = null,
            Problem = new CodingProblem
            {
                Statement = "Large problem", Constraints = [],
                Examples = Enumerable.Range(0, 6).Select(index => new CodingProblemExample
                {
                    Id = $"example-{index}", Input = new string('x', 1_800_000), Output = "", Explanation = "",
                }).ToArray(),
            },
        };
        new CodingAssignmentContentValidator().Validate(definition).IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task OfficialExecution_NeverReceivesTheTeacherReferenceReplacement()
    {
        var executor = new CapturingExecutor();
        var adapter = new CodeAssessmentTypeAdapter(executor);
        var projection = adapter.ProjectAuthoring(Definition("standard-io")).Items.Single().PrivateProjection;
        var response = adapter.DecodeResponse(new AssessmentResponseEnvelopeV1(1, "coding-assignment", "code-files",
            JsonSerializer.SerializeToElement(new { files = new Dictionary<string, object>
            {
                ["/home/user/main.cpp"] = new { content = "// LEARNER-ANSWER", encoding = "text" },
            } })), [projection]);
        await adapter.EvaluateDeterministicAsync(new DeterministicReviewRequest([projection],
            new AssessmentExecutionDeliveryV1(1, Guid.NewGuid(), new string('a', 64), [], new Dictionary<string, AssessmentExecutionDeliveryItemV1>()),
            response, CodeAssessmentContracts.HandlerKey, CodeAssessmentContracts.Version), CancellationToken.None);
        executor.Definition.Should().NotBeNull();
        executor.Definition!.ValidationSolutions.Should().BeNull();
        executor.Definition.Data.Files["/home/user/main.cpp"].Content.Should().Be("int main(){}");
        executor.Files.GetRawText().Should().Contain("LEARNER-ANSWER").And.NotContain("SYNTHETIC-TEACHER-REFERENCE");
    }

    internal static JsonElement Definition(string family)
    {
        using var stream = typeof(CodePublicationMetadataTests).Assembly.GetManifestResourceStream(
            "GameGuild.Coding.Fixtures.coding-publication-cases.json") ?? throw new InvalidOperationException("Shared fixture is required.");
        using var document = JsonDocument.Parse(stream);
        return document.RootElement.GetProperty(family).Clone();
    }

    private static CodingAssignmentContent Read(JsonElement source) => source.Deserialize<CodingAssignmentContent>(CodeAssessmentContracts.ContentJson)
        ?? throw new InvalidOperationException("The shared definition is required.");
    private static CodeAssessmentTypeAdapter Adapter() => new(new UnconfiguredCodeAssessmentExecutor());

    private sealed class CapturingExecutor : ICodeAssessmentExecutor
    {
        public CodingAssignmentContent? Definition { get; private set; }
        public JsonElement Files { get; private set; }
        public Task<CodeExecutionReceipt> ExecuteAsync(CodingAssignmentContent definition, JsonElement files, CodeToolchainIdentity toolchain, CancellationToken cancellationToken)
        {
            Definition = definition;
            Files = files.Clone();
            return Task.FromResult(new CodeExecutionReceipt([true, true], new string('a', 64)));
        }
    }
}
