using System.Text.Json;

namespace GameGuild.Learning.Courses;

internal static class LessonContentFormatInference
{
    public static LessonContentFormat FromBody(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return LessonContentFormat.Markdown;
        }

        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("root", out _))
            {
                return LessonContentFormat.Lexical;
            }
        }
        catch (JsonException)
        {
            // Plain lesson bodies remain Markdown unless the client declares another format.
        }

        return LessonContentFormat.Markdown;
    }
}
