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

public sealed class CodeExecutionConfigurationAdapterTests
{
    public static TheoryData<string> InvalidConfigurations => new()
    {
        "runtime-without-execution", "runtime-missing", "runtime-null", "runtime-unknown", "legacy-language",
        "c-graphics", "wasi-with-library", "graphics-wrong-tools", "graphics-wrong-library",
        "limits-missing", "limits-null", "entry-point-missing", "entry-point-null", "toolchain-missing", "toolchain-null",
        "time-missing", "time-zero", "time-exceeded", "output-zero", "output-exceeded", "file-limit-below-authored", "file-limit-exceeded",
        "memory-configured", "memory-zero", "entry-point-traversal", "entry-point-missing-file", "entry-point-private",
        "toolchain-key", "toolchain-version", "toolchain-abi", "toolchain-digest", "toolchain-digest-prefix",
        "stdio-symbol", "function-symbol-mismatch", "function-signature-mismatch", "aliased-authored-files", "unsafe-authored-path",
    };

    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public void ProjectionAndDelivery_PreserveExactExplicitConfigurationAndRedactOnlyProtectedData(string family)
    {
        var source = JsonSerializer.SerializeToElement(Definition(family), CodeAssessmentContracts.ContentJson);
        var adapter = Adapter();

        var projection = adapter.ProjectAuthoring(source);
        var item = projection.Items.Single().PrivateProjection;
        var delivery = adapter.GenerateDelivery(item).GetProperty("definition");

        CanonicalJson.Serialize(projection.Content).Should().Be(CanonicalJson.Serialize(source));
        AssertOriginalConfiguration(projection.Content, family);
        AssertOriginalConfiguration(item.GetProperty("definition"), family);
        AssertOriginalConfiguration(delivery, family);
        delivery.TryGetProperty("ValidationSolutions", out _).Should().BeFalse();
        delivery.GetProperty("Tests").GetProperty("Private").GetArrayLength().Should().Be(0);
        delivery.GetRawText().Should().NotContain("SYNTHETIC-");
        item.GetRawText().Should().Contain("SYNTHETIC-TEACHER-REFERENCE");
        delivery.GetProperty("Data").GetProperty("Files").GetProperty("/home/user/main.cpp")
            .GetProperty("Id").GetString().Should().Be("main-file");
    }

    [Fact]
    public void LegacyProjectionAndDelivery_KeepAbsentExecutionAndRuntimeAbsent()
    {
        var source = CodePublicationMetadataTests.Definition("legacy");
        var adapter = Adapter();

        var projection = adapter.ProjectAuthoring(source);
        var itemDefinition = projection.Items.Single().PrivateProjection.GetProperty("definition");
        var delivery = adapter.GenerateDelivery(projection.Items.Single().PrivateProjection).GetProperty("definition");

        CanonicalJson.Serialize(projection.Content).Should().Be(CanonicalJson.Serialize(source));
        foreach (var definition in new[] { projection.Content, itemDefinition, delivery })
        {
            definition.TryGetProperty("Execution", out _).Should().BeFalse();
            definition.GetProperty("Environment").TryGetProperty("Runtime", out _).Should().BeFalse();
        }
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public void Authoring_RejectsInvalidExplicitConfigurationWithoutEchoingPrivateReferences(string mutation)
    {
        var source = AlteredDefinition(mutation);

        Action publish = () => Adapter().ProjectAuthoring(source);

        publish.Should().Throw<JsonException>().Which.Message.Should().NotContain("SYNTHETIC-");
    }

    [Theory]
    [MemberData(nameof(InvalidConfigurations))]
    public async Task OfficialReview_RevalidatesExplicitConfigurationBeforeCallingExecutor(string mutation)
    {
        var executor = new CapturingExecutor();
        var adapter = new CodeAssessmentTypeAdapter(executor);
        var item = JsonNode.Parse(adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(Definition()))
            .Items.Single().PrivateProjection.GetRawText())!.AsObject();
        item["definition"] = JsonNode.Parse(AlteredDefinition(mutation).GetRawText());
        var projection = JsonSerializer.SerializeToElement(item);
        var response = JsonSerializer.SerializeToElement(new { files = new Dictionary<string, object>() });

        Func<Task> review = async () => await adapter.EvaluateDeterministicAsync(Request(projection, response), CancellationToken.None);

        (await review.Should().ThrowAsync<JsonException>()).Which.Message.Should().NotContain("SYNTHETIC-");
        executor.Calls.Should().Be(0);
        executor.Definition.Should().BeNull();
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public void Decoder_RejectsMergedFileCountNotMerelyTheNumberOfSubmittedFiles(int maximum, int addedFiles)
    {
        var definition = WithFileLimit(maximum);
        var adapter = Adapter();
        var projection = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(definition)).Items.Single().PrivateProjection;
        var files = Enumerable.Range(0, addedFiles).ToDictionary(index => $"extra-{index}.cpp", _ => File("// added"));

        Action decode = () => adapter.DecodeResponse(Envelope(files), [projection]);

        decode.Should().Throw<JsonException>();
    }

    [Fact]
    public void Decoder_AuthoredAliasReplacementDoesNotConsumeAnotherFileSlot()
    {
        var adapter = Adapter();
        var projection = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(WithFileLimit(2))).Items.Single().PrivateProjection;
        var files = new Dictionary<string, object> { ["/user/main.cpp"] = File("// replacement") };

        var response = adapter.DecodeResponse(Envelope(files), [projection]);

        response.GetProperty("files").EnumerateObject().Should().ContainSingle();
        response.GetProperty("files").GetProperty("/user/main.cpp").GetProperty("content").GetString().Should().Be("// replacement");
    }

    [Fact]
    public void Decoder_AllowsTheExactMergedFileLimitIncludingUnsubmittedPrivateFiles()
    {
        var adapter = Adapter();
        var projection = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(WithFileLimit(3))).Items.Single().PrivateProjection;
        var files = new Dictionary<string, object>
        {
            ["/user/main.cpp"] = File("// replacement"),
            ["extra.cpp"] = File("// added"),
        };

        var response = adapter.DecodeResponse(Envelope(files), [projection]);

        response.GetProperty("files").EnumerateObject().Should().HaveCount(2);
    }

    [Fact]
    public async Task OfficialReview_RejectsMergedFileOverflowEvenWhenTheDecoderWasNotCalled()
    {
        var executor = new CapturingExecutor();
        var adapter = new CodeAssessmentTypeAdapter(executor);
        var projection = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(WithFileLimit(2))).Items.Single().PrivateProjection;
        var response = JsonSerializer.SerializeToElement(new { files = new Dictionary<string, object> { ["extra.cpp"] = File("// added") } });

        Func<Task> review = async () => await adapter.EvaluateDeterministicAsync(Request(projection, response), CancellationToken.None);

        await review.Should().ThrowAsync<JsonException>();
        executor.Calls.Should().Be(0);
    }

    [Theory]
    [InlineData("function")]
    [InlineData("standard-io")]
    public async Task OfficialExecutor_ReceivesTheExactFrozenConfigurationNotLaterAuthoringValues(string family)
    {
        var executor = new CapturingExecutor();
        var adapter = new CodeAssessmentTypeAdapter(executor);
        var frozen = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(Definition(family))).Items.Single().PrivateProjection;
        var later = adapter.ProjectAuthoring(JsonSerializer.SerializeToElement(LaterDefinition(family))).Items.Single().PrivateProjection;
        var response = adapter.DecodeResponse(Envelope(new Dictionary<string, object>
        {
            ["/user/main.cpp"] = File("// LEARNER-ANSWER"),
        }), [frozen]);

        await adapter.EvaluateDeterministicAsync(Request(frozen, response), CancellationToken.None);

        executor.Calls.Should().Be(1);
        executor.Definition.Should().NotBeNull();
        var executed = executor.Definition!;
        AssertOriginalConfiguration(JsonSerializer.SerializeToElement(executed), family);
        executor.Toolchain.Should().Be(new CodeToolchainIdentity("4.5.2", "emception-browser-v1",
            "405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37"));
        executed.ValidationSolutions.Should().BeNull();
        executed.Tests.Private.Should().ContainSingle();
        executed.Data.Files["/home/user/main.cpp"].Content.Should().Be(family == "function"
            ? "int sum(int a,int b){return 0;}" : "int main(){}");
        executor.Files.GetProperty("/user/main.cpp").GetProperty("content").GetString().Should().Be("// LEARNER-ANSWER");
        JsonSerializer.Serialize(executor.Definition).Should().NotContain("SYNTHETIC-TEACHER-REFERENCE");
        later.GetProperty("definition").GetProperty("Execution").GetProperty("Toolchain").GetProperty("Key").GetString().Should().Be("c-emception");
        later.GetProperty("definition").GetProperty("Environment").GetProperty("Runtime").GetString().Should().Be("wasi");
    }

    internal static CodingAssignmentContent Definition(string family = "standard-io")
    {
        var source = CodePublicationMetadataTests.Definition(family).Deserialize<CodingAssignmentContent>(CodeAssessmentContracts.ContentJson)!;
        return source with
        {
            Environment = new CodingEnvironment { Language = "cpp", Runtime = "sdl3", Tools = "emcc", LibBundle = "sdl3" },
            Execution = new CodingExecutionConfiguration
            {
                Limits = new CodingExecutionLimits { TimeLimitMs = 1379, OutputLimitBytes = 65521, MaxFileCount = 7 },
                EntryPoint = new CodingExecutionEntryPoint { Path = "/home/user/main.cpp", Symbol = family == "function" ? "sum" : null },
                Toolchain = new CodingExecutionToolchain
                {
                    Key = "cpp-emception", Version = "4.5.2", RuntimeAbi = "emception-browser-v1",
                    ArtifactDigest = "sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37",
                },
            },
        };
    }

    internal static CodingAssignmentContent LaterDefinition(string family)
    {
        var source = Definition(family);
        var execution = source.Execution!;
        var files = new Dictionary<string, BundleFileMeta>(source.Data.Files, StringComparer.Ordinal);
        files["/home/user/next.c"] = files["/home/user/main.cpp"];
        files.Remove("/home/user/main.cpp");
        return source with
        {
            Environment = new CodingEnvironment { Language = "c", Runtime = "wasi", Tools = "clang" },
            Execution = execution with
            {
                Limits = new CodingExecutionLimits { TimeLimitMs = 2761, OutputLimitBytes = 91009, MaxFileCount = 9 },
                EntryPoint = execution.EntryPoint with { Path = "/home/user/next.c" },
                Toolchain = execution.Toolchain with { Key = "c-emception" },
            },
            Data = new WorkspaceData { Files = files },
            ValidationSolutions = source.ValidationSolutions!.Select(solution => solution with
            {
                Files = solution.Files.Select(file => file with { Path = "/home/user/next.c" }).ToArray(),
            }).ToArray(),
        };
    }

    internal static void AssertOriginalConfiguration(JsonElement definition, string family)
    {
        var environment = definition.GetProperty("Environment");
        environment.GetProperty("Language").GetString().Should().Be("cpp");
        environment.GetProperty("Runtime").GetString().Should().Be("sdl3");
        environment.GetProperty("Tools").GetString().Should().Be("emcc");
        environment.GetProperty("LibBundle").GetString().Should().Be("sdl3");
        var execution = definition.GetProperty("Execution");
        execution.GetProperty("Limits").GetProperty("TimeLimitMs").GetInt32().Should().Be(1379);
        execution.GetProperty("Limits").GetProperty("OutputLimitBytes").GetInt32().Should().Be(65521);
        execution.GetProperty("Limits").GetProperty("MaxFileCount").GetInt32().Should().Be(7);
        execution.GetProperty("Limits").TryGetProperty("MemoryLimitBytes", out _).Should().BeFalse();
        execution.GetProperty("EntryPoint").GetProperty("Path").GetString().Should().Be("/home/user/main.cpp");
        if (family == "function")
        {
            execution.GetProperty("EntryPoint").GetProperty("Symbol").GetString().Should().Be("sum");
        }
        else
        {
            execution.GetProperty("EntryPoint").TryGetProperty("Symbol", out _).Should().BeFalse();
        }
        execution.GetProperty("Toolchain").GetProperty("Key").GetString().Should().Be("cpp-emception");
        execution.GetProperty("Toolchain").GetProperty("Version").GetString().Should().Be("4.5.2");
        execution.GetProperty("Toolchain").GetProperty("RuntimeAbi").GetString().Should().Be("emception-browser-v1");
        execution.GetProperty("Toolchain").GetProperty("ArtifactDigest").GetString().Should()
            .Be("sha256:405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37");
    }

    private static CodingAssignmentContent WithFileLimit(int maximum)
    {
        var definition = Definition();
        var execution = definition.Execution!;
        return definition with
        {
            Environment = definition.Environment with { AllowStudentCreateFiles = true },
            Execution = execution with { Limits = execution.Limits with { MaxFileCount = maximum } },
        };
    }

    private static JsonElement AlteredDefinition(string mutation)
    {
        var family = mutation.StartsWith("function-", StringComparison.Ordinal) ? "function" : "standard-io";
        var source = JsonSerializer.SerializeToNode(Definition(family))!.AsObject();
        var environment = source["Environment"]!.AsObject();
        var execution = source["Execution"]!.AsObject();
        var limits = execution["Limits"]!.AsObject();
        var entryPoint = execution["EntryPoint"]!.AsObject();
        var toolchain = execution["Toolchain"]!.AsObject();
        switch (mutation)
        {
            case "runtime-without-execution": source.Remove("Execution"); break;
            case "runtime-missing": environment.Remove("Runtime"); break;
            case "runtime-null": environment["Runtime"] = null; break;
            case "runtime-unknown": environment["Runtime"] = "unknown"; break;
            case "legacy-language": environment["Language"] = "sdl-cpp"; break;
            case "c-graphics": environment["Language"] = "c"; toolchain["Key"] = "c-emception"; break;
            case "wasi-with-library": environment["Runtime"] = "wasi"; environment["Tools"] = "clang"; break;
            case "graphics-wrong-tools": environment["Tools"] = "clang"; break;
            case "graphics-wrong-library": environment["LibBundle"] = "raylib"; break;
            case "limits-missing": execution.Remove("Limits"); break;
            case "limits-null": execution["Limits"] = null; break;
            case "entry-point-missing": execution.Remove("EntryPoint"); break;
            case "entry-point-null": execution["EntryPoint"] = null; break;
            case "toolchain-missing": execution.Remove("Toolchain"); break;
            case "toolchain-null": execution["Toolchain"] = null; break;
            case "time-missing": limits.Remove("TimeLimitMs"); break;
            case "time-zero": limits["TimeLimitMs"] = 0; break;
            case "time-exceeded": limits["TimeLimitMs"] = 30001; break;
            case "output-zero": limits["OutputLimitBytes"] = 0; break;
            case "output-exceeded": limits["OutputLimitBytes"] = 2000001; break;
            case "file-limit-below-authored": limits["MaxFileCount"] = 1; break;
            case "file-limit-exceeded": limits["MaxFileCount"] = 101; break;
            case "memory-configured": limits["MemoryLimitBytes"] = 134217728; break;
            case "memory-zero": limits["MemoryLimitBytes"] = 0; break;
            case "entry-point-traversal": entryPoint["Path"] = "../main.cpp"; break;
            case "entry-point-missing-file": entryPoint["Path"] = "/home/user/absent.cpp"; break;
            case "entry-point-private": entryPoint["Path"] = "/home/user/hidden.h"; break;
            case "toolchain-key": toolchain["Key"] = "c-emception"; break;
            case "toolchain-version": toolchain["Version"] = "4.4.0"; break;
            case "toolchain-abi": toolchain["RuntimeAbi"] = "emception-browser-v2"; break;
            case "toolchain-digest": toolchain["ArtifactDigest"] = "sha256:" + new string('0', 64); break;
            case "toolchain-digest-prefix": toolchain["ArtifactDigest"] = "405e9f1fdd936ba31e11247fa1f901eaee24c4fcf641e343505df75c3d539b37"; break;
            case "stdio-symbol": entryPoint["Symbol"] = "main"; break;
            case "function-symbol-mismatch": entryPoint["Symbol"] = "other"; break;
            case "function-signature-mismatch": source["Tests"]!["Private"]![0]!["Function"]!["Parameters"]![0]!["Name"] = "renamed"; break;
            case "aliased-authored-files":
                var alias = source["Data"]!["Files"]!["/home/user/main.cpp"]!.DeepClone();
                alias["Id"] = "alias-file";
                source["Data"]!["Files"]!["/user/main.cpp"] = alias;
                break;
            case "unsafe-authored-path":
                source["Data"]!["Files"]!["../unused.h"] = JsonSerializer.SerializeToNode(new BundleFileMeta { Id = "unused-file", Content = "// support" });
                break;
            default: throw new ArgumentOutOfRangeException(nameof(mutation));
        }
        return JsonSerializer.SerializeToElement(source);
    }

    private static CodeAssessmentTypeAdapter Adapter() => new(new UnconfiguredCodeAssessmentExecutor());
    private static object File(string content) => new { content, encoding = "text" };
    private static AssessmentResponseEnvelopeV1 Envelope(Dictionary<string, object> files) =>
        new(1, "coding-assignment", "code-files", JsonSerializer.SerializeToElement(new { files }));
    private static DeterministicReviewRequest Request(JsonElement projection, JsonElement response) => new([projection],
        new AssessmentExecutionDeliveryV1(1, Guid.NewGuid(), new string('a', 64), [], new Dictionary<string, AssessmentExecutionDeliveryItemV1>()),
        response, CodeAssessmentContracts.HandlerKey, CodeAssessmentContracts.Version);

    private sealed class CapturingExecutor : ICodeAssessmentExecutor
    {
        public int Calls { get; private set; }
        public CodingAssignmentContent? Definition { get; private set; }
        public JsonElement Files { get; private set; }
        public CodeToolchainIdentity? Toolchain { get; private set; }

        public Task<CodeExecutionReceipt> ExecuteAsync(CodingAssignmentContent definition, JsonElement files, CodeToolchainIdentity toolchain,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Calls++;
            Definition = definition;
            Files = files.Clone();
            Toolchain = toolchain;
            return Task.FromResult(new CodeExecutionReceipt([true, true], new string('a', 64)));
        }
    }
}
