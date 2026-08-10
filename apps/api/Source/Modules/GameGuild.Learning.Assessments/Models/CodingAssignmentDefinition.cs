using System.Text.Json.Serialization;

namespace GameGuild.Learning.Assessments;

/// <summary>
/// V2 coding assignment definition — stored in Assessment.DefinitionPayload (jsonb).
/// </summary>
public sealed record CodingAssignmentDefinition
{
    public string Kind { get; init; } = "coding";
    public string Language { get; init; } = "cpp";
    public WorkspaceConfigDto? WorkspaceConfig { get; init; }
    public CodingTestPlanDto? TestPlan { get; init; }
    public int MaxScore { get; init; }
    public int PassingScore { get; init; }
}

// ── Workspace ────────────────────────────────────────────────────────────

public sealed record WorkspaceConfigDto
{
    public string? Id { get; init; }
    public string? Label { get; init; }
    public int? Version { get; init; }
    public CompileConfigDto? Compile { get; init; }
    public RunConfigDto? Run { get; init; }
    public TestConfigDto? Test { get; init; }
    public WorkspaceFeaturesDto? Features { get; init; }
    public Dictionary<string, BundleFileDto>? Files { get; init; }
}

public sealed record CompileConfigDto
{
    public string? Tool { get; init; }
    public List<string>? Args { get; init; }
    public string? Cwd { get; init; }
    public string? Output { get; init; }
    public string? Toolchain { get; init; }
}

public sealed record RunConfigDto
{
    public string? Type { get; init; }
    public string? Tool { get; init; }
    public List<string>? Args { get; init; }
}

public sealed record TestConfigDto
{
    public string? Tool { get; init; }
    public List<string>? CompileArgs { get; init; }
    public List<string>? RunArgs { get; init; }
    public string? Framework { get; init; }
}

public sealed record WorkspaceFeaturesDto
{
    public bool? Canvas { get; init; }
    public bool? TerminalInput { get; init; }
    public bool? ShowTestButton { get; init; }
}

public sealed record BundleFileDto
{
    public string? Encoding { get; init; }
    public string? Content { get; init; }
}

// ── Test Plan ────────────────────────────────────────────────────────────

public sealed record CodingTestPlanDto
{
    public NativeBuildConfigDto? Build { get; init; }
    public List<TestCaseDto> Cases { get; init; } = [];
    public int? TimeoutMsPerCase { get; init; }
}

public sealed record NativeBuildConfigDto
{
    public string? Toolchain { get; init; }
    public string? Compiler { get; init; }
    public List<string>? Flags { get; init; }
    public List<string>? Ldflags { get; init; }
    public Dictionary<string, string>? Defines { get; init; }
    public List<string>? IncludePaths { get; init; }
    public List<string>? LibPaths { get; init; }
    public List<string>? Libs { get; init; }
    public List<string>? Sources { get; init; }
    public string? Output { get; init; }
}

// ── Polymorphic TestCase ─────────────────────────────────────────────────

[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(StdioTestCaseDto), "stdio")]
[JsonDerivedType(typeof(StdioFileTestCaseDto), "stdio-file")]
[JsonDerivedType(typeof(ClangQueryTestCaseDto), "clang-query")]
[JsonDerivedType(typeof(DoctestTestCaseDto), "doctest")]
[JsonDerivedType(typeof(CustomTestCaseDto), "custom")]
public abstract record TestCaseDto
{
    public double? Weight { get; init; }
    public bool Hidden { get; init; }
}

public sealed record StdioTestCaseDto : TestCaseDto
{
    public string? Stdin { get; init; }
    public string ExpectedStdout { get; init; } = string.Empty;
    public string? ExpectedStderr { get; init; }
    public int? ExpectedExit { get; init; }
}

public sealed record StdioFileTestCaseDto : TestCaseDto
{
    public string InFile { get; init; } = string.Empty;
    public string ExpectedOutFile { get; init; } = string.Empty;
}

public sealed record ClangQueryTestCaseDto : TestCaseDto
{
    public string Matcher { get; init; } = string.Empty;
    public string Expect { get; init; } = string.Empty;
}

public sealed record DoctestTestCaseDto : TestCaseDto
{
    public string[] SourceFiles { get; init; } = [];
}

/// <summary>
/// Custom test case — authored by instructor via JS; included so the discriminator round-trips.
/// </summary>
public sealed record CustomTestCaseDto : TestCaseDto;
