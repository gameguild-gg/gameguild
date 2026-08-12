using System.Text.Json.Serialization;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Polymorphic base record for coding-assignment tests. Discriminator is the lowercase <c>kind</c> field
/// (values <c>"standard"</c>, <c>"functional"</c>).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(StandardTest), "standard")]
[JsonDerivedType(typeof(FunctionalTest), "functional")]
public abstract record Test
{
    /// <summary>Relative weight used to compute total score. Defaults to 1.0; must be &gt;= 0.</summary>
    public double Weight { get; init; } = 1.0;

    /// <summary>Optional human-readable test name.</summary>
    public string? Name { get; init; }
}

/// <summary>
/// Stdio-based test: runs the student program and compares stdout/stderr/exit code.
/// </summary>
public sealed record StandardTest : Test
{
    public string? Stdin { get; init; }

    public required string Stdout { get; init; }

    public string? Stderr { get; init; }

    public int? ExitCode { get; init; }
}

/// <summary>
/// Function-call test: invokes a specific C/C++ function with typed arguments and compares the return value.
/// </summary>
public sealed record FunctionalTest : Test
{
    public required TestFunctionData Function { get; init; }

    public required FunctionParameter Result { get; init; }
}
