namespace GameGuild.Learning.Courses;

/// <summary>
/// Discriminates whether <see cref="ProgramContent.EstimatedMinutes"/> was computed automatically
/// (200 words-per-minute word count, recomputed on every save) or pinned manually by an author
/// (preserved verbatim across saves).
/// </summary>
public enum EstimatedMinutesSource
{
    Auto = 0,
    Manual = 1,
}
