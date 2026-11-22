using System.Text.Json.Serialization;

namespace GameGuild.Learning.Courses;

/// <summary>
/// v1 coding-assignment content stored in <see cref="ProgramContent.JsonBody"/>.
/// Discriminator root: <c>Type == "coding-assignment"</c>, <c>Version == 1</c>.
/// </summary>
public sealed record CodingAssignmentContent
{
    public string Type { get; init; } = "coding-assignment";

    public int Version { get; init; } = 1;

    /// <summary>
    /// Product experience rendered for this definition. This axis is
    /// independent from language, toolchain, test kind, and review policy.
    /// </summary>
    public required string ExerciseType { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodingProblem? Problem { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<CodingValidationSolution>? ValidationSolutions { get; init; }

    public required CodingEnvironment Environment { get; init; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public CodingExecutionConfiguration? Execution { get; init; }

    public required WorkspaceData Data { get; init; }

    public required TestSuite Tests { get; init; }

    public required GradingConfig Grading { get; init; }
}
