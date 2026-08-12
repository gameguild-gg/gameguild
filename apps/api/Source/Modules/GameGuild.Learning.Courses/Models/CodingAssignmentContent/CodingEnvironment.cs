namespace GameGuild.Learning.Courses;

/// <summary>
/// Workspace runtime environment for a coding assignment.
/// </summary>
public sealed record CodingEnvironment
{
    /// <summary>One of: cpp, c, sdl-cpp, raylib-cpp (validator-enforced).</summary>
    public required string Language { get; init; }

    /// <summary>Toolset id (e.g. "clang").</summary>
    public required string Tools { get; init; }

    /// <summary>Optional pre-bundled library (sdl3, raylib, allegro, ...).</summary>
    public string? LibBundle { get; init; }

    /// <summary>Whether the learner may create new files in the workspace.</summary>
    public bool AllowStudentCreateFiles { get; init; }
}
