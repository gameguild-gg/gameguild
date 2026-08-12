namespace GameGuild.Learning.Courses;

/// <summary>
/// v1 coding-assignment content stored in <see cref="ProgramContent.JsonBody"/>.
/// Discriminator root: <c>Type == "coding-assignment"</c>, <c>Version == 1</c>.
/// </summary>
public sealed record CodingAssignmentContent
{
    public string Type { get; init; } = "coding-assignment";

    public int Version { get; init; } = 1;

    public required CodingEnvironment Environment { get; init; }

    public required WorkspaceData Data { get; init; }

    public required TestSuite Tests { get; init; }

    public required GradingConfig Grading { get; init; }
}
