namespace GameGuild.Learning.Courses;

/// <summary>
/// Grading configuration for a coding assignment.
/// </summary>
public sealed record GradingConfig
{
    public int MaxScore { get; init; }

    public int PassingScore { get; init; }
}
