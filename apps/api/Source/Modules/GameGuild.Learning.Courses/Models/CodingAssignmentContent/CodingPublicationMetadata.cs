namespace GameGuild.Learning.Courses;

/// <summary>The problem frozen with a published coding definition, not live lesson metadata.</summary>
public sealed record CodingProblem
{
    public required string Statement { get; init; }
    public required IReadOnlyList<CodingProblemExample> Examples { get; init; }
    public required IReadOnlyList<string> Constraints { get; init; }
}

public sealed record CodingProblemExample
{
    public required string Id { get; init; }
    public required string Input { get; init; }
    public required string Output { get; init; }
    public required string Explanation { get; init; }
}

/// <summary>Author-only replacements, separate from files compiled for a learner submission.</summary>
public sealed record CodingValidationSolution
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Role { get; init; }
    public required IReadOnlyList<CodingSolutionFile> Files { get; init; }
}

public sealed record CodingSolutionFile
{
    public required string FileId { get; init; }
    public required string Path { get; init; }
    public required string Content { get; init; }
}
