using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace GameGuild.Learning.Courses;

internal static class ReadingTimeEstimator
{
    private const int WordsPerMinute = 200;

    private static readonly Regex HtmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MarkdownPunctuationRegex = new(@"[#*_>\[\]()!~|`-]", RegexOptions.Compiled);

    private static readonly HashSet<string> TextPropertyKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "text", "question", "title", "description", "prompt", "content", "body", "label",
        "placeholder", "explanation", "feedback", "answer", "option", "choices",
        "statement", "rationale", "hint", "caption", "alt", "ariaLabel",
    };

    public static int? EstimateMinutes(ProgramContentType type, LessonContentFormat? lessonFormat, string? body, string? jsonBody)
    {
        if (lessonFormat == LessonContentFormat.Video)
        {
            return null;
        }

        var buffer = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(jsonBody))
        {
            try
            {
                using var document = JsonDocument.Parse(jsonBody);
                CollectText(document.RootElement, buffer);
            }
            catch (JsonException)
            {
                // Malformed JSON falls through to the plain body.
            }
        }

        if (!string.IsNullOrWhiteSpace(body))
        {
            var stripped = HtmlTagRegex.Replace(body, " ");
            stripped = MarkdownPunctuationRegex.Replace(stripped, " ");
            buffer.Append(stripped).Append(' ');
        }

        var words = buffer.ToString().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

        if (words == 0)
        {
            return null;
        }

        return Math.Max(1, (int)Math.Ceiling(words / (double)WordsPerMinute));
    }

    private static void CollectText(JsonElement element, StringBuilder buffer)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String &&
                        TextPropertyKeys.Contains(property.Name))
                    {
                        buffer.Append(property.Value.GetString()).Append(' ');
                    }
                    else
                    {
                        CollectText(property.Value, buffer);
                    }
                }

                break;

            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    CollectText(child, buffer);
                }

                break;

            // Root-level strings have no owning property key; skip.
        }
    }
}
