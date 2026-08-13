using System.Text.Json.Serialization;

namespace GameGuild.Learning.Courses;

/// <summary>
/// Polymorphic base record for coding-assignment tests. Discriminator is the lowercase <c>kind</c> field
/// (values <c>"standard"</c>, <c>"functional"</c>).
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(StandardTest), "standard")]
[JsonDerivedType(typeof(FunctionalTestGroup), "functional")]
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
/// Function-call test group: invokes a single C/C++ function (carried once on <see cref="Function"/>)
/// against one or more input/expected cases. Weight is per-group, not per-case.
/// </summary>
public sealed record FunctionalTestGroup : Test
{
    public required TestFunctionData Function { get; init; }

    public required IReadOnlyList<FunctionalTestCase> Cases { get; init; }
}

/// <summary>
/// One case within a <see cref="FunctionalTestGroup"/>: argument list + expected return value.
/// <see cref="Inputs"/> length MUST match <see cref="TestFunctionData.Parameters"/> length (validator-enforced).
/// </summary>
public sealed record FunctionalTestCase
{
    public required FunctionParameter[] Inputs { get; init; }

    public required FunctionParameter Expected { get; init; }
}
