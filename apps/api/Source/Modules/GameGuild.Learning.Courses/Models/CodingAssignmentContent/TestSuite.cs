namespace GameGuild.Learning.Courses;

/// <summary>
/// Test buckets for a coding assignment. Field names are PascalCase on the wire (deliberate divergence from the draft).
/// Visibility values match <c>TestVisibilityType</c> from the draft: <c>"Public"</c>, <c>"Private"</c>.
/// </summary>
public sealed record TestSuite
{
    public List<Test> Public { get; init; } = new();

    public List<Test> Private { get; init; } = new();
}
