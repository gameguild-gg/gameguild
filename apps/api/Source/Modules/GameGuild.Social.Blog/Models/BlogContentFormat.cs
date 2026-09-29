namespace GameGuild.Social.Blog;

/// <summary>
/// Defines how a blog post body is authored and rendered. Values are persisted and must remain stable.
/// </summary>
public enum BlogContentFormat
{
    Markdown = 0,
    Lexical = 1,
}
