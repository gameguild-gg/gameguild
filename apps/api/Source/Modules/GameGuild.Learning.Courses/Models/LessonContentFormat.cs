namespace GameGuild.Learning.Courses;

/// <summary>
/// Defines how a lesson body is authored and rendered. Values are persisted and must remain stable.
/// </summary>
public enum LessonContentFormat
{
    Markdown = 0,
    Lexical = 1,
    RevealJs = 2,
    Video = 3,
}
